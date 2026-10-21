using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.RaceEnd)]
    public class PacketRaceEnd : Packet
    {
        public string ID;

        public PacketRaceEnd() { }
        public PacketRaceEnd(NetIncomingMessage im) { Deserialize(im); }
    }
}
