using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotSiloAmmoRemove)]
    public class PacketLandPlotSiloAmmoRemove : Packet
    {
        public string ID;
        public byte Type;
        public int Count;
        public int Slot;

        public PacketLandPlotSiloAmmoRemove() { }
        public PacketLandPlotSiloAmmoRemove(NetIncomingMessage im) { Deserialize(im); }
    }
}
