using HarmonyLib;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SlimeSubbehaviour))]
    [HarmonyPatch("RotateTowards")]
    class SlimeSubbehaviour_RotateTowards
    {
        static bool Prefix(SlimeSubbehaviour __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netActor = __instance.GetComponent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }
}