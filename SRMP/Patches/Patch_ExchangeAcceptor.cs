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
    [HarmonyPatch(typeof(ExchangeAcceptor))]
    [HarmonyPatch("OnTriggerEnter")]
    class ExchangeAcceptor_OnTriggerEnter
    {
        static bool Prefix(ExchangeAcceptor __instance, Collider col)
        {
            if (!Globals.IsMultiplayer) return true;

            var netActor = col.GetComponent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }

    [HarmonyPatch(typeof(ExchangeRewardItemEntryUI))]
    [HarmonyPatch("SetEntry")]
    class ExchangeRewardItemEntryUI_SetEntry
    {
        static bool Prefix(ExchangeRewardItemEntryUI __instance, ExchangeDirector.ItemEntry entry)
        {
            if (entry == null)
            {
                __instance.gameObject.SetActive(false);
                return false;
            }
            __instance.gameObject.SetActive(true);
            if (entry.specReward != ExchangeDirector.NonIdentReward.NONE)
            {
                __instance.icon.sprite = SRSingleton<SceneContext>.Instance.ExchangeDirector.GetSpecRewardIcon(entry.specReward);
                __instance.amountText.text = __instance.GetCountDisplayForReward(entry.specReward);
                return false;
            }
            __instance.icon.sprite = SRSingleton<GameContext>.Instance.LookupDirector.GetIcon(entry.id);
            __instance.amountText.text = entry.count.ToString();
            return false;
        }
    }

    [HarmonyPatch(typeof(ExchangeProgressItemEntryUI))]
    [HarmonyPatch("SetEntry")]
    class ExchangeProgressItemEntryUI_SetEntry
    {
        static bool Prefix(ExchangeProgressItemEntryUI __instance, ExchangeDirector.RequestedItemEntry entry)
        {
            if (entry == null)
            {
                __instance.gameObject.SetActive(false);
                return false;
            }
            __instance.gameObject.SetActive(true);
            if (entry.specReward != ExchangeDirector.NonIdentReward.NONE)
            {
                __instance.icon.sprite = SRSingleton<SceneContext>.Instance.ExchangeDirector.GetSpecRewardIcon(entry.specReward);
            }
            else
            {
                __instance.icon.sprite = SRSingleton<GameContext>.Instance.LookupDirector.GetIcon(entry.id);
            }
            __instance.progressText.text = SRSingleton<GameContext>.Instance.MessageDirector.GetBundle("ui").Get("l.exchange_progress", new object[]
            {
            entry.progress,
            entry.count
            });
            return false;
        }
    }
}