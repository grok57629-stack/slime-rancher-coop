using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerUpgrade)]
    public class PacketPlayerUpgrade : Packet
    {
        public byte Upgrade;

        public PacketPlayerUpgrade() { }
        public PacketPlayerUpgrade(NetIncomingMessage im) { Deserialize(im); }
    }
}
