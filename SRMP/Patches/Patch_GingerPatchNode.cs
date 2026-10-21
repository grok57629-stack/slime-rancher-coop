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
    [HarmonyPatch(typeof(GingerPatchNode))]
    [HarmonyPatch("Grow", new Type[0])]
    class GingerPatchNode_Grow
    {
        static void Prefix(GingerPatchNode __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGingerAction()
            {
                ID = __instance.id,
                Grow = true
            }.Send();
        }
    }

    [HarmonyPatch(typeof(GingerPatchNode))]
    [HarmonyPatch("Harvested")]
    class GingerPatchNode_Harvested
    {
        static void Prefix(GingerPatchNode __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGingerAction()
            {
                ID = __instance.id,
                Harvest = true
            }.Send();
        }
    }

    [HarmonyPatch(typeof(GingerPatchNode))]
    [HarmonyPatch("HidePatchAndReset")]
    class GingerPatchNode_HidePatchAndReset
    {
        static void Prefix(GingerPatchNode __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGingerAction()
            {
                ID = __instance.id,
            }.Send();
        }
    }
}