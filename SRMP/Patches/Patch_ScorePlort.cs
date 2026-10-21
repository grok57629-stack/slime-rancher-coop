using HarmonyLib;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(ScorePlort))]
    [HarmonyPatch("OnTriggerEnter")]
    class ScorePlort_OnTriggerEnter
    {
        static bool Prefix(ScorePlort __instance, Collider col)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netActor = col.GetComponent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }
}