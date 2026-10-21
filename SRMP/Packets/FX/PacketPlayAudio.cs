using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PlayAudio)]
    public class PacketPlayAudio : Packet
    {
        public string CueName;
        public Vector3 Position;
        public bool Loop;

        public PacketPlayAudio() { }
        public PacketPlayAudio(NetIncomingMessage im) { Deserialize(im); }
    }
}
