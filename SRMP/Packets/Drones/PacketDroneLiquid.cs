using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.DroneLiquid)]
    public class PacketDroneLiquid : Packet
    {
        public string ID;

        public PacketDroneLiquid() { }
        public PacketDroneLiquid(NetIncomingMessage im) { Deserialize(im); }
    }
}
