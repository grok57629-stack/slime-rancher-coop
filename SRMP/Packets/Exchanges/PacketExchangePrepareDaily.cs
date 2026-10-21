using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ExchangePrepareDaily)]
    public class PacketExchangePrepareDaily : Packet
    {
        public List<string> pendingOfferRancherIds { get; set; }

        public PacketExchangePrepareDaily() { }
        public PacketExchangePrepareDaily(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(pendingOfferRancherIds.Count);
            foreach (var pending in pendingOfferRancherIds)
            {
                om.Write(pending);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            pendingOfferRancherIds = new List<string>();

            int pendingCount = im.ReadInt32();
            for (int i = 0; i < pendingCount; i++)
            {
                pendingOfferRancherIds.Add(im.ReadString());
            }
        }
    }
}
