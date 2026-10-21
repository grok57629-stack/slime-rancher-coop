using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Packets;
using UnityEngine;
using SRMultiplayer.Networking;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(Reproduce))]
    [HarmonyPatch("RegistryUpdate")]
    class Reproduce_RegistryUpdate
    {
        static bool Prefix(Reproduce __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netActor = __instance.GetComponentInParent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }

    [HarmonyPatch(typeof(Reproduce))]
    [HarmonyPatch("ReproducePeriod")]
    class Reproduce_ReproducePeriod
    {
        static void Postfix(Reproduce __instance, ref float __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var netActor = __instance.GetComponentInParent<NetworkActor>();
            if (netActor != null)
            {
                new PacketActorReproduceTime()
                {
                    ID = netActor.ID,
                    Time = SRSingleton<SceneContext>.Instance.TimeDirector.HoursFromNow(__result)
                }.Send();
            }
        }
    }
}