using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.IncinerateFX)]
    public class PacketIncinerateFX : Packet
    {
        public string ID;
        public bool Small;
        public bool Ash;
        public Vector3 Position;
        public Quaternion Rotation;

        public PacketIncinerateFX() { }
        public PacketIncinerateFX(NetIncomingMessage im) { Deserialize(im); }
    }
}
