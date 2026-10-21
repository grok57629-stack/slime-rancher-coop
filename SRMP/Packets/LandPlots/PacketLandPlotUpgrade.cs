using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotUpgrade)]
    public class PacketLandPlotUpgrade : Packet
    {
        public string ID;
        public byte Upgrade;

        public PacketLandPlotUpgrade() { }
        public PacketLandPlotUpgrade(NetIncomingMessage im) { Deserialize(im); }
    }
}
