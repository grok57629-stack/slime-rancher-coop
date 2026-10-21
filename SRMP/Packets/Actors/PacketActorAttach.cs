using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorResourceAttach)]
    public class PacketActorResourceAttach : Packet
    {
        public string PlotID;
        public int ID;
        public int ResourceID;
        public int JointIndex;

        public PacketActorResourceAttach() { }
        public PacketActorResourceAttach(NetIncomingMessage im) { Deserialize(im); }
    }
}
