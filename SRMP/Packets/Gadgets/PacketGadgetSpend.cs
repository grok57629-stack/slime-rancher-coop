using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetSpend)]
    public class PacketGadgetSpend : Packet
    {
        public ushort ID;

        public PacketGadgetSpend() { }
        public PacketGadgetSpend(NetIncomingMessage im) { Deserialize(im); }
    }
}
