using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(GadgetDirector))]
    [HarmonyPatch("AddBlueprint")]
    class GadgetDirector_AddBlueprint
    {
        static void Postfix(GadgetDirector __instance, ref bool __result, Gadget.Id blueprint)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                new PacketGadgetAddBlueprint()
                {
                    ID = (ushort)blueprint
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(GadgetDirector))]
    [HarmonyPatch("AddGadget")]
    class GadgetDirector_AddGadget
    {
        static void Postfix(GadgetDirector __instance, Gadget.Id gadget)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGadgetAdd()
            {
                ID = (ushort)gadget
            }.Send();
        }
    }

    [HarmonyPatch(typeof(GadgetDirector))]
    [HarmonyPatch("SpendGadget")]
    class GadgetDirector_SpendGadget
    {
        static void Postfix(GadgetDirector __instance, Gadget.Id gadget)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGadgetSpend()
            {
                ID = (ushort)gadget
            }.Send();
        }
    }

    [HarmonyPatch(typeof(GadgetDirector))]
    [HarmonyPatch("TryToSpendFromRefinery")]
    class GadgetDirector_TryToSpendFromRefinery
    {
        static void Postfix(GadgetDirector __instance, ref bool __result, GadgetDefinition.CraftCost[] costs)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                Dictionary<ushort, int> amounts = new Dictionary<ushort, int>();
                foreach (GadgetDefinition.CraftCost craftCost in costs)
                {
                    if (craftCost.amount > 0)
                    {
                        amounts.Add((ushort)craftCost.id, craftCost.amount);
                    }
                }
                new PacketGadgetRefinerySpend()
                {
                    Amounts = amounts
                }.Send();
            }
        }
    }
}