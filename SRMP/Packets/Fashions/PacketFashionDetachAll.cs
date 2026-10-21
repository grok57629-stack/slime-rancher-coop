using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.FashionDetachAll)]
    public class PacketFashionDetachAll : Packet
    {
        public byte Type;
        public string IDString;
        public int IDInt;
        public Vector3 Position;
        public Quaternion Rotation;

        public PacketFashionDetachAll() { }
        public PacketFashionDetachAll(NetIncomingMessage im) { Deserialize(im); }
    }
}
