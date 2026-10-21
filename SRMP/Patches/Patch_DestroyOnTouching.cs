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
    [HarmonyPatch(typeof(DestroyOnTouching))]
    [HarmonyPatch("UpdateDestroyTime")]
    class DestroyOnTouching_UpdateDestroyTime
    {
        static bool Prefix(DestroyOnTouching __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var entity = __instance.GetComponent<NetworkActor>();
            return (entity != null && entity.IsLocal);
        }
    }
}