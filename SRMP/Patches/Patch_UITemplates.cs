using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(UITemplates))]
    [HarmonyPatch("CreateCreditsPrefab")]
    class UITemplates_CreateCreditsPrefab
    {
        static void Prefix(bool aboutCredits)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketWorldCredits().Send();
        }
    }
}