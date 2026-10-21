using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(FirestormActivator))]
    [HarmonyPatch("Update")]
    class FirestormActivator_Update
    {
        static bool Prefix(FirestormActivator __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            if (Globals.IsServer)
            {
                return true;
            }
            __instance.MaybeUpdatePlayerState();
            return false;
        }
    }

    [HarmonyPatch(typeof(FirestormActivator))]
    [HarmonyPatch("MaybeShutdownFirestorm")]
    class FirestormActivator_MaybeShutdownFirestorm
    {
        static void Prefix(FirestormActivator __instance)
        {
            if (!Globals.IsMultiplayer) return;

            if (__instance.timeDir.HasReached(__instance.worldModel.endFirestormTime))
            {
                new PacketFireStormMode()
                {
                    Mode = (byte)FirestormActivator.Mode.IDLE
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(FirestormActivator))]
    [HarmonyPatch("MaybeStartFirestorm")]
    class FirestormActivator_MaybeStartFirestorm
    {
        static void Prefix(FirestormActivator __instance)
        {
            if (!Globals.IsMultiplayer) return;

            if (__instance.timeDir.HasReached(__instance.worldModel.nextFirestormTime))
            {
                new PacketFireStormMode()
                {
                    Mode = (byte)FirestormActivator.Mode.PREPARING
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(FirestormActivator))]
    [HarmonyPatch("MaybeTriggerNearbyColumns")]
    class FirestormActivator_MaybeTriggerNearbyColumns
    {
        static void Prefix(FirestormActivator __instance)
        {
            if (!Globals.IsMultiplayer) return;

            if (__instance.timeDir.HasReached(__instance.worldModel.nextFirecolumnTime) && !__instance.timeDir.HasReached(__instance.worldModel.endFirecolumnsTime))
            {
                new PacketFireStormMode()
                {
                    Mode = (byte)FirestormActivator.Mode.ACTIVE
                }.Send();
            }
        }
    }
}