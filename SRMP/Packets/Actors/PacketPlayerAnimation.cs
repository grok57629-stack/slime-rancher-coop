using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SRMultiplayer.Packets.PacketAccessDoors;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerAnimation)]
    public class PacketPlayerAnimation : Packet
    {
        // The animator parameter mask is a UInt64. Four layer fields plus one
        // mask and at most 64 parameter values fit in this protocol bound.
        public const int MaxDataItems = 69;

        public enum AnimationType : int
        {
            Layer,
            Speed,
            Parameters

        }

        public byte ID;
        public byte Type;
        public struct AnimationData
        {
            public byte type
            {
                get
                {
                    if (fData.HasValue) return 0;
                    if (bData.HasValue) return 1;
                    if (iData.HasValue) return 2;
                    if (uData.HasValue) return 3;
                    return 8;
                }
            }
            public float? fData;
            public bool? bData;
            public int? iData;
            public ulong? uData;
        }

        public Queue<AnimationData> internalData { get; set; } = new Queue<AnimationData>();

        public void Add<T>(T obj)
        {
            switch (obj)
            {
                case float itm:
                    internalData.Enqueue(new AnimationData() { fData = itm });
                    break;
                case bool itm:
                    internalData.Enqueue(new AnimationData() { bData = itm });
                    break;
                case int itm:
                    internalData.Enqueue(new AnimationData() { iData = itm });
                    break;
                case ulong itm:
                    internalData.Enqueue(new AnimationData() { uData = itm });
                    break;
            }
        }

        /// <summary>
        /// mark construction inheritance incase we need it
        /// </summary>
        public PacketPlayerAnimation() : base() { }
        /// <summary>
        /// mark construction inheritance so the deserialization automatically happens for is
        /// since the base decalres this
        /// </summary>
        public PacketPlayerAnimation(NetIncomingMessage im) : base(im) { }

        /// <summary>
        /// The server applies animation locally, and the read methods consume
        /// internalData. Relay must use an independent queue so remote players
        /// receive the original packet contents.
        /// </summary>
        public PacketPlayerAnimation CopyForRelay()
        {
            return new PacketPlayerAnimation
            {
                ID = ID,
                Type = Type,
                internalData = new Queue<AnimationData>(internalData)
            };
        }

        public override void Serialize(NetOutgoingMessage om)
        {
            int minimumCount = Type == (byte)AnimationType.Layer ? 5 : 1;
            if (Type > (byte)AnimationType.Parameters || internalData == null
                || internalData.Count < minimumCount || internalData.Count > MaxDataItems)
                throw new InvalidDataException($"Invalid player animation packet: type={Type}, count={internalData?.Count ?? -1}.");

            base.Serialize(om);

            om.Write(internalData.Count);
            foreach (var data in internalData)
            {
                om.Write(data.type);
                switch (data.type)
                {
                    case 0:
                        om.Write(data.fData.Value);
                        break;
                    case 1:
                        om.Write(data.bData.Value);
                        break;
                    case 2:
                        om.Write(data.iData.Value);
                        break;
                    case 3:
                        om.Write(data.uData.Value);
                        break;
                    default:
                        throw new InvalidDataException($"Unknown player animation value type: {data.type}.");
                }
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            internalData = new Queue<AnimationData>();
            int Count = im.ReadInt32();
            if (Count < 0 || Count > MaxDataItems)
                throw new InvalidDataException($"Invalid player animation data count: {Count}.");
            if (Type > (byte)AnimationType.Parameters)
                throw new InvalidDataException($"Unknown player animation type: {Type}.");
            int minimumCount = Type == (byte)AnimationType.Layer ? 5 : 1;
            if (Count < minimumCount)
                throw new InvalidDataException($"Incomplete player animation packet: type={Type}, count={Count}.");

            for (int i = 0; i < Count; i++)
            {
                var data = new AnimationData();
                byte type = im.ReadByte();
                switch (type)
                {
                    case 0:
                        data.fData = im.ReadFloat();
                        break;
                    case 1:
                        data.bData = im.ReadBoolean();
                        break;
                    case 2:
                        data.iData = im.ReadInt32();
                        break;
                    case 3:
                        data.uData = im.ReadUInt64();
                        break;
                    default:
                        throw new InvalidDataException($"Unknown player animation value type: {type}.");
                }

                internalData.Enqueue(data);
            }
        }
    }

}
