using HarmonyLib;
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
    //[HarmonyPatch(typeof(Nutcracker))]
    //[HarmonyPatch("OnTriggerEnter")]
    //class Nutcracker_OnTriggerEnter
    //{
    //    static bool Prefix(Nutcracker __instance, Collider col)
    //    {
    //        if (!Globals.IsMultiplayer) return true;

    //        var netActor = col.GetComponent<NetworkActor>();
    //        return (netActor != null && netActor.IsLocal);
    //    }
    //}

    [HarmonyPatch(typeof(Nutcracker))]
    [HarmonyPatch("DoCrack")]
    class Nutcracker_DoCrack
    {
        static bool Prefix(Nutcracker __instance, GameObject toCrack, ref IEnumerator __result)
        {
            if (!Globals.IsMultiplayer) return true;

            var netActor = toCrack.GetComponent<NetworkActor>();
            if (netActor != null && !netActor.IsLocal)
            {
                // DoCrack returns an IEnumerator. Skipping it without setting
                // __result passed null to Unity's StartCoroutine.
                var cracker = __instance.gameObject.GetOrAddComponent<NetworkNutcracker>();
                cracker.Cracker = __instance;
                __result = cracker.DoCrack(toCrack);
                return false;
            }
            return true;
        }
    }
}
