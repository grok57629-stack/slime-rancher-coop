using HarmonyLib;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(HydroTurret))]
    [HarmonyPatch("Update")]
    class HydroTurret_Update
    {
        static bool Prefix(HydroTurret __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netSite = __instance.GetComponentInParent<NetworkGadgetSite>();
            return (netSite != null && netSite.IsLocal);
        }
    }
}