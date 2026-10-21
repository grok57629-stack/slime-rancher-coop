using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SiloStorageActivator))]
    [HarmonyPatch("OnActiveSlotChanged")]
    class SiloStorageActivator_OnActiveSlotChanged
    {
        static void Prefix(SiloStorageActivator __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var id = __instance.landPlotModel.gameObj.GetComponent<LandPlotLocation>().id;
            int num = __instance.landPlotModel.siloStorageIndices[__instance.activatorIdx];

            new PacketLandPlotSiloSlot()
            {
                ID = id,
                Slot = num,
                ActivatorID = __instance.activatorIdx
            }.Send();
        }
    }
}