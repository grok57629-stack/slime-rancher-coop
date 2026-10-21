using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetEchoNetTime)]
    public class PacketGadgetEchoNetTime : Packet
    {
        public string ID;

        public PacketGadgetEchoNetTime() { }
        public PacketGadgetEchoNetTime(NetIncomingMessage im) { Deserialize(im); }
    }
}
