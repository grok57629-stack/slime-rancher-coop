using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetRotation)]
    public class PacketGadgetRotation : Packet
    {
        public string ID;
        public float Rotation;

        public PacketGadgetRotation() { }
        public PacketGadgetRotation(NetIncomingMessage im) { Deserialize(im); }
    }
}
