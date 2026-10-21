using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GlobalFX)]
    public class PacketGlobalFX : Packet
    {
        public string Name;
        public Vector3 Position;

        public PacketGlobalFX() { }
        public PacketGlobalFX(NetIncomingMessage im) { Deserialize(im); }
    }
}
