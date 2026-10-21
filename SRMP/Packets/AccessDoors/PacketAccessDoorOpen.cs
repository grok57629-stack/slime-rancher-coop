using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.AccessDoorOpen)]
    public class PacketAccessDoorOpen : Packet
    {
        public string ID;
        public byte State;

        public PacketAccessDoorOpen() { }
        public PacketAccessDoorOpen(NetIncomingMessage im) { Deserialize(im); }
    }
}
