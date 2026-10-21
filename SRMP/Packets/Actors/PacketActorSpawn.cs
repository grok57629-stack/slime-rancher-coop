using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorSpawn)]
    public class PacketActorSpawn : Packet
    {
        public int ID;
        public byte Owner;
        public ushort Ident;
        public Vector3 Position;
        public Quaternion Rotation;
        public byte RegionSet;

        public PacketActorSpawn() { }
        public PacketActorSpawn(NetIncomingMessage im) { Deserialize(im); }
    }
}
