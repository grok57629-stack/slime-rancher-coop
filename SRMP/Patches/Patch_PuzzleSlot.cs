using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PuzzleSlot))]
    [HarmonyPatch("ActivateOnFill")]
    class PuzzleSlot_ActivateOnFill
    {
        static void Prefix(PuzzleSlot __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPuzzleSlotFilled()
            {
                ID = __instance.id
            }.Send();
        }
    }
}