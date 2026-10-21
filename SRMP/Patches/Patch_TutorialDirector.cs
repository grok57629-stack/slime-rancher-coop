using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(TutorialDirector))]
    [HarmonyPatch("SetModel")]
    class TutorialDirector_SetModel
    {
        static void Postfix(TutorialDirector __instance)
        {
            if (!Globals.IsMultiplayer) return;

            foreach (TutorialDirector.Id tut in (TutorialDirector.Id[])Enum.GetValues(typeof(TutorialDirector.Id)))
            {
                __instance.tutModel.completedIds.Add(tut);
            }
        }
    }
}