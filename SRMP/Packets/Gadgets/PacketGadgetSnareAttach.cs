using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetSnareAttach)]
    public class PacketGadgetSnareAttach : Packet
    {
        public string ID;
        public ushort Ident;

        public PacketGadgetSnareAttach() { }
        public PacketGadgetSnareAttach(NetIncomingMessage im) { Deserialize(im); }
    }
}
