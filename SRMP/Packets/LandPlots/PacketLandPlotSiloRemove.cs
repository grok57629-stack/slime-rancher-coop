using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotSiloRemove)]
    public class PacketLandPlotSiloRemove : Packet
    {
        public string ID;
        public ushort Ident;
        public int Slot;
        public byte SiloType;
        public byte CatcherType;

        public PacketLandPlotSiloRemove() { }
        public PacketLandPlotSiloRemove(NetIncomingMessage im) { Deserialize(im); }
    }
}
