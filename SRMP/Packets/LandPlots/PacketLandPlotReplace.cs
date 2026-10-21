using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotReplace)]
    public class PacketLandPlotReplace : Packet
    {
        public string ID;
        public byte Type;

        public PacketLandPlotReplace() { }
        public PacketLandPlotReplace(NetIncomingMessage im) { Deserialize(im); }
    }
}
