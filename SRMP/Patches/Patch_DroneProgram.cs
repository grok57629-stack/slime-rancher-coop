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
    [HarmonyPatch(typeof(DroneProgram))]
    [HarmonyPatch("Action")]
    class DroneProgram_Action
    {
        static bool Prefix(DroneProgram __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netDrone = __instance.GetComponentInParent<NetworkDrone>();
            return (netDrone != null && netDrone.IsLocal);
        }
    }
}