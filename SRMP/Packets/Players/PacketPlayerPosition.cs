using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerPosition)]
    public class PacketPlayerPosition : Packet
    {
        public byte ID;
        public Vector3 Position;
        public float Rotation;
        public float WeaponY;
        public byte RegionSet;
        public bool OnLoad = true;

        public PacketPlayerPosition() { }
        public PacketPlayerPosition(NetIncomingMessage im) { Deserialize(im); }
    }
}
