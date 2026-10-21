using HarmonyLib;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SpawnResource))]
    [HarmonyPatch("Update")]
    class SpawnResource_Update
    {
        static bool Prefix(SpawnResource __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            var netSpawnResource = __instance.GetComponent<NetworkSpawnResource>();
            if (netSpawnResource != null && netSpawnResource.LandPlot != null)
            {
                return netSpawnResource.LandPlot.IsLocal;
            }
            return (netSpawnResource != null && netSpawnResource.IsLocal);
        }
    }

    [HarmonyPatch(typeof(SpawnResource))]
    [HarmonyPatch("UpdateToTime")]
    class SpawnResource_UpdateToTime
    {
        static bool Prefix(SpawnResource __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netSpawnResource = __instance.GetComponent<NetworkSpawnResource>();
            if (netSpawnResource != null && netSpawnResource.LandPlot != null)
            {
                return netSpawnResource.LandPlot.IsLocal;
            }
            return (netSpawnResource != null && netSpawnResource.IsLocal);
        }
    }
}