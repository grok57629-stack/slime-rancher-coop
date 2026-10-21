using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Packets;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(DecorizerStorage))]
    [HarmonyPatch("Cleanup")]
    class DecorizerStorage_Cleanup
    {
        static void Postfix(DecorizerStorage __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketWorldDecorizer()
            {
                Contents = __instance.model.contents.ToDictionary(c => c.Key, v => v.Value),
                Settings = __instance.model.settings.ToDictionary(s => s.Key, v => (ushort)v.Value.selected)
            }.Send();
        }
    }

    [HarmonyPatch(typeof(DecorizerStorage))]
    [HarmonyPatch("selected", MethodType.Setter)]
    class DecorizerStorage_set_selected
    {
        static void Prefix(DecorizerStorage __instance, ref Identifiable.Id value)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketWorldDecorizerSetting()
            {
                ID = __instance.id,
                Selected = (ushort)value
            }.Send();
        }
    }
}