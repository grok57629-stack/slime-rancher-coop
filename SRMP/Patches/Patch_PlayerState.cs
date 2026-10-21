using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("Reset")]
    class PlayerState_Reset
    {
        static void Postfix(PlayerState __instance)
        {
            Dictionary<PlayerState.AmmoMode, Ammo> dictionary = new Dictionary<PlayerState.AmmoMode, Ammo>(PlayerState.AmmoModeComparer.Instance);
            dictionary.Add(PlayerState.AmmoMode.DEFAULT, new NetworkPlayerAmmo(0, __instance.GetPotentialAmmo(), 5, 4, PlayerState.PLAYER_AMMO_PREDS, new Func<Identifiable.Id, int, int>(__instance.GetMaxAmmo_Default)));
            HashSet<Identifiable.Id> hashSet = new HashSet<Identifiable.Id>(Identifiable.idComparer);
            hashSet.Add(Identifiable.Id.QUICKSILVER_PLORT);
            hashSet.Add(Identifiable.Id.VALLEY_AMMO_1);
            hashSet.Add(Identifiable.Id.VALLEY_AMMO_2);
            hashSet.Add(Identifiable.Id.VALLEY_AMMO_3);
            hashSet.Add(Identifiable.Id.VALLEY_AMMO_4);
            Predicate<Identifiable.Id>[] array = new Predicate<Identifiable.Id>[3];
            array[0] = ((Identifiable.Id id) => id == Identifiable.Id.QUICKSILVER_PLORT);
            array[1] = ((Identifiable.Id id) => id == Identifiable.Id.VALLEY_AMMO_1);
            array[2] = ((Identifiable.Id id) => id == Identifiable.Id.VALLEY_AMMO_2 || id == Identifiable.Id.VALLEY_AMMO_3 || id == Identifiable.Id.VALLEY_AMMO_4);
            dictionary.Add(PlayerState.AmmoMode.NIMBLE_VALLEY, new NetworkPlayerAmmo(1, hashSet, 3, 3, array, new Func<Identifiable.Id, int, int>(__instance.GetMaxAmmo_NimbleValley)));

            __instance.ammoDict = dictionary;
        }
    }

    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("SpendKey")]
    class PlayerModel_SpendKey
    {
        static void Postfix(ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                new PacketWorldKey()
                {
                    Added = false
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("UnlockMap")]
    class PlayerModel_UnlockMap
    {
        static void Postfix(ZoneDirector.Zone zone)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketWorldMapUnlock()
            {
                Zone = (byte)zone
            }.Send();
        }
    }

    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("SpendCurrency")]
    class PlayerModel_SpendCurrency
    {
        static void Postfix(PlayerState __instance, int adjust)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPlayerCurrency()
            {
                Total = Math.Max(0, __instance.GetCurrency()),
                Adjust = -adjust,
                Origin = Globals.LocalID
            }.Send();
        }
    }

    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("AddCurrency")]
    class PlayerModel_AddCurrency
    {
        static void Postfix(PlayerState __instance, int adjust, PlayerState.CoinsType coinsType)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPlayerCurrency()
            {
                Total = Math.Max(0, __instance.GetCurrency()),
                Adjust = adjust,
                Type = (byte)coinsType,
                Origin = Globals.LocalID
            }.Send();
        }
    }

    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("SetCurrencyDisplay")]
    class PlayerModel_SetCurrencyDisplay
    {
        static void Postfix(PlayerState __instance, int? currencyDisplay)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPlayerCurrencyDisplay()
            {
                IsNull = currencyDisplay == null,
                Currency = currencyDisplay != null ? currencyDisplay.Value : DroneFastForwarder.coinsPopup
            }.Send();
        }
    }

    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("AddUpgrade")]
    class PlayerModel_AddUpgrade
    {
        static void Postfix(PlayerState.Upgrade upgrade)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPlayerUpgrade()
            {
                Upgrade = (byte)upgrade
            }.Send();
        }
    }

    [HarmonyPatch(typeof(PlayerState))]
    [HarmonyPatch("Update")]
    class PlayerState_Update
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var add = AccessTools.Method(typeof(List<PlayerState.Upgrade>), "Add");
            var replacement = AccessTools.Method(typeof(PlayerState_Update), "Transpiler_Execute");
            foreach (var code in codes)
            {
                if (code.Calls(add))
                {
                    // Keep labels/exception blocks and consume both original
                    // stack arguments (list receiver + upgrade value).
                    code.opcode = OpCodes.Call;
                    code.operand = replacement;
                }
            }

            return codes.AsEnumerable();
        }

        static void Transpiler_Execute(List<PlayerState.Upgrade> upgrades, PlayerState.Upgrade upgrade)
        {
            upgrades.Add(upgrade);

            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketPlayerUpgradeUnlock()
            {
                Upgrade = (byte)upgrade
            }.Send();
        }
    }
}
