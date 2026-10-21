using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ExchangeTryAccept)]
    public class PacketExchangeTryAccept : Packet
    {
        public byte Type;
        public ushort ID;

        public PacketExchangeTryAccept() { }
        public PacketExchangeTryAccept(NetIncomingMessage im) { Deserialize(im); }
    }
}
