using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.FashionAttach)]
    public class PacketFashionAttach : Packet
    {
        public byte Type;
        public string IDString;
        public int IDInt;
        public ushort Fashion;

        public PacketFashionAttach() { }
        public PacketFashionAttach(NetIncomingMessage im) { Deserialize(im); }
    }
}
