using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(Oasis))]
    [HarmonyPatch("OnSetLive")]
    class Oasis_OnSetLive
    {
        static void Prefix(Oasis __instance, bool immediate)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketOasisLive()
            {
                ID = __instance.id
            }.Send();
        }
    }
}