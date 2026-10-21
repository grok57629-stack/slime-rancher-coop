using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(LandPlotLocation))]
    [HarmonyPatch("Replace")]
    class LandPlotLocation_Replace
    {
        static void Postfix(LandPlotLocation __instance, LandPlot oldLandPlot, GameObject replacementPrefab)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketLandPlotReplace()
            {
                ID = __instance.id,
                Type = (byte)replacementPrefab.GetComponent<LandPlot>().typeId
            }.Send();
        }
    }
}