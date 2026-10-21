using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GordoEat)]
    public class PacketGordoEat : Packet
    {
        public string ID;
        public bool Request;
        public int FoodID;
        public Vector3 Position;
        public Quaternion Rotation;
        public int Count; // Absolute host state; -1 means the Gordo has burst.
        public bool Favorite;

        public PacketGordoEat() { }
        public PacketGordoEat(NetIncomingMessage im) { Deserialize(im); }
    }
}
