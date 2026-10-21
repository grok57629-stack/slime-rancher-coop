using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PediaShowPopup)]
    public class PacketPediaShowPopup : Packet
    {
        public ushort ID;

        public PacketPediaShowPopup() { }
        public PacketPediaShowPopup(NetIncomingMessage im) { Deserialize(im); }
    }
}
