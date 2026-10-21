using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.Oasis)]
    public class PacketOasis : Packet
    {
        public struct OasisData
        {
            public string ID;
            public OasisModel Model;
        }
        public List<OasisData> Oasis { get; set; }

        public PacketOasis() { }
        public PacketOasis(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Oasis.Count);
            foreach (var oasis in Oasis)
            {
                om.Write(oasis.ID);
                om.Write(oasis.Model.isLive);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Oasis = new List<OasisData>();
            int oasisCount = im.ReadInt32();
            for (int i = 0; i < oasisCount; i++)
            {
                var oasisData = new OasisData()
                {
                    ID = im.ReadString(),
                    Model = new OasisModel()
                    {
                        isLive = im.ReadBoolean()
                    }
                };
                Oasis.Add(oasisData);
            }
        }
    }
}
