using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldSwitchActivate)]
    public class PacketWorldSwitchActivate : Packet
    {
        public string ID;
        public byte State;

        public PacketWorldSwitchActivate() { }
        public PacketWorldSwitchActivate(NetIncomingMessage im) { Deserialize(im); }
    }
}
