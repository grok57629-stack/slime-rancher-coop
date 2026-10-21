using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerLeft)]
    public class PacketPlayerLeft : Packet
    {
        public byte ID;

        public PacketPlayerLeft() { }
        public PacketPlayerLeft(NetIncomingMessage im) { Deserialize(im); }
    }
}
