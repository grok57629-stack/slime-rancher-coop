using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(ProgressDirector))]
    [HarmonyPatch("NoteProgressChanged")]
    class ProgressDirector_NoteProgressChanged
    {
        static void Postfix(ProgressDirector __instance, ProgressDirector.ProgressType type)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            int progress = SRSingleton<SceneContext>.Instance.ProgressDirector.GetProgress(type);

            new PacketWorldProgress()
            {
                Type = (ushort)type,
                Amount = progress
            }.Send();
        }
    }
}