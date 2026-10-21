using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldKey)]
    public class PacketWorldKey : Packet
    {
        public bool Added;

        public PacketWorldKey() { }
        public PacketWorldKey(NetIncomingMessage im) { Deserialize(im); }
    }
}
