using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(GadgetChickenCloner))]
    [HarmonyPatch("OnTriggerEnter")]
    class GadgetChickenCloner_OnTriggerEnter
    {
        static bool Prefix(GadgetChickenCloner __instance, Collider collider)
        {
            if (!Globals.IsMultiplayer) return true;

            var entity = collider.GetComponent<NetworkActor>();
            return (entity != null && entity.IsLocal);
        }
    }
}