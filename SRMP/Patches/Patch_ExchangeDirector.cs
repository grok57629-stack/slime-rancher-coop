using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(ExchangeDirector))]
    [HarmonyPatch("Update")]
    class ExchangeDirector_Update
    {
        static bool Prefix(ExchangeDirector __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            if (Globals.IsServer)
            {
                ExchangeDirector.Offer offer = __instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.GENERAL) ? __instance.worldModel.currOffers[ExchangeDirector.OfferType.GENERAL] : null;
                if (offer != null && __instance.timeDir.HasReached(offer.expireTime))
                {
                    __instance.ClearOffer(ExchangeDirector.OfferType.GENERAL);
                }
                if (offer == null && __instance.timeDir.HasReached(__instance.worldModel.nextDailyOfferCreateTime))
                {
                    __instance.worldModel.nextDailyOfferCreateTime = __instance.GetNextDailyOfferCreateTime();
                    __instance.PrepareNextDailyOffer();
                    __instance.OfferDidChange();
                    
                    new PacketExchangePrepareDaily()
                    {
                        pendingOfferRancherIds = __instance.worldModel.pendingOfferRancherIds
                    }.Send();
                }
                if (!__instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.OGDEN_RECUR) && (__instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.OGDEN) || (float)__instance.progressDir.GetProgress(ProgressDirector.ProgressType.OGDEN_REWARDS) >= 3f))
                {
                    __instance.worldModel.currOffers[ExchangeDirector.OfferType.OGDEN_RECUR] = __instance.CreateOgdenRecurOffer();
                    __instance.OfferDidChange();

                    new PacketExchangeOffer()
                    {
                        Type = (byte)ExchangeDirector.OfferType.OGDEN_RECUR,
                        Offer = __instance.worldModel.currOffers[ExchangeDirector.OfferType.OGDEN_RECUR]
                    }.Send();
                }
                if (!__instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.MOCHI_RECUR) && (__instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.MOCHI) || (float)__instance.progressDir.GetProgress(ProgressDirector.ProgressType.MOCHI_REWARDS) >= 3f))
                {
                    __instance.worldModel.currOffers[ExchangeDirector.OfferType.MOCHI_RECUR] = __instance.CreateMochiRecurOffer();
                    __instance.OfferDidChange();

                    new PacketExchangeOffer()
                    {
                        Type = (byte)ExchangeDirector.OfferType.MOCHI_RECUR,
                        Offer = __instance.worldModel.currOffers[ExchangeDirector.OfferType.MOCHI_RECUR]
                    }.Send();
                }
                if (!__instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.VIKTOR_RECUR) && (__instance.worldModel.currOffers.ContainsKey(ExchangeDirector.OfferType.VIKTOR) || (float)__instance.progressDir.GetProgress(ProgressDirector.ProgressType.VIKTOR_REWARDS) >= 3f))
                {
                    __instance.worldModel.currOffers[ExchangeDirector.OfferType.VIKTOR_RECUR] = __instance.CreateViktorRecurOffer();
                    __instance.OfferDidChange();

                    new PacketExchangeOffer()
                    {
                        Type = (byte)ExchangeDirector.OfferType.VIKTOR_RECUR,
                        Offer = __instance.worldModel.currOffers[ExchangeDirector.OfferType.VIKTOR_RECUR]
                    }.Send();
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ExchangeDirector))]
    [HarmonyPatch("MaybeStartNext")]
    class ExchangeDirector_MaybeStartNext
    {
        static void Postfix(ExchangeDirector __instance, ref bool __result, ExchangeDirector.OfferType offerType)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                ExchangeDirector.ProgressOfferEntry progressEntry = __instance.GetProgressEntry(offerType);
                new PacketExchangeOffer()
                {
                    Type = (byte)progressEntry.specialOfferType,
                    Offer = __instance.worldModel.currOffers[progressEntry.specialOfferType]
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(ExchangeDirector))]
    [HarmonyPatch("ClearOffer")]
    class ExchangeDirector_ClearOffer
    {
        static void Postfix(ExchangeDirector __instance, ExchangeDirector.OfferType type)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketExchangeClear()
            {
                Type = (byte)type
            }.Send();
        }
    }

    [HarmonyPatch(typeof(ExchangeDirector))]
    [HarmonyPatch("SelectDailyOffer")]
    class ExchangeDirector_SelectDailyOffer
    {
        static void Postfix(ExchangeDirector __instance, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                new PacketExchangeOffer()
                {
                    Type = (byte)ExchangeDirector.OfferType.GENERAL,
                    Offer = __instance.worldModel.currOffers[ExchangeDirector.OfferType.GENERAL]
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(ExchangeDirector.Offer))]
    [HarmonyPatch("TryAccept")]
    class ExchangeDirector_TryAccept
    {
        static void Postfix(ExchangeDirector.Offer __instance, ref bool __result, Identifiable.Id id, ExchangeDirector.Awarder[] awarders, ExchangeDirector.OfferType offerType)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                new PacketExchangeTryAccept()
                {
                    Type = (byte)offerType,
                    ID = (ushort)id
                }.Send();
            }
        }
    }
}