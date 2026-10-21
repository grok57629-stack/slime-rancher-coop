using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldProgress)]
    public class PacketWorldProgress : Packet
    {
        public ushort Type;
        public int Amount;

        public PacketWorldProgress() { }
        public PacketWorldProgress(NetIncomingMessage im) { Deserialize(im); }
    }
}
