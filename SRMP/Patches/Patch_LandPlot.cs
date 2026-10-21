using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(LandPlot))]
    [HarmonyPatch("AddUpgrade")]
    class LandPlot_AddUpgrade
    {
        static void Postfix(LandPlot __instance, LandPlot.Upgrade upgrade)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            string plotid = __instance.transform.parent.GetComponent<LandPlotLocation>().id;

            new PacketLandPlotUpgrade()
            {
                ID = plotid,
                Upgrade = (byte)upgrade
            }.Send();
        }
    }

    [HarmonyPatch(typeof(LandPlot))]
    [HarmonyPatch("DestroyAttached")]
    class LandPlot_DestroyAttached
    {
        static void Postfix(LandPlot __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            string plotid = __instance.transform.parent.GetComponent<LandPlotLocation>().id;

            new PacketLandPlotPlantGarden()
            {
                ID = plotid,
                Type = (ushort)Identifiable.Id.NONE,
                AttachedID = (byte)__instance.model.attachedId,
                AttachedResourceID = (ushort)__instance.model.attachedResourceId
            }.Send();
        }
    }
}