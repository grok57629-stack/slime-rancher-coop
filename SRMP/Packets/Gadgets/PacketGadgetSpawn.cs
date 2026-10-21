using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetSpawn)]
    public class PacketGadgetSpawn : Packet
    {
        public string ID;
        public ushort Ident;
        public float Rotation;

        public PacketGadgetSpawn() { }
        public PacketGadgetSpawn(NetIncomingMessage im) { Deserialize(im); }
    }
}
