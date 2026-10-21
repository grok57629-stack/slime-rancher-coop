using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.OasisLive)]
    public class PacketOasisLive : Packet
    {
        public string ID;

        public PacketOasisLive() { }
        public PacketOasisLive(NetIncomingMessage im) { Deserialize(im); }
    }
}
