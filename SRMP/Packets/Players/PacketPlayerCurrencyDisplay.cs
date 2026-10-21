using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerCurrencyDisplay)]
    public class PacketPlayerCurrencyDisplay : Packet
    {
        public bool IsNull;
        public int Currency;

        public PacketPlayerCurrencyDisplay() { }
        public PacketPlayerCurrencyDisplay(NetIncomingMessage im) { Deserialize(im); }
    }
}
