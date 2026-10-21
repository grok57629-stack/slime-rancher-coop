using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(DroneSubbehaviourRest))]
    [HarmonyPatch("OnFirstAction")]
    class DroneSubbehaviourRest_OnFirstAction
    {
        static void Prefix(DroneSubbehaviourRest __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket || __instance.drone.GetComponent<NetworkDrone>()?.IsLocal != true) return;

            new PacketDroneActive()
            {
                ID = __instance.drone.droneModel.siteId,
                Enabled = false
            }.Send();
        }
    }

    [HarmonyPatch(typeof(DroneSubbehaviourRest))]
    [HarmonyPatch("OnAction")]
    class DroneSubbehaviourRest_OnAction
    {
        static void Postfix(DroneSubbehaviourRest __instance, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket || __instance.drone.GetComponent<NetworkDrone>()?.IsLocal != true) return;

            if (__result)
            {
                new PacketDroneActive()
                {
                    ID = __instance.drone.droneModel.siteId,
                    Enabled = true
                }.Send();
            }
        }
    }
}
