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
    [HarmonyPatch(typeof(WorldStateMasterSwitch))]
    [HarmonyPatch("SetStateForAll")]
    class WorldStateMasterSwitch_SetStateForAll
    {
        static void Postfix(WorldStateMasterSwitch __instance, SwitchHandler.State state)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketWorldSwitchActivate()
            {
                ID = __instance.id,
                State = (byte)state
            }.Send();
        }
    }
}