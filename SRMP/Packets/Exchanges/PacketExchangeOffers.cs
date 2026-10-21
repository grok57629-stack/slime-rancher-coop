using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ExchangeOffers)]
    public class PacketExchangeOffers : Packet
    {
        public struct OfferData
        {
            public byte Type;
            public ExchangeDirector.Offer Offer;
        }
        public List<string> pendingOfferRancherIds { get; set; }
        public List<OfferData> Offers { get; set; }

        public PacketExchangeOffers() { }
        public PacketExchangeOffers(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(pendingOfferRancherIds.Count);
            foreach (var pending in pendingOfferRancherIds)
            {
                om.Write(pending);
            }

            om.Write(Offers.Count);
            foreach (var offerData in Offers)
            {
                om.Write(offerData.Offer.requests.Count);
                foreach (var request in offerData.Offer.requests)
                {
                    om.Write((ushort)request.id);
                    om.Write(request.count);
                    om.Write(request.progress);
                    om.Write((ushort)request.specReward);
                }
                om.Write(offerData.Offer.rewards.Count);
                foreach (var reward in offerData.Offer.rewards)
                {
                    om.Write((ushort)reward.id);
                    om.Write(reward.count);
                    om.Write((ushort)reward.specReward);
                }
                om.Write(offerData.Type);
                om.Write(offerData.Offer.offerId);
                om.Write(offerData.Offer.rancherId);
                om.Write(offerData.Offer.expireTime);
                om.Write(offerData.Offer.earlyExchangeTime);
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

            Offers = new List<OfferData>();
            int offerCount = im.ReadInt32();
            for (int i = 0; i < offerCount; i++)
            {
                var requests = new List<ExchangeDirector.RequestedItemEntry>();
                int requestCount = im.ReadInt32();
                for (int j = 0; j < requestCount; j++)
                {
                    requests.Add(new ExchangeDirector.RequestedItemEntry((Identifiable.Id)im.ReadUInt16(), im.ReadInt32(), im.ReadInt32(), (ExchangeDirector.NonIdentReward)im.ReadUInt16()));
                }
                var rewards = new List<ExchangeDirector.ItemEntry>();
                int rewardCount = im.ReadInt32();
                for (int j = 0; j < rewardCount; j++)
                {
                    rewards.Add(new ExchangeDirector.ItemEntry((Identifiable.Id)im.ReadUInt16(), im.ReadInt32(), (ExchangeDirector.NonIdentReward)im.ReadUInt16()));
                }
                Offers.Add(new OfferData()
                {
                    Type = im.ReadByte(),
                    Offer = new ExchangeDirector.Offer(im.ReadString(), im.ReadString(), im.ReadDouble(), im.ReadDouble(), requests, rewards)
                });
            }
        }
    }
}
