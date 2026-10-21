using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GingerAction)]
    public class PacketGingerAction : Packet
    {
        public string ID;
        public bool Grow;
        public bool Harvest;

        public PacketGingerAction() { }
        public PacketGingerAction(NetIncomingMessage im) { Deserialize(im); }
    }
}
