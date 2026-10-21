using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SECTR_CharacterAudio))]
    [HarmonyPatch("OnFootstep")]
    class SECTR_CharacterAudio_OnFootstep
    {
        static void Postfix(SECTR_CharacterAudio __instance, PhysicMaterial currentMaterial)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayAudio()
            {
                CueName = __instance._GetCurrentSurface(currentMaterial).FootstepCue.name,
                Position = __instance.transform.position,
                Loop = false
            }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
        }
    }

    [HarmonyPatch(typeof(SECTR_CharacterAudio))]
    [HarmonyPatch("OnJump")]
    class SECTR_CharacterAudio_OnJump
    {
        static void Postfix(SECTR_CharacterAudio __instance, PhysicMaterial currentMaterial)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayAudio()
            {
                CueName = __instance._GetCurrentSurface(currentMaterial).JumpCue.name,
                Position = __instance.transform.position,
                Loop = false
            }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
        }
    }

    [HarmonyPatch(typeof(SECTR_CharacterAudio))]
    [HarmonyPatch("OnLand")]
    class SECTR_CharacterAudio_OnLand
    {
        static void Postfix(SECTR_CharacterAudio __instance, PhysicMaterial currentMaterial)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayAudio()
            {
                CueName = __instance._GetCurrentSurface(currentMaterial).LandCue.name,
                Position = __instance.transform.position,
                Loop = false
            }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
        }
    }
}
