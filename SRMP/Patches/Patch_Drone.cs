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
    [HarmonyPatch(typeof(Drone))]
    [HarmonyPatch("Awake")]
    class Drone_Awake
    {
        static void Postfix(Drone __instance)
        {
            __instance.ammo = new NetworkDroneAmmo(__instance);
            var netDrone = __instance.gameObject.AddComponent<NetworkDrone>();
            netDrone.Drone = __instance;
            netDrone.Region = __instance.GetComponentInParent<NetworkRegion>(true);
        }
    }
}