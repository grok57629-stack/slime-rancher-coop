using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.RaceTrigger)]
    public class PacketRaceTrigger : Packet
    {
        public int ID;

        public PacketRaceTrigger() { }
        public PacketRaceTrigger(NetIncomingMessage im) { Deserialize(im); }
    }
}
