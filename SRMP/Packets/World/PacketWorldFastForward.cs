using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldFastForward)]
    public class PacketWorldFastForward : Packet
    {
        public double FastForwardTill;

        public PacketWorldFastForward() { }
        public PacketWorldFastForward(NetIncomingMessage im) { Deserialize(im); }
    }
}
