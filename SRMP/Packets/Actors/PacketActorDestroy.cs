using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorDestroy)]
    public class PacketActorDestroy : Packet
    {
        public int ID;

        public PacketActorDestroy() { }
        public PacketActorDestroy(NetIncomingMessage im) { Deserialize(im); }
    }
}
