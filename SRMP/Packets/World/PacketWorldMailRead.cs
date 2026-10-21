using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldMailRead)]
    public class PacketWorldMailRead : Packet
    {
        public byte Type;
        public string Key;

        public PacketWorldMailRead() { }
        public PacketWorldMailRead(NetIncomingMessage im) { Deserialize(im); }
    }
}
