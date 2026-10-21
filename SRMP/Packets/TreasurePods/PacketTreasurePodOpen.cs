using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.TreasurePodOpen)]
    public class PacketTreasurePodOpen : Packet
    {
        public string ID;

        public PacketTreasurePodOpen() { }
        public PacketTreasurePodOpen(NetIncomingMessage im) { Deserialize(im); }
    }
}
