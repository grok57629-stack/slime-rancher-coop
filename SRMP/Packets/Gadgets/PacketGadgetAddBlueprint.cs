using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetAddBlueprint)]
    public class PacketGadgetAddBlueprint : Packet
    {
        public ushort ID;

        public PacketGadgetAddBlueprint() { }
        public PacketGadgetAddBlueprint(NetIncomingMessage im) { Deserialize(im); }
    }
}
