using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Packets;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(BaseUI))]
    [HarmonyPatch("Play")]
    class BaseUI_Play
    {
        static void Postfix(BaseUI __instance, SECTR_AudioCue cue)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPlayAudio()
            {
                CueName = cue.name,
                Position = SRSingleton<SceneContext>.Instance.Player.transform.position,
                Loop = false
            }.Send();
        }
    }
}