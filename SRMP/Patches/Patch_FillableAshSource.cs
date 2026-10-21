using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(FillableAshSource))]
    [HarmonyPatch("AddAsh")]
    class FillableAshSource_AddAsh
    {
        static void Postfix(FillableAshSource __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var id = __instance.plotModel.gameObj.GetComponent<LandPlotLocation>().id;
            var num = __instance.plotModel.ashUnits;

            new PacketLandPlotAsh()
            {
                ID = id,
                Amount = num
            }.Send();
        }
    }

    [HarmonyPatch(typeof(FillableAshSource))]
    [HarmonyPatch("ConsumeAsh")]
    class FillableAshSource_ConsumeAsh
    {
        static void Postfix(FillableAshSource __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var id = __instance.plotModel.gameObj.GetComponent<LandPlotLocation>().id;
            var num = __instance.plotModel.ashUnits;

            new PacketLandPlotAsh()
            {
                ID = id,
                Amount = num
            }.Send();
        }
    }
}