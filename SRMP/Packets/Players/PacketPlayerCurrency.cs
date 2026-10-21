using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerCurrency)]
    public class PacketPlayerCurrency : Packet
    {
        public int Total;
        public int Adjust;
        public short Type;
        public byte Origin;

        public PacketPlayerCurrency() { }
        public PacketPlayerCurrency(NetIncomingMessage im) { Deserialize(im); }
    }
}
