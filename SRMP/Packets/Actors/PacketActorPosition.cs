using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorPosition)]
    public class PacketActorPosition : Packet
    {
        public int ID;
        public Vector3 Position;
        public Quaternion Rotation;

        public PacketActorPosition() { }
        public PacketActorPosition(NetIncomingMessage im) { Deserialize(im); }
    }
}
