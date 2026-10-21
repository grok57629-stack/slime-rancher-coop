using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorOwner)]
    public class PacketActorOwner : Packet
    {
        public int ID;
        public byte Owner;
        public bool Interaction;

        public PacketActorOwner() { }
        public PacketActorOwner(NetIncomingMessage im) { Deserialize(im); }
    }
}
