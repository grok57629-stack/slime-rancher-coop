using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerLoaded)]
    public class PacketPlayerLoaded : Packet
    {
        public byte ID;

        public PacketPlayerLoaded() { }
        public PacketPlayerLoaded(NetIncomingMessage im) { Deserialize(im); }
    }
}
