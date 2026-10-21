using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.DroneStationEnabled)]
    public class PacketDroneStationEnabled : Packet
    {
        public string ID;
        public bool Enabled;

        public PacketDroneStationEnabled() { }
        public PacketDroneStationEnabled(NetIncomingMessage im) { Deserialize(im); }
    }
}
