using HarmonyLib;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(QuicksilverEnergyGenerator))]
    [HarmonyPatch("Activate")]
    class QuicksilverEnergyGenerator_Activate
    {
        static void Postfix(QuicksilverEnergyGenerator __instance, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                new PacketRaceActivate()
                {
                    ID = __instance.id
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(QuicksilverEnergyGenerator))]
    [HarmonyPatch("ExtendActiveDuration")]
    class QuicksilverEnergyGenerator_ExtendActiveDuration
    {
        static void Postfix(QuicksilverEnergyGenerator __instance, float hours, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                new PacketRaceTime()
                {
                    ID = __instance.id,
                    Time = hours
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(QuicksilverEnergyGenerator))]
    [HarmonyPatch("SetState")]
    class QuicksilverEnergyGenerator_SetState
    {
        static void Postfix(QuicksilverEnergyGenerator __instance, QuicksilverEnergyGenerator.State state, bool enableSFX)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if(state == QuicksilverEnergyGenerator.State.COOLDOWN && !enableSFX)
            {
                new PacketRaceEnd()
                {
                    ID = __instance.id
                }.Send();
            }
        }
    }
}