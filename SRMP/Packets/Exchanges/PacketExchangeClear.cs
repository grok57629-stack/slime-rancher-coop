using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ExchangeClear)]
    public class PacketExchangeClear : Packet
    {
        public byte Type;

        public PacketExchangeClear() { }
        public PacketExchangeClear(NetIncomingMessage im) { Deserialize(im); }
    }
}
