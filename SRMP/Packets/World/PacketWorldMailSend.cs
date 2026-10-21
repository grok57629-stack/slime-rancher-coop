using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldMailSend)]
    public class PacketWorldMailSend : Packet
    {
        public byte Type;
        public string Key;

        public PacketWorldMailSend() { }
        public PacketWorldMailSend(NetIncomingMessage im) { Deserialize(im); }
    }
}
