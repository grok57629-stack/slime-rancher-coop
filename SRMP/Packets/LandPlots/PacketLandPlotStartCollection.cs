using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotStartCollection)]
    public class PacketLandPlotStartCollection : Packet
    {
        public string ID;

        public PacketLandPlotStartCollection() { }
        public PacketLandPlotStartCollection(NetIncomingMessage im) { Deserialize(im); }
    }
}
