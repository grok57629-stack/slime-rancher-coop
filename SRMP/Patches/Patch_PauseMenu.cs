using HarmonyLib;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SRMultiplayer.EpicSDK;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PauseMenu))]
    [HarmonyPatch("Quit")]
    class PauseMenu_Quit
    {
        static void Prefix()
        {
            if (!Globals.IsMultiplayer) return;
            PlayerInventory.Save();
            // Scene destruction must never announce gameplay actor removals.
            Globals.GameLoaded = false;

            EpicApplication.Instance.Lobby.LeaveLobby();
                
            EpicApplication.Instance.Lobby.DestroyLobby();
        }
    }

    [HarmonyPatch(typeof(PauseMenu))]
    [HarmonyPatch("PauseGame")]
    class PauseMenu_PauseGame
    {
        static void Prefix(PauseMenu __instance)
        {
            var hostMenu = __instance.GetComponentInChildren<NetworkHostUI>(true);
            if (hostMenu != null)
            {
                hostMenu.gameObject.SetActive(true);
                hostMenu.SetOnlineStatus(Globals.IsServer);
            }
        }
    }

    [HarmonyPatch(typeof(PauseMenu))]
    [HarmonyPatch("UnPauseGame")]
    class PauseMenu_UnPauseGame
    {
        static void Prefix(PauseMenu __instance)
        {
            var hostMenu = __instance.GetComponentInChildren<NetworkHostUI>(true);
            if (hostMenu != null)
            {
                hostMenu.gameObject.SetActive(false);
            }
        }
    }
}
