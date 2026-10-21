using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PlaceGadgetUI))]
    [HarmonyPatch("Place")]
    class PlaceGadgetUI_Place
    {
        static void Postfix(PlaceGadgetUI __instance, Gadget.Id id)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketGadgetSpawn()
            {
                ID = __instance.site.id,
                Ident = (ushort)id,
                Rotation = __instance.site.attached.transform.eulerAngles.y
            }.Send();
        }
    }
}
