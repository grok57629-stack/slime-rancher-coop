using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldMapUnlock)]
    public class PacketWorldMapUnlock : Packet
    {
        public byte Zone;

        public PacketWorldMapUnlock() { }
        public PacketWorldMapUnlock(NetIncomingMessage im) { Deserialize(im); }
    }
}
