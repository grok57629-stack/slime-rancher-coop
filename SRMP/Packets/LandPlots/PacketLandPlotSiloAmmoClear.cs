using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotSiloAmmoClear)]
    public class PacketLandPlotSiloAmmoClear : Packet
    {
        public string ID;
        public byte Type;
        public int Slot;

        public PacketLandPlotSiloAmmoClear() { }
        public PacketLandPlotSiloAmmoClear(NetIncomingMessage im) { Deserialize(im); }
    }
}
