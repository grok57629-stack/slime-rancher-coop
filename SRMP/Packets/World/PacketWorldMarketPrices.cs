using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldMarketPrices)]
    public class PacketWorldMarketPrices : Packet
    {
        public Dictionary<ushort, EconomyDirector.CurrValueEntry> Prices { get; set; }
        public Dictionary<ushort, float> Saturation { get; set; }

        public PacketWorldMarketPrices() { }
        public PacketWorldMarketPrices(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Prices.Count);
            foreach (var data in Prices)
            {
                om.Write(data.Key);
                om.Write(data.Value.currValue);
                om.Write(data.Value.prevValue);
            }
            om.Write(Saturation.Count);
            foreach (var data in Saturation)
            {
                om.Write(data.Key);
                om.Write(data.Value);
            }
        }
        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Prices = new Dictionary<ushort, EconomyDirector.CurrValueEntry>();
            Saturation = new Dictionary<ushort, float>();

            int count = 0;
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Prices.Add(im.ReadUInt16(), new EconomyDirector.CurrValueEntry(0, im.ReadFloat(), im.ReadFloat(), 0));
            }
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Saturation.Add(im.ReadUInt16(), im.ReadFloat());
            }
        }
    }
}
