using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.DroneAmmoRemove)]
    public class PacketDroneAmmoRemove : Packet
    {
        public string ID;
        public int Count;

        public PacketDroneAmmoRemove() { }
        public PacketDroneAmmoRemove(NetIncomingMessage im) { Deserialize(im); }
    }
}
