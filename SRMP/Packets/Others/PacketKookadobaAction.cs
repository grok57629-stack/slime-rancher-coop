using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.KookadobaAction)]
    public class PacketKookadobaAction : Packet
    {
        public int ID;
        public bool Grow;
        public bool Harvest;

        public PacketKookadobaAction() { }
        public PacketKookadobaAction(NetIncomingMessage im) { Deserialize(im); }
    }
}
