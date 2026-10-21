using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotAsh)]
    public class PacketLandPlotAsh : Packet
    {
        public string ID;
        public float Amount;

        public PacketLandPlotAsh() { }
        public PacketLandPlotAsh(NetIncomingMessage im) { Deserialize(im); }
    }
}
