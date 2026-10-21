using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.DroneAnimation)]
    public class PacketDroneAnimation : Packet
    {
        public string ID;
        public byte Anim;

        public PacketDroneAnimation() { }
        public PacketDroneAnimation(NetIncomingMessage im) { Deserialize(im); }
    }
}
