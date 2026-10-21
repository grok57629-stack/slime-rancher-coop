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
    [HarmonyPatch(typeof(SlimeEatWater))]
    [HarmonyPatch("Update")]
    class SlimeEatWater_Update
    {
        static bool Prefix(SlimeEatWater __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netActor = __instance.GetComponent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }
}