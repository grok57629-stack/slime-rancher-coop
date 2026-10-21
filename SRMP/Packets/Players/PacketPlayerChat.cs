using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayerChat)]
    public class PacketPlayerChat : Packet
    {
        public string message;

        public PacketPlayerChat() { }
        public PacketPlayerChat(NetIncomingMessage im) { Deserialize(im); }
    }
}
