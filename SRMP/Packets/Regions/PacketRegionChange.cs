using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.RegionChange)]
    public class PacketRegionChange : Packet
    {
        public int ID;
        public bool Load;

        public PacketRegionChange() { }
        public PacketRegionChange(NetIncomingMessage im) { Deserialize(im); }
    }
}
