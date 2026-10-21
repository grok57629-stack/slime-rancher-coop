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
    [HarmonyPatch(typeof(PuzzleGateActivator))]
    [HarmonyPatch("DoDeactivateSequence")]
    class PuzzleGateActivator_DoDeactivateSequence
    {
        static void Prefix(PuzzleGateActivator __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPuzzleGateActivate()
            {
                
            }.Send();
        }
    }
}