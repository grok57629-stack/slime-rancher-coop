using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldSelectPalette)]
    public class PacketWorldSelectPalette : Packet
    {
        public byte Type;
        public ushort Pal;

        public PacketWorldSelectPalette() { }
        public PacketWorldSelectPalette(NetIncomingMessage im) { Deserialize(im); }
    }
}
