using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(QuicksilverPlortCollector))]
    [HarmonyPatch("Update")]
    class QuicksilverPlortCollector_Update
    {
        static bool Prefix(QuicksilverPlortCollector __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            var netActor = __instance.GetComponent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }
}