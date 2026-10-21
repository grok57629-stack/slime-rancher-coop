using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Packets;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PediaDirector))]
    [HarmonyPatch("MaybeShowPopup", new Type[] { typeof(PediaDirector.Id) })]
    class PediaDirector_MaybeShowPopup
    {
        static void Prefix(PediaDirector __instance, PediaDirector.Id id)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if(!__instance.IsUnlocked(id))
            {
                new PacketPediaShowPopup()
                {
                    ID = (ushort)id
                }.Send();
            }
        }
    }
}