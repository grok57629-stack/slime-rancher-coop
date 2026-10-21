using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(Ammo))]
    [HarmonyPatch("DecrementSelectedAmmo")]
    class Ammo_DecrementSelectedAmmo
    {
        static void Postfix(Ammo __instance, int amount)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance is NetworkAmmo)
            {
                new PacketLandPlotSiloAmmoRemove()
                {
                    ID = ((NetworkAmmo)__instance).ID,
                    Type = (byte)((NetworkAmmo)__instance).Silo.type,
                    Slot = __instance.selectedAmmoIdx,
                    Count = amount
                }.Send();
            }
            else if (__instance is NetworkDroneAmmo drone)
            {
                new PacketDroneAmmoRemove { ID = drone.Drone.droneModel.siteId, Count = amount }.Send();
            }
            else if(__instance is NetworkPlayerAmmo)
            {
                SRMP.Log($"DecrementSelectedAmmo {((NetworkPlayerAmmo)__instance).ID} {amount}", "PLAYERAMMO");
            }
        }
    }

    [HarmonyPatch(typeof(Ammo))]
    [HarmonyPatch("Clear", new Type[] { typeof(int) })]
    class Ammo_Clear
    {
        static void Postfix(Ammo __instance, int index)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance is NetworkAmmo)
            {
                new PacketLandPlotSiloAmmoClear()
                {
                    ID = ((NetworkAmmo)__instance).ID,
                    Type = (byte)((NetworkAmmo)__instance).Silo.type,
                    Slot = index
                }.Send();
            }
            else if (__instance is NetworkDroneAmmo)
            {
                new PacketDroneAmmoClear()
                {
                    ID = ((NetworkDroneAmmo)__instance).Drone.droneModel.siteId
                }.Send();
            }
            else if (__instance is NetworkPlayerAmmo)
            {
                SRMP.Log($"Clear {((NetworkPlayerAmmo)__instance).ID} {index}", "PLAYERAMMO");
            }
        }
    }

    [HarmonyPatch(typeof(Ammo))]
    [HarmonyPatch("ReplaceWithQuicksilverAmmo")]
    class Ammo_ReplaceWithQuicksilverAmmo
    {
        static void Postfix(Ammo __instance, Identifiable.Id id, int count, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result && __instance is NetworkPlayerAmmo)
            {
                SRMP.Log($"ReplaceWithQuicksilverAmmo {((NetworkPlayerAmmo)__instance).ID} {id} {count}", "PLAYERAMMO");
            }
        }
    }

    [HarmonyPatch(typeof(Ammo))]
    [HarmonyPatch("SetAmmoSlot")]
    class Ammo_SetAmmoSlot
    {
        static void Postfix(Ammo __instance, int idx, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result && __instance is NetworkPlayerAmmo)
            {
                SRMP.Log($"SetAmmoSlot {((NetworkPlayerAmmo)__instance).ID} {idx}", "PLAYERAMMO");
            }
        }
    }

    [HarmonyPatch(typeof(Ammo))]
    [HarmonyPatch("Replace", new Type[] { typeof(Identifiable.Id), typeof(Identifiable.Id) })]
    class Ammo_Replace
    {
        static void Postfix(Ammo __instance, Identifiable.Id previous, Identifiable.Id next, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result && __instance is NetworkPlayerAmmo)
            {
                SRMP.Log($"Replace {((NetworkPlayerAmmo)__instance).ID} {previous} {next}", "PLAYERAMMO");
            }
        }
    }

    [HarmonyPatch(typeof(Ammo))]
    [HarmonyPatch("Decrement", new Type[] { typeof(int), typeof(int) })]
    class Ammo_Decrement
    {
        static void Postfix(Ammo __instance, int index, int count)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance is NetworkAmmo)
            {
                new PacketLandPlotSiloAmmoRemove()
                {
                    ID = ((NetworkAmmo)__instance).ID,
                    Type = (byte)((NetworkAmmo)__instance).Silo.type,
                    Slot = index,
                    Count = count
                }.Send();
            }
            else if (__instance is NetworkDroneAmmo)
            {
                new PacketDroneAmmoRemove()
                {
                    ID = ((NetworkDroneAmmo)__instance).Drone.droneModel.siteId,
                    Count = count
                }.Send();
            }
            else if (__instance is NetworkPlayerAmmo)
            {
                SRMP.Log($"Decrement {((NetworkPlayerAmmo)__instance).ID} {index} {count}", "PLAYERAMMO");
            }
        }
    }

    [HarmonyPatch(typeof(Ammo))]
    [HarmonyPatch("MaybeAddToSpecificSlot", new Type[] { typeof(Identifiable.Id), typeof(Identifiable), typeof(int), typeof(int), typeof(bool) })]
    class Ammo_MaybeAddToSpecificSlot
    {
        static void Postfix(Ammo __instance, ref bool __result, Identifiable.Id id, Identifiable identifiable, int slotIdx, int count, bool overflow)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__instance is NetworkAmmo && __result)
            {
                new PacketLandPlotSiloAmmoAdd()
                {
                    ID = ((NetworkAmmo)__instance).ID,
                    Type = (byte)((NetworkAmmo)__instance).Silo.type,
                    Ident = (ushort)id,
                    Slot = slotIdx,
                    Count = count,
                    Overflow = overflow,
                    Emotions = __instance.Slots[slotIdx].emotions
                }.Send();
            }
            else if(__instance is NetworkDroneAmmo && __result)
            {
                new PacketDroneAmmoAdd()
                {
                    ID = ((NetworkDroneAmmo)__instance).Drone.droneModel.siteId,
                    Ident = (ushort)id,
                    Count = count,
                    Overflow = overflow
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(Ammo), "MaybeAddToSlot")]
    class Ammo_MaybeAddToSlot
    {
        static void Prefix(Ammo __instance, out int[] __state)
        {
            __state = null;
            if (!Globals.IsMultiplayer || Globals.HandlePacket ||
                !(__instance is NetworkAmmo || __instance is NetworkDroneAmmo)) return;
            __state = __instance.Slots.Select(slot => slot == null ? 0 : slot.count).ToArray();
        }

        static void Postfix(Ammo __instance, bool __result, int[] __state)
        {
            if (!__result || __state == null || Globals.HandlePacket) return;
            // Let the game handle predicates, water and emotion averaging.
            // Observe only the successful change, including DroneAmmo's path.
            for (int i = 0; i < __state.Length; i++)
            {
                var slot = __instance.Slots[i];
                int added = (slot == null ? 0 : slot.count) - __state[i];
                if (added <= 0) continue;
                if (__instance is NetworkAmmo silo)
                    new PacketLandPlotSiloAmmoAdd
                    {
                        ID = silo.ID, Type = (byte)silo.Silo.type, Ident = (ushort)slot.id,
                        Slot = i, Count = added, Emotions = slot.emotions
                    }.Send();
                else if (__instance is NetworkDroneAmmo drone)
                    new PacketDroneAmmoAdd
                    {
                        ID = drone.Drone.droneModel.siteId, Ident = (ushort)slot.id, Count = added
                    }.Send();
            }
        }
    }
}
