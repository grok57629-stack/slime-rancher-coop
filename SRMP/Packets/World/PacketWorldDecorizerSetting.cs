using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldDecorizerSetting)]
    public class PacketWorldDecorizerSetting : Packet
    {
        public string ID;
        public ushort Selected;

        public PacketWorldDecorizerSetting() { }
        public PacketWorldDecorizerSetting(NetIncomingMessage im) { Deserialize(im); }
    }
}
