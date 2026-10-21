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
    [HarmonyPatch(typeof(FireColumn))]
    [HarmonyPatch("ActivateFire")]
    class FireColumn_ActivateFire
    {
        static void Prefix(FireColumn __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var netColumn = __instance.GetComponent<NetworkFireColumn>();
            if (netColumn != null && __instance.isActiveAndEnabled && !__instance.fireActive && !__instance.deactivating)
            {
                new PacketFireColumnActivate()
                {
                    ID = netColumn.ID
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(FireColumn))]
    [HarmonyPatch("DeactivateFire")]
    class FireColumn_DeactivateFire
    {
        static void Prefix(FireColumn __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance.isActiveAndEnabled && __instance.fireActive)
            {

            }
        }
    }
}