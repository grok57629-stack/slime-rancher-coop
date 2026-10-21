using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.RaceTime)]
    public class PacketRaceTime : Packet
    {
        public string ID;
        public float Time;

        public PacketRaceTime() { }
        public PacketRaceTime(NetIncomingMessage im) { Deserialize(im); }
    }
}
