using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
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
    [HarmonyPatch(typeof(Extractor))]
    [HarmonyPatch("StartNewCycleOrDestroy")]
    class Extractor_StartNewCycleOrDestroy
    {
        static void Postfix(Extractor __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var gadget = __instance?.GetComponentInParent<NetworkGadgetSite>();
            if (gadget != null && gadget.IsLocal)
            {
                new PacketGadgetExtractorUpdate()
                {
                    ID = __instance.model.siteId,
                    cycleEndTime = __instance.model.cycleEndTime,
                    cyclesRemaining = __instance.model.cyclesRemaining,
                    nextProduceTime = __instance.model.nextProduceTime,
                    queuedToProduce = __instance.model.queuedToProduce
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(Extractor))]
    [HarmonyPatch("OnInteract")]
    class Extractor_OnInteract
    {
        static void Postfix(Extractor __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGadgetExtractorUpdate()
            {
                ID = __instance.model.siteId,
                cycleEndTime = __instance.model.cycleEndTime,
                cyclesRemaining = __instance.model.cyclesRemaining,
                nextProduceTime = __instance.model.nextProduceTime,
                queuedToProduce = __instance.model.queuedToProduce
            }.Send();
        }
    }

    [HarmonyPatch(typeof(Extractor))]
    [HarmonyPatch("SpawnItem")]
    class Extractor_SpawnItem
    {
        static bool Prefix(Extractor __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var gadget = __instance.GetComponentInParent<NetworkGadgetSite>();
            if (gadget != null && gadget.IsLocal)
            {
                new PacketGadgetExtractorUpdate()
                {
                    ID = __instance.model.siteId,
                    cycleEndTime = __instance.model.cycleEndTime,
                    cyclesRemaining = __instance.model.cyclesRemaining,
                    nextProduceTime = __instance.model.nextProduceTime,
                    queuedToProduce = __instance.model.queuedToProduce
                }.Send();
                return true;
            }
            return false;
        }
    }
}