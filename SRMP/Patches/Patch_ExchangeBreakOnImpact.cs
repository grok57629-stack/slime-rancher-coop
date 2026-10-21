using HarmonyLib;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(ExchangeBreakOnImpact))]
    [HarmonyPatch("BreakOpen")]
    class ExchangeBreakOnImpact_BreakOpen
    {

        static bool Prefix(ExchangeBreakOnImpact __instance)
        {
            //if  multiplayer, only the server can trigger the impact
           if (!Globals.IsMultiplayer) return true;



            var entity = __instance.GetComponent<NetworkActor>();

            SRMP.Log("Exchange Box Break: " + (entity != null && entity.IsLocal), "EXCHANGE");

            //if the box exists and the entity is local process the break
            return (entity != null && entity.IsLocal);
        }
    }
}