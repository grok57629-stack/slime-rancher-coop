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
    [HarmonyPatch(typeof(DroneGadget))]
    [HarmonyPatch("SetPrograms")]
    class DroneGadget_SetPrograms
    {
        static void Prefix(DroneGadget __instance, DroneMetadata.Program[] programs)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketDronePrograms()
            {
                ID = __instance.droneModel.siteId,
                Programs = programs.Select(p => new DroneModel.ProgramData() { target = p.target.id, source = p.source.id, destination = p.destination.id }).ToArray()
            }.Send();
        }
    }
}