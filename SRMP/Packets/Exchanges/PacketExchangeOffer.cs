using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.ExchangeOffer)]
    public class PacketExchangeOffer : Packet
    {
        public byte Type;
        public ExchangeDirector.Offer Offer { get; set; }

        public PacketExchangeOffer() { }
        public PacketExchangeOffer(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Offer.requests.Count);
            foreach (var request in Offer.requests)
            {
                om.Write((ushort)request.id);
                om.Write(request.count);
                om.Write(request.progress);
                om.Write((ushort)request.specReward);
            }
            om.Write(Offer.rewards.Count);
            foreach (var reward in Offer.rewards)
            {
                om.Write((ushort)reward.id);
                om.Write(reward.count);
                om.Write((ushort)reward.specReward);
            }
            om.Write(Offer.offerId);
            om.Write(Offer.rancherId);
            om.Write(Offer.expireTime);
            om.Write(Offer.earlyExchangeTime);
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

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
            Offer = new ExchangeDirector.Offer(im.ReadString(), im.ReadString(), im.ReadDouble(), im.ReadDouble(), requests, rewards);
        }
    }
}
