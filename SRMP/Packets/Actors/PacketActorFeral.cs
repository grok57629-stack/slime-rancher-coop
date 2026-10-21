using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorFeral)]
    public class PacketActorFeral : Packet
    {
        public int ID;
        public bool Feral;
        public bool Deagitate;

        public PacketActorFeral() { }
        public PacketActorFeral(NetIncomingMessage im) { Deserialize(im); }
    }
}
