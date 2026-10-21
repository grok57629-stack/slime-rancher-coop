using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.GadgetRefinerySpend)]
    public class PacketGadgetRefinerySpend : Packet
    {
        public Dictionary<ushort, int> Amounts { get; set; }

        public PacketGadgetRefinerySpend() { }
        public PacketGadgetRefinerySpend(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Amounts.Count);
            foreach(var data in Amounts)
            {
                om.Write(data.Key);
                om.Write(data.Value);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Amounts = new Dictionary<ushort, int>();
            int dataCount = im.ReadInt32();
            for(int i = 0; i < dataCount; i++)
            {
                Amounts.Add(im.ReadUInt16(), im.ReadInt32());
            }
        }
    }
}
