using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorReproduceTime)]
    public class PacketActorReproduceTime : Packet
    {
        public int ID;
        public double Time;

        public PacketActorReproduceTime() { }
        public PacketActorReproduceTime(NetIncomingMessage im) { Deserialize(im); }
    }
}
