using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.DronePosition)]
    public class PacketDronePosition : Packet
    {
        public string ID;
        public Vector3 Position;
        public Quaternion Rotation;

        public PacketDronePosition() { }
        public PacketDronePosition(NetIncomingMessage im) { Deserialize(im); }
    }
}
