using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.DroneAmmoClear)]
    public class PacketDroneAmmoClear : Packet
    {
        public string ID;

        public PacketDroneAmmoClear() { }
        public PacketDroneAmmoClear(NetIncomingMessage im) { Deserialize(im); }
    }
}
