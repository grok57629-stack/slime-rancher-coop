using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.AccessDoors)]
    public class PacketAccessDoors : Packet
    {
        public struct AccessDoorData
        {
            public string ID;
            public AccessDoorModel Model;
        }
        public List<AccessDoorData> Doors { get; set; }

        public PacketAccessDoors() { }
        public PacketAccessDoors(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Doors.Count);
            foreach(var door in Doors)
            {
                om.Write(door.ID);
                om.Write((byte)door.Model.state);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Doors = new List<AccessDoorData>();
            int doorCount = im.ReadInt32();
            for(int i = 0; i < doorCount; i++)
            {
                Doors.Add(new AccessDoorData()
                {
                    ID = im.ReadString(),
                    Model = new AccessDoorModel()
                    {
                        state = (AccessDoor.State)im.ReadByte()
                    }
                });
            }
        }
    }
}
