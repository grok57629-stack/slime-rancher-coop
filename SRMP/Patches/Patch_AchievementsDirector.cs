using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(AchievementsDirector))]
    [HarmonyPatch("Update")]
    class AchievementsDirector_Update
    {
        static bool Prefix()
        {
            if (!Globals.DisableAchievements) return true;

            return false;
        }
    }

    [HarmonyPatch(typeof(AchievementsDirector))]
    [HarmonyPatch("LateUpdate")]
    class AchievementsDirector_LateUpdate
    {
        static bool Prefix()
        {
            if (!Globals.DisableAchievements) return true;

            return false;
        }
    }
}