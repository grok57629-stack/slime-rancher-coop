using Lidgren.Network;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    /// <summary>A bounded bundle of unreliable actor transforms.</summary>
    [Packet(PacketType.ActorPositions)]
    public class PacketActorPositions : Packet
    {
        public const int MaxPositionsPerPacket = 20;

        public struct ActorPosition
        {
            public int ID;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public List<ActorPosition> Positions { get; set; }

        public PacketActorPositions() { }
        public PacketActorPositions(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            if (Positions == null || Positions.Count > MaxPositionsPerPacket)
                throw new InvalidDataException("Actor position batch has an invalid size.");

            base.Serialize(om);
            om.Write((byte)Positions.Count);
            foreach (var position in Positions)
            {
                om.Write(position.ID);
                om.Write(position.Position);
                om.Write(position.Rotation);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);
            int count = im.ReadByte();
            if (count > MaxPositionsPerPacket)
                throw new InvalidDataException("Actor position batch exceeds the packet limit.");

            Positions = new List<ActorPosition>(count);
            for (int i = 0; i < count; i++)
            {
                Positions.Add(new ActorPosition
                {
                    ID = im.ReadInt32(),
                    Position = im.ReadVector3(),
                    Rotation = im.ReadQuaternion()
                });
            }
        }
    }
}
