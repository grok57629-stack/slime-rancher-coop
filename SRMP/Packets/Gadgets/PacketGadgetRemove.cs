using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetRemove)]
    public class PacketGadgetRemove : Packet
    {
        public string ID;

        public PacketGadgetRemove() { }
        public PacketGadgetRemove(NetIncomingMessage im) { Deserialize(im); }
    }
}
