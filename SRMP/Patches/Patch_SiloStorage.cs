using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SiloStorage))]
    [HarmonyPatch("InitAmmo")]
    class SiloStorage_InitAmmo
    {
        static bool Prefix(SiloStorage __instance)
        {
            if(__instance.ammo == null)
            {
                __instance.ammo = new NetworkAmmo(__instance, __instance.type.GetContents(), __instance.numSlots, __instance.numSlots, new Predicate<Identifiable.Id>[__instance.numSlots], (Identifiable.Id id, int index) => __instance.maxAmmo);
            }
            else if(!(__instance.ammo is NetworkAmmo))
            {
                __instance.ammo = new NetworkAmmo(__instance, __instance.type.GetContents(), __instance.numSlots, __instance.numSlots, new Predicate<Identifiable.Id>[__instance.numSlots], (Identifiable.Id id, int index) => __instance.maxAmmo);
            }
            return false;
        }
    }
}