using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.RaceActivate)]
    public class PacketRaceActivate : Packet
    {
        public string ID;

        public PacketRaceActivate() { }
        public PacketRaceActivate(NetIncomingMessage im) { Deserialize(im); }
    }
}
