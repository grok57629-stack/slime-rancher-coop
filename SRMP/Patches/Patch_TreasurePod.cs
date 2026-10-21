using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(TreasurePod))]
    [HarmonyPatch("Activate")]
    class TreasurePodModel_Activate
    {
        static void Prefix(TreasurePod __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance.HasKey())
            {
                var id = __instance.id;

                new PacketTreasurePodOpen()
                {
                    ID = id
                }.Send();
            }
        }
    }
}