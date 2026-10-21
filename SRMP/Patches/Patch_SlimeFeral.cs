using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SlimeFeral), "RegistryUpdate")]
    class SlimeFeral_RegistryUpdate
    {
        static bool Prefix(SlimeFeral __instance)
        {
            if (!Globals.IsMultiplayer) return true;
            var actor = __instance.GetComponentInParent<NetworkActor>();
            return actor != null && actor.IsLocal;
        }
    }

    [HarmonyPatch(typeof(SlimeFeral))]
    [HarmonyPatch("MakeFeral")]
    class SlimeFeral_MakeFeral
    {
        static bool Prefix(SlimeFeral __instance)
        {
            if (!Globals.IsMultiplayer) return true;
            // SetModel and a later live update can both request the same aura.
            if (__instance.model != null && __instance.model.isFeral && __instance.aura != null) return false;
            if (Globals.HandlePacket) return true;

            var netActor = __instance.GetComponentInParent<NetworkActor>();
            if (netActor != null && netActor.IsLocal)
            {
                new PacketActorFeral()
                {
                    ID = netActor.ID,
                    Feral = true,
                    Deagitate = false
                }.Send();
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(SlimeFeral))]
    [HarmonyPatch("MakeNotFeral")]
    class SlimeFeral_MakeNotFeral
    {
        static void Prefix(SlimeFeral __instance, bool deagitate)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var netActor = __instance.GetComponentInParent<NetworkActor>();
            if (netActor != null && netActor.IsLocal)
            {
                new PacketActorFeral()
                {
                    ID = netActor.ID,
                    Feral = false,
                    Deagitate = deagitate
                }.Send();
            }
        }
    }
}
