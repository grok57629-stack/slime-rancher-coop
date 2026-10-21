using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldCredits)]
    public class PacketWorldCredits : Packet
    {
        public PacketWorldCredits() { }
        public PacketWorldCredits(NetIncomingMessage im) { Deserialize(im); }
    }
}