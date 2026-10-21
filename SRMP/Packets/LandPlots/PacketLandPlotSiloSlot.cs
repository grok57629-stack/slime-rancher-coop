using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotSiloSlot)]
    public class PacketLandPlotSiloSlot : Packet
    {
        public string ID;
        public int Slot;
        public int ActivatorID;

        public PacketLandPlotSiloSlot() { }
        public PacketLandPlotSiloSlot(NetIncomingMessage im) { Deserialize(im); }
    }
}
