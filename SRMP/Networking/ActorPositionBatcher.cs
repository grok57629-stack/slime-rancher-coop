using Lidgren.Network;
using SRMultiplayer.Packets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SRMultiplayer.Networking
{
    /// <summary>Coalesces actor movement changes and sends bounded batches once per frame.</summary>
    internal static class ActorPositionBatcher
    {
        private static readonly Dictionary<int, PacketActorPositions.ActorPosition> Pending =
            new Dictionary<int, PacketActorPositions.ActorPosition>();

        internal static void Enqueue(int id, Vector3 position, Quaternion rotation)
        {
            if (id == 0) return;
            Pending[id] = new PacketActorPositions.ActorPosition
            {
                ID = id,
                Position = position,
                Rotation = rotation
            };
        }

        internal static void Flush()
        {
            if (Pending.Count == 0) return;
            if (!Globals.IsMultiplayer || (!Globals.IsServer && !Globals.IsClient))
            {
                Pending.Clear();
                return;
            }

            var positions = Pending.Values.ToArray();
            Pending.Clear();
            for (int offset = 0; offset < positions.Length; offset += PacketActorPositions.MaxPositionsPerPacket)
            {
                int count = System.Math.Min(PacketActorPositions.MaxPositionsPerPacket, positions.Length - offset);
                var batch = new List<PacketActorPositions.ActorPosition>(count);
                for (int i = 0; i < count; i++) batch.Add(positions[offset + i]);

                new PacketActorPositions { Positions = batch }.Send(NetDeliveryMethod.Unreliable);
            }
        }
    }
}
