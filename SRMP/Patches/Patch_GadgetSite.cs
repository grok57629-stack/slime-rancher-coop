using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(GadgetSite))]
    [HarmonyPatch("DestroyAttached")]
    class GadgetSite_DestroyAttached
    {
        static void Postfix(GadgetSite __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGadgetRemove()
            {
                ID = __instance.id
            }.Send();
        }
    }

    [HarmonyPatch(typeof(GadgetSite))]
    [HarmonyPatch("OnRotateCW")]
    class GadgetSite_OnRotateCW
    {
        static void Postfix(GadgetSite __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance.attached != null)
            {
                new PacketGadgetRotation()
                {
                    ID = __instance.model.id,
                    Rotation = __instance.attached.GetComponent<Gadget>().GetRotation()
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(GadgetSite))]
    [HarmonyPatch("OnRotateCCW")]
    class GadgetSite_OnRotateCCW
    {
        static void Postfix(GadgetSite __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance.attached != null)
            {
                new PacketGadgetRotation()
                {
                    ID = __instance.model.id,
                    Rotation = __instance.attached.GetComponent<Gadget>().GetRotation()
                }.Send();
            }
        }
    }
}