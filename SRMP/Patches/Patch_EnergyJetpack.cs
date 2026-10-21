using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(EnergyJetpack))]
    [HarmonyPatch("OnStart_Jetpack")]
    class EnergyJetpack_OnStart_Jetpack
    {
        static void Postfix(EnergyJetpack __instance)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayerFX()
            {
                ID = Globals.LocalID,
                Type = (byte)PacketPlayerFX.FXType.JetpackAudio,
                Enable = true
            }.Send();
        }
    }

    [HarmonyPatch(typeof(EnergyJetpack))]
    [HarmonyPatch("OnStop_Jetpack")]
    class EnergyJetpack_OnStop_Jetpack
    {
        static void Postfix(EnergyJetpack __instance)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayerFX()
            {
                ID = Globals.LocalID,
                Type = (byte)PacketPlayerFX.FXType.JetpackAudio,
                Enable = false
            }.Send();
        }
    }
}
