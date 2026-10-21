using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(TimeDirector))]
    [HarmonyPatch("FastForwardTo")]
    class TimeDirector_FastForwardTo
    {
        static void Prefix(TimeDirector __instance, ref double fastForwardUntil)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (SRSingleton<SceneContext>.Instance.TimeDirector.IsFastForwarding()) return;

            new PacketWorldFastForward()
            {
                FastForwardTill = fastForwardUntil
            }.Send();
        }
    }
}