using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotCollect)]
    public class PacketLandPlotCollect : Packet
    {
        public string ID;
        public double collectorNextTime;
        public double forceCollectUntil;
        public double endCollectAt;

        public PacketLandPlotCollect() { }
        public PacketLandPlotCollect(NetIncomingMessage im) { Deserialize(im); }
    }
}
