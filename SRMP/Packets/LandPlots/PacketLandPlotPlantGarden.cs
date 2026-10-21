using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotPlantGarden)]
    public class PacketLandPlotPlantGarden : Packet
    {
        public string ID;
        public ushort Type;
        public byte AttachedID;
        public ushort AttachedResourceID;

        public PacketLandPlotPlantGarden() { }
        public PacketLandPlotPlantGarden(NetIncomingMessage im) { Deserialize(im); }
    }
}
