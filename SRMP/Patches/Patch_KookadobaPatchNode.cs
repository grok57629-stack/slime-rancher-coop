using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
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
    [HarmonyPatch(typeof(KookadobaPatchNode))]
    [HarmonyPatch("Grow", new Type[] { typeof(GameObject) })]
    class KookadobaPatchNode_Grow
    {
        static void Prefix(KookadobaPatchNode __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketKookadobaAction()
            {
                ID = __instance.GetComponent<NetworkKookadobaPatchNode>().ID,
                Grow = true
            }.Send();
        }
    }

    [HarmonyPatch(typeof(KookadobaPatchNode))]
    [HarmonyPatch("Harvested")]
    class KookadobaPatchNode_Harvested
    {
        static void Prefix(KookadobaPatchNode __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketKookadobaAction()
            {
                ID = __instance.GetComponent<NetworkKookadobaPatchNode>().ID,
                Harvest = true
            }.Send();
        }
    }
}