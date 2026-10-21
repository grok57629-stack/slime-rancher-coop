using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Packets;
using SRMultiplayer.Networking;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(FashionPod))]
    [HarmonyPatch("Update")]
    class FashionPod_Update
    {
        static bool Prefix(FashionPod __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            var gadget = __instance.GetComponentInParent<NetworkGadgetSite>();
            return (gadget != null && gadget.IsLocal);
        }
    }
}