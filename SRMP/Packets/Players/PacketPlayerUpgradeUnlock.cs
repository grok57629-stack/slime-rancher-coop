using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerUpgradeUnlock)]
    public class PacketPlayerUpgradeUnlock : Packet
    {
        public byte Upgrade;

        public PacketPlayerUpgradeUnlock() { }
        public PacketPlayerUpgradeUnlock(NetIncomingMessage im) { Deserialize(im); }
    }
}
