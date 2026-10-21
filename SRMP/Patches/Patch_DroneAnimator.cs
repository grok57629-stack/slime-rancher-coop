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
    [HarmonyPatch(typeof(DroneAnimator))]
    [HarmonyPatch("SetAnimation")]
    class DroneAnimator_SetAnimation
    {
        static bool Prefix(DroneAnimator __instance, DroneAnimator.Id id)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netDrone = __instance.GetComponentInParent<NetworkDrone>();
            if (netDrone != null && netDrone.IsLocal && netDrone.Drone.droneModel != null)
            {
                new PacketDroneAnimation()
                {
                    ID = netDrone.Drone.droneModel.siteId,
                    Anim = (byte)id
                }.Send();
                return true;
            }
            return true;
        }
    }
}
