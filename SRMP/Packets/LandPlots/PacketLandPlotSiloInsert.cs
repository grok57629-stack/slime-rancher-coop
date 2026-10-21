using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotSiloInsert)]
    public class PacketLandPlotSiloInsert : Packet
    {
        public string ID;
        public ushort Ident;
        public int Slot;
        public byte CatcherType;
        public int Count;
        public bool Overflow;

        public PacketLandPlotSiloInsert() { }
        public PacketLandPlotSiloInsert(NetIncomingMessage im) { Deserialize(im); }
    }
}
