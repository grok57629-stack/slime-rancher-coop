using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SRMultiplayer.Networking
{
    // Shared by both EOS endpoints. This file deliberately has no Unity/EOS
    // dependencies so the actual transport framing can be regression tested.
    internal static class PacketFraming
    {
        internal const int HeaderSize = 20;
        internal const int FragmentSize = 1000;
        internal const int MaxMessageSize = 16 * 1024 * 1024;
        // Version 3 changes the authentication roster payload. Older peers
        // must be rejected at the frame boundary instead of misreading actor
        // cursors/rosters and failing later with a misleading roster-size error.
        internal const ushort ProtocolVersion = 3;
        private const uint Magic = 0x504D5253; // SRMP, little endian

        internal static IEnumerable<byte[]> Encode(ushort type, uint messageId, byte[] payload, int length)
        {
            if (payload == null || length <= 0 || length > payload.Length || length > MaxMessageSize)
                throw new ArgumentOutOfRangeException(nameof(length));

            int count = (length + FragmentSize - 1) / FragmentSize;
            for (int index = 0; index < count; index++)
            {
                int offset = index * FragmentSize;
                int size = Math.Min(FragmentSize, length - offset);
                var frame = new byte[HeaderSize + size];
                using (var writer = new BinaryWriter(new MemoryStream(frame)))
                {
                    writer.Write(Magic);
                    writer.Write(ProtocolVersion);
                    writer.Write(type);
                    writer.Write(messageId);
                    writer.Write(length);
                    writer.Write((ushort)index);
                    writer.Write((ushort)count);
                    writer.Write(payload, offset, size);
                }
                yield return frame;
            }
        }

        internal static void ReadHeader(byte[] frame, int length, out ushort type, out uint id,
            out int totalLength, out int index, out int count)
        {
            if (frame == null || length < HeaderSize || length > frame.Length)
                throw new InvalidDataException("Truncated SRMP frame.");
            using (var reader = new BinaryReader(new MemoryStream(frame, 0, length)))
            {
                if (reader.ReadUInt32() != Magic || reader.ReadUInt16() != ProtocolVersion)
                    throw new InvalidDataException("Incompatible SRMP protocol. Install the same build on every peer.");
                type = reader.ReadUInt16();
                id = reader.ReadUInt32();
                totalLength = reader.ReadInt32();
                index = reader.ReadUInt16();
                count = reader.ReadUInt16();
            }
            if (totalLength <= 0 || totalLength > MaxMessageSize ||
                count != (totalLength + FragmentSize - 1) / FragmentSize || index >= count ||
                length != HeaderSize + Math.Min(FragmentSize, totalLength - index * FragmentSize))
                throw new InvalidDataException("Invalid SRMP fragment length/index/count.");
        }
    }

    // One instance per remote peer, never one shared bucket per packet type.
    internal sealed class PacketReassembler
    {
        private sealed class Pending
        {
            internal ushort Type;
            internal byte[] Data;
            internal bool[] Received;
            internal int Count;
            internal double StartedAt;
        }

        private readonly Dictionary<uint, Pending> pending = new Dictionary<uint, Pending>();
        private readonly HashSet<uint> completed = new HashSet<uint>();
        private readonly Queue<uint> completedOrder = new Queue<uint>();
        private int pendingBytes;
        internal int PendingCount => pending.Count;

        internal bool TryReceive(byte[] frame, int length, double now, out ushort type, out byte[] payload)
        {
            payload = null;
            PacketFraming.ReadHeader(frame, length, out type, out uint id, out int totalLength,
                out int index, out int count);
            Expire(now);
            if (completed.Contains(id)) return false;

            if (!pending.TryGetValue(id, out var message))
            {
                if (pending.Count >= 32 || pendingBytes + totalLength > 2 * PacketFraming.MaxMessageSize)
                    throw new InvalidDataException("SRMP incomplete-message budget exceeded.");
                message = new Pending
                {
                    Type = type, Data = new byte[totalLength], Received = new bool[count], StartedAt = now
                };
                pending.Add(id, message);
                pendingBytes += totalLength;
            }
            else if (message.Type != type || message.Data.Length != totalLength)
            {
                throw new InvalidDataException("Conflicting SRMP message headers.");
            }

            if (!message.Received[index])
            {
                Buffer.BlockCopy(frame, PacketFraming.HeaderSize, message.Data,
                    index * PacketFraming.FragmentSize, length - PacketFraming.HeaderSize);
                message.Received[index] = true;
                message.Count++;
            }
            if (message.Count != count) return false;

            pending.Remove(id);
            pendingBytes -= totalLength;
            completed.Add(id);
            completedOrder.Enqueue(id);
            if (completedOrder.Count > 1024) completed.Remove(completedOrder.Dequeue());
            payload = message.Data;
            return true;
        }

        internal void Expire(double now)
        {
            foreach (var id in pending.Where(p => now - p.Value.StartedAt > 30).Select(p => p.Key).ToArray())
            {
                pendingBytes -= pending[id].Data.Length;
                pending.Remove(id);
            }
        }
    }
}
