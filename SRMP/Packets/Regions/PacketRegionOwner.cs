using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.RegionOwner)]
    public class PacketRegionOwner : Packet
    {
        public int ID;
        public byte Owner;

        public PacketRegionOwner() { }
        public PacketRegionOwner(NetIncomingMessage im) { Deserialize(im); }
    }
}
