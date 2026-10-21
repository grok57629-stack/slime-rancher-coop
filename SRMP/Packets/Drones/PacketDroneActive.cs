using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.DroneActive)]
    public class PacketDroneActive : Packet
    {
        public string ID;
        public bool Enabled;

        public PacketDroneActive() { }
        public PacketDroneActive(NetIncomingMessage im) { Deserialize(im); }
    }
}
