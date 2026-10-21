using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GingerAttach)]
    public class PacketGingerAttach : Packet
    {
        public string ID;
        public int ActorID;

        public PacketGingerAttach() { }
        public PacketGingerAttach(NetIncomingMessage im) { Deserialize(im); }
    }
}
