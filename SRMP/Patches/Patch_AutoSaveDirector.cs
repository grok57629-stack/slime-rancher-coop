using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using MonomiPark.SlimeRancher.Persist;
using SRMultiplayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(AutoSaveDirector))]
    [HarmonyPatch("SaveGame")]
    class AutoSaveDirector_SaveGame
    {
        static bool Prefix()
        {
            if(Globals.IsServer)
            {
                foreach(var player in Globals.Players.Values)
                {
                    if(player != null && player.HasLoaded && player.UUID != Guid.Empty)
                    {
                        player.Save();
                    }
                }
                return true;
            }
            else if (Globals.IsClient)
            {
                Networking.PlayerInventory.Save();
            }
            return !Globals.IsClient;
        }
    }
}
