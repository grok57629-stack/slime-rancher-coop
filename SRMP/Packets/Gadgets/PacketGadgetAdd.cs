using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetAdd)]
    public class PacketGadgetAdd : Packet
    {
        public ushort ID;

        public PacketGadgetAdd() { }
        public PacketGadgetAdd(NetIncomingMessage im) { Deserialize(im); }
    }
}
