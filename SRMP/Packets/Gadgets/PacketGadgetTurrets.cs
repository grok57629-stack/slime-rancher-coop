using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetTurrets)]
    public class PacketGadgetTurrets : Packet
    {
        public struct TurretData
        {
            public int Index;
            public Quaternion Rotation;
        }
        public string ID;
        public List<TurretData> Turrets { get; set; }

        public PacketGadgetTurrets() { }
        public PacketGadgetTurrets(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Turrets.Count);
            foreach(var turret in Turrets)
            {
                om.Write(turret.Index);
                om.Write(turret.Rotation);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Turrets = new List<TurretData>();
            int turretCount = im.ReadInt32();
            for(int i = 0; i < turretCount; i++)
            {
                Turrets.Add(new TurretData()
                {
                    Index = im.ReadInt32(),
                    Rotation = im.ReadQuaternion()
                });
            }
        }
    }
}
