using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetSnareGordo)]
    public class PacketGadgetSnareGordo : Packet
    {
        public string ID;
        public ushort Ident;

        public PacketGadgetSnareGordo() { }
        public PacketGadgetSnareGordo(NetIncomingMessage im) { Deserialize(im); }
    }
}
