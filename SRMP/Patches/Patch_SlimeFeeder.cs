using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SlimeFeeder))]
    [HarmonyPatch("SetFeederSpeed")]
    class SlimeFeeder_SetFeederSpeed
    {
        static void Prefix(SlimeFeeder __instance, SlimeFeeder.FeedSpeed speed)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var id = __instance.model.gameObj.GetComponent<LandPlotLocation>().id;

            new PacketLandPlotFeederSpeed()
            {
                ID = id,
                Speed = (byte)speed
            }.Send();
        }
    }

    [HarmonyPatch(typeof(SlimeFeeder))]
    [HarmonyPatch("ProcessFeedOperation")]
    class SlimeFeeder_ProcessFeedOperation
    {
        static bool Prefix(SlimeFeeder __instance, bool ejectFood)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netLandPlot = __instance.storage.model.gameObj.GetComponent<NetworkLandplot>();
            if (netLandPlot != null && netLandPlot.IsLocal)
            {
                string plotid = netLandPlot.Location.id;

                Ammo relevantAmmo = __instance.storage.GetRelevantAmmo();
                relevantAmmo.SetAmmoSlot(0);
                if (relevantAmmo.HasSelectedAmmo())
                {
                    new PacketLandPlotSiloRemove()
                    {
                        ID = plotid,
                        SiloType = (byte)(__instance.storage == null ? 0 : __instance.storage.type),
                        CatcherType = 0,
                        Slot = 0
                    }.Send();
                }
                return true;
            }
            __instance.model.remainingFeedOperations = Math.Max(0, __instance.model.remainingFeedOperations - 1);
            return false;
        }
    }
}