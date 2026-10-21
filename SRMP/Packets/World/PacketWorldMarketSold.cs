using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldMarketSold)]
    public class PacketWorldMarketSold : Packet
    {
        public ushort Ident;
        public int Count;

        public PacketWorldMarketSold() { }
        public PacketWorldMarketSold(NetIncomingMessage im) { Deserialize(im); }
    }
}
