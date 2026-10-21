using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Server;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(TimeDirector))]
    [HarmonyPatch("Update")]
    class FIX_TimeDirector_DelayedUnpause
    {
        static void Postfix(TimeDirector __instance)
        {
            if (Globals.IsMultiplayer)
            {
                var autoHost = AutoHost.Instance;
                bool pauseForEmptyServer = autoHost != null
                    && autoHost.IsHosting
                    && autoHost.Config != null
                    && autoHost.Config.PauseWhenNoPlayers
                    && autoHost.IsIdlePaused;
                Time.timeScale = pauseForEmptyServer ? 0f : 1f;
            }

            if (Globals.PauseState == PauseState.Pause && __instance.pauserCount <= 0)
            {
                Globals.PauseState = PauseState.Playing;
            }
        }
    }

    [HarmonyPatch(typeof(TimeDirector))]
    [HarmonyPatch("Pause")]
    class FIX_TimeDirector_Pause
    {
        static void Postfix(TimeDirector __instance)
        {
            if (__instance.pauserCount > 0)
            {
                Globals.PauseState = PauseState.Pause;
            }
        }
    }

    [HarmonyPatch(typeof(SRInput))]
    [HarmonyPatch("SetInputMode", typeof(SRInput.InputMode))]
    class FIX_SRInput_SetInputMode
    {
        static bool Prefix(SRInput __instance, ref SRInput.InputMode mode)
        {
            if (!Globals.IsMultiplayer) return true;

            if (Globals.PauseState == PauseState.Pause)
            {
                __instance.actions.Enabled = true;
                __instance.pauseActions.Enabled = true;
                __instance.engageActions.Enabled = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MapUI))]
    [HarmonyPatch("OpenMap")]
    class FIX_MapUI_OpenMap
    {
        static void Postfix(MapUI __instance)
        {
            // Fix input mode not refreshing after dying
            SRInput.instance.SetInputMode(SRInput.InputMode.PAUSE);
        }
    }

    [HarmonyPatch(typeof(vp_FPInput))]
    [HarmonyPatch("Update")]
    class FIX_FPInput_Pause
    {
        private static bool initialAllowInput = false;

        static void Prefix(vp_FPInput __instance)
        {
            if (Globals.IsMultiplayer && Globals.PauseState == PauseState.Pause)
            {
                // Save the initial state of the input
                initialAllowInput = __instance.m_AllowGameplayInput;

                // Disable input so that we don't move when "paused"
                __instance.m_AllowGameplayInput = false;
            }
        }

        static void Postfix(vp_FPInput __instance)
        {
            if (Globals.IsMultiplayer && Globals.PauseState == PauseState.Pause)
            {
                // Restore any initial value after we've blocked the input
                // since I have no idea if it should have been initially disabled
                __instance.m_AllowGameplayInput = initialAllowInput;
            }
        }
    }

    [HarmonyPatch(typeof(PauseMenu))]
    [HarmonyPatch("Update")]
    class FIX_PauseMenu_Update
    {
        static bool Prefix(PauseMenu __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            if ((SRInput.Actions.menu.WasPressed || SRInput.PauseActions.unmenu.WasPressed) && !__instance.timeDir.IsFastForwarding())
            {
                if (__instance.pauseUI.activeSelf)
                {
                    if (__instance.timeDir.ExactlyOnePauser() && !__instance.suppressUnpause)
                    {
                        __instance.UnPauseGame();
                    }
                }
                else if (Globals.PauseState == PauseState.Playing)
                {
                    __instance.PauseGame();
                }
            }
            else if (SRInput.PauseActions.cancel.WasPressed && !__instance.suppressUnpause && __instance.pauseUI.activeSelf && __instance.timeDir.ExactlyOnePauser())
            {
                __instance.UnPauseGame();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(CrosshairUI))]
    [HarmonyPatch("Update")]
    class FIX_CrosshairUI_Update
    {
        static bool Prefix()
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.PauseState == PauseState.Playing;
        }
    }

    [HarmonyPatch(typeof(Flashlight))]
    [HarmonyPatch("Update")]
    class FIX_Flashlight_Update
    {
        static bool Prefix()
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.PauseState == PauseState.Playing;
        }
    }

    [HarmonyPatch(typeof(ResourceCycle))]
    [HarmonyPatch("RegistryUpdate")]
    class FIX_ResourceCycle_RegistryUpdate
    {
        static bool Prefix()
        {
            if (!Globals.IsMultiplayer) return true;

            return true; // Globals.PauseState == PauseState.Playing;
        }
    }

    [HarmonyPatch(typeof(UIDetector))]
    [HarmonyPatch("InteractionEnabled")]
    class FIX_UIDetector_InteractionEnabled
    {
        static bool Prefix(ref bool __result)
        {
            if (!Globals.IsMultiplayer) return true;

            if (Globals.PauseState == PauseState.Pause)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(WeaponVacuum))]
    [HarmonyPatch("Update")]
    class FIX_WeaponVacuum_Update
    {
        static bool Prefix()
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.PauseState == PauseState.Playing;
        }
    }

    [HarmonyPatch(typeof(vp_FPCamera))]
    [HarmonyPatch("Update")]
    class FIX_vp_FPCamera_Update
    {
        static bool Prefix()
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.PauseState == PauseState.Playing;
        }
    }

    [HarmonyPatch(typeof(vp_FPCamera))]
    [HarmonyPatch("LateUpdate")]
    class FIX_vp_FPCamera_LateUpdate
    {
        static bool Prefix()
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.PauseState == PauseState.Playing;
        }
    }

    [HarmonyPatch(typeof(vp_FPCamera))]
    [HarmonyPatch("FixedUpdate")]
    class FIX_vp_FPCamera_FixedUpdate
    {
        static bool Prefix()
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.PauseState == PauseState.Playing;
        }
    }
}
