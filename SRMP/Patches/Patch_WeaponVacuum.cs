using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(WeaponVacuum))]
    [HarmonyPatch("PlayTransientAudio")]
    class WeaponVacuum_PlayTransientAudio
    {
        static void Postfix(WeaponVacuum __instance, SECTR_AudioCue cue)
        {
            if (!Globals.IsMultiplayer) return;

            if (!cue.name.Equals("VacAmmoSelect"))
            {
                new PacketPlayAudio()
                {
                    CueName = cue.name,
                    Position = __instance.transform.position,
                    Loop = false
                }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
            }
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum.VacAudioHandler))]
    [HarmonyPatch("SetActive")]
    class WeaponVacuum_VacAudioHandler_SetActive
    {
        static void Prefix(WeaponVacuum.VacAudioHandler __instance, bool active)
        {
            if (!Globals.IsMultiplayer) return;

            if ((active && !__instance.active) || (!active && __instance.active))
            {
                new PacketPlayerFX()
                {
                    ID = Globals.LocalID,
                    Type = (byte)PacketPlayerFX.FXType.VacAudio,
                    Enable = active
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum))]
    [HarmonyPatch("CaptureEffect")]
    class WeaponVacuum_CaptureEffect
    {
        static void Postfix(WeaponVacuum __instance)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayerFX()
            {
                ID = Globals.LocalID,
                Type = (byte)PacketPlayerFX.FXType.Capture,
                Enable = true
            }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum))]
    [HarmonyPatch("CaptureFailedEffect")]
    class WeaponVacuum_CaptureFailedEffect
    {
        static void Postfix(WeaponVacuum __instance)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayerFX()
            {
                ID = Globals.LocalID,
                Type = (byte)PacketPlayerFX.FXType.CaptureFailed,
                Enable = true
            }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum))]
    [HarmonyPatch("Update")]
    class WeaponVacuum_Update
    {
        static bool m_VacFXActive;
        static void Postfix(WeaponVacuum __instance)
        {
            if (!Globals.IsMultiplayer) return;

            if(__instance.vacFX.activeSelf && !m_VacFXActive)
            {
                new PacketPlayerFX()
                {
                    ID = Globals.LocalID,
                    Type = (byte)PacketPlayerFX.FXType.Vac,
                    Enable = true
                }.Send();
            }
            else if(!__instance.vacFX.activeSelf && m_VacFXActive)
            {
                new PacketPlayerFX()
                {
                    ID = Globals.LocalID,
                    Type = (byte)PacketPlayerFX.FXType.Vac,
                    Enable = false
                }.Send();
            }
            m_VacFXActive = __instance.vacFX.activeSelf;
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum))]
    [HarmonyPatch("ShootEffect")]
    class WeaponVacuum_ShootEffect
    {
        static void Postfix(WeaponVacuum __instance)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketPlayerFX()
            {
                ID = Globals.LocalID,
                Type = (byte)PacketPlayerFX.FXType.Shoot,
                Enable = true
            }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum))]
    [HarmonyPatch("AirBurst")]
    class WeaponVacuum_AirBurst
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes.InsertRange(codes.Count - 1, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WeaponVacuum_AirBurst), "Transpiler_Execute"))
            });

            return codes.AsEnumerable();
        }

        static void Transpiler_Execute()
        {
            new PacketPlayerFX()
            {
                ID = Globals.LocalID,
                Type = (byte)PacketPlayerFX.FXType.Airburst,
                Enable = true
            }.Send(Lidgren.Network.NetDeliveryMethod.Unreliable);
        }
    }
}