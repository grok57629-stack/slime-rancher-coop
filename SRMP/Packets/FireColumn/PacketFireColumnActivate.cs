using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.FireColumnActivate)]
    public class PacketFireColumnActivate : Packet
    {
        public int ID;

        public PacketFireColumnActivate() { }
        public PacketFireColumnActivate(NetIncomingMessage im) { Deserialize(im); }
    }
}
