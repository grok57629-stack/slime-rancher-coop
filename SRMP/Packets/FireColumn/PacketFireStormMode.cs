using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.FireStormMode)]
    public class PacketFireStormMode : Packet
    {
        public byte Mode;

        public PacketFireStormMode() { }
        public PacketFireStormMode(NetIncomingMessage im) { Deserialize(im); }
    }
}
