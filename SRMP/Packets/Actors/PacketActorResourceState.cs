using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ActorResourceState)]
    public class PacketActorResourceState : Packet
    {
        public int ID;
        public byte State;
        public double ProgressTime;
        public bool PreparingToRelease;

        public PacketActorResourceState() { }
        public PacketActorResourceState(NetIncomingMessage im) { Deserialize(im); }
    }
}
