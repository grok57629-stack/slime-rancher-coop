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
    [HarmonyPatch(typeof(DroneStationBattery))]
    [HarmonyPatch("AddLiquid")]
    class DroneStationBattery_AddLiquid
    {
        static void Prefix(DroneStationBattery __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketDroneLiquid()
            {
                ID = __instance.droneModel.siteId
            }.Send();
        }
    }
}