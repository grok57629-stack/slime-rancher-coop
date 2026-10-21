using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer;
using SRMultiplayer.Networking;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(ZoneDirector))]
    [HarmonyPatch("Start")]
    class ZoneDirector_Start
    {
        static bool Prefix(ZoneDirector __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            foreach (ZoneDirector.AuxItemEntry auxItemEntry in __instance.auxItems)
            {
                __instance.auxItemDict[auxItemEntry.item] = auxItemEntry.weight;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ZoneDirector))]
    [HarmonyPatch("Update")]
    class ZoneDirector_Update
    {
        static bool Prefix(ZoneDirector __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.IsServer;
        }
    }
}