using HarmonyLib;
using SRMultiplayer.Packets;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(GardenCatcher))]
    [HarmonyPatch("Plant")]
    class GardenCatcher_Plant
    {
        static void Postfix(GardenCatcher __instance, Identifiable.Id cropId)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            string id = __instance.activator.model.gameObj.GetComponent<LandPlotLocation>().id;

            new PacketLandPlotPlantGarden()
            {
                ID = id,
                Type = (ushort)cropId,
                AttachedID = (byte)__instance.activator.model.attachedId,
                AttachedResourceID = (ushort)__instance.activator.model.attachedResourceId
            }.Send();
        }
    }
}