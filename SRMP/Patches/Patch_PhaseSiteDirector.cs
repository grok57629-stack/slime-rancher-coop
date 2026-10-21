using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PhaseSiteDirector))]
    [HarmonyPatch("RefreshTotalPhaseableObjects")]
    class PhaseSiteDirector_RefreshTotalPhaseableObjects
    {
        static void Postfix(PhaseSiteDirector __instance)
        {
            Globals.LemonTrees = __instance.worldModel.occupiedPhaseSites.ToList();
        }
    }
}