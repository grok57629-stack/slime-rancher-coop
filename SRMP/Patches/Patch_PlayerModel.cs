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
    [HarmonyPatch(typeof(PlayerModel))]
    [HarmonyPatch("AddKey")]
    class PlayerModel_AddKey
    {
        static void Postfix()
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketWorldKey()
            {
                Added = true
            }.Send();
        }
    }
}