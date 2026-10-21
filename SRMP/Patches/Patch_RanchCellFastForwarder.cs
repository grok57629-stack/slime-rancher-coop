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
    [HarmonyPatch(typeof(RanchCellFastForwarder))]
    [HarmonyPatch("OnFastForward")]
    class RanchCellFastForwarder_OnFastForward
    {
        static bool Prefix(RanchCellFastForwarder __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.IsServer;
        }
    }

    [HarmonyPatch(typeof(RanchCellFastForwarder))]
    [HarmonyPatch("OnFastForwardChanged")]
    class RanchCellFastForwarder_OnFastForwardChanged
    {
        static bool Prefix(RanchCellFastForwarder __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            var netRegion = __instance.GetComponent<NetworkRegion>();
            return Globals.IsServer && netRegion != null && netRegion.Players.Count <= 1;
        }
    }

    [HarmonyPatch(typeof(RanchCellFastForwarder))]
    [HarmonyPatch("OnHibernationStateChanged")]
    class RanchCellFastForwarder_OnHibernationStateChanged
    {
        static bool Prefix(RanchCellFastForwarder __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.IsServer;
        }
    }

    [HarmonyPatch(typeof(RanchCellFastForwarder))]
    [HarmonyPatch("OnHibernation")]
    class RanchCellFastForwarder_OnHibernation
    {
        static bool Prefix(RanchCellFastForwarder __instance)
        {
            if (!Globals.IsMultiplayer) return true;
            
            return false;
        }
    }
}