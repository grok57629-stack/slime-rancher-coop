using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.TreasurePods)]
    public class PacketTreasurePods : Packet
    {
        public struct TreasurePodData
        {
            public string ID;
            public TreasurePodModel Model;
        }
        public List<TreasurePodData> TreasurePods { get; set; }

        public PacketTreasurePods() { }
        public PacketTreasurePods(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(TreasurePods.Count);
            foreach(var pod in TreasurePods)
            {
                om.Write(pod.ID);
                om.Write((byte)pod.Model.state);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            TreasurePods = new List<TreasurePodData>();
            int podCount = im.ReadInt32();
            for(int i = 0; i < podCount; i++)
            {
                TreasurePods.Add(new TreasurePodData()
                {
                    ID = im.ReadString(),
                    Model = new TreasurePodModel()
                    {
                        state = (TreasurePod.State)im.ReadByte()
                    }
                });
            }
        }
    }
}
