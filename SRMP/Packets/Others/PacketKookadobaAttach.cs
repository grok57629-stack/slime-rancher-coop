using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.KookadobaAttach)]
    public class PacketKookadobaAttach : Packet
    {
        public int ID;
        public int ActorID;

        public PacketKookadobaAttach() { }
        public PacketKookadobaAttach(NetIncomingMessage im) { Deserialize(im); }
    }
}
