using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotFeederSpeed)]
    public class PacketLandPlotFeederSpeed : Packet
    {
        public string ID;
        public byte Speed;

        public PacketLandPlotFeederSpeed() { }
        public PacketLandPlotFeederSpeed(NetIncomingMessage im) { Deserialize(im); }
    }
}
