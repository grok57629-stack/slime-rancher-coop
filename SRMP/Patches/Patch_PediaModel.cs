using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PediaModel))]
    [HarmonyPatch("Unlock")]
    class PediaModel_Unlock
    {
        static void Prefix(PediaModel __instance, PediaDirector.Id[] ids)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPediaUnlock()
            {
                IDs = ids.Select(i => (ushort)i).ToList()
            }.Send();
        }
    }
}