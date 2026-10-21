using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(GordoEat), "MaybeEat")]
    class GordoEat_MaybeEat
    {
        static bool Prefix(GordoEat __instance, ref bool __result, Collider col, out PacketGordoEat __state)
        {
            __state = null;
            if (!Globals.IsMultiplayer) return true;
            __result = false;
            if (Globals.HandlePacket || col == null || !__instance.CanEat()) return false;
            if (__instance.allEats.Count == 0)
                __instance.allEats.AddRange(__instance.slimeDefinition.Diet.GetDietIdentifiableIds());
            var gordo = __instance.GetComponent<NetworkGordo>();
            var food = col.GetComponent<NetworkActor>();
            var identifiable = col.GetComponent<Identifiable>();
            if (gordo == null || string.IsNullOrEmpty(gordo.ID) || food == null || identifiable == null ||
                !__instance.allEats.Contains(identifiable.id)) return false;

            // Capture before the original destroys the food. Only the host runs
            // MaybeEat, the burst coroutine, and GiveRewards.
            __state = new PacketGordoEat
            {
                ID = gordo.ID,
                FoodID = food.ID,
                Position = col.transform.position,
                Rotation = col.transform.rotation
            };
            foreach (var entry in __instance.slimeDefinition.Diet.EatMap)
                if (entry.eats == identifiable.id) { __state.Favorite = entry.isFavorite; break; }

            if (Globals.IsServer) return true;
            if (food.IsLocal && gordo.PendingFood.Add(food.ID))
            {
                __state.Request = true;
                __state.Send();
            }
            __state = null;
            return false;
        }

        static void Postfix(GordoEat __instance, bool __result, PacketGordoEat __state)
        {
            if (!Globals.IsServer || !__result || __state == null) return;
            __state.Count = __instance.GetEatenCount();
            __state.Send();
            SRMP.Log($"[Gordo] {__state.ID}: food={__state.FoodID}, count={__state.Count}/{__instance.GetTargetCount()}");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var start = AccessTools.Method(typeof(MonoBehaviour), "StartCoroutine", new[] { typeof(IEnumerator) });
            var replacement = AccessTools.Method(typeof(GordoEat_MaybeEat), nameof(StartBurst));
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(start))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                }
                yield return instruction;
            }
        }

        static Coroutine StartBurst(MonoBehaviour owner, IEnumerator burst)
        {
            // The host may be in a different region. Unity does not start a
            // coroutine on a proxied Gordo, and stops it when its region hides.
            return Globals.IsServer
                ? SRSingleton<GameContext>.Instance.StartCoroutine(CompleteHostBurst(owner, burst))
                : owner.StartCoroutine(burst);
        }

        static IEnumerator CompleteHostBurst(MonoBehaviour owner, IEnumerator burst)
        {
            try
            {
                while (owner != null && Globals.IsServer && Globals.GameLoaded && burst.MoveNext())
                    yield return burst.Current;
            }
            finally { (burst as System.IDisposable)?.Dispose(); }
        }
    }

    [HarmonyPatch(typeof(GordoEat), "ImmediateReachedTarget")]
    class GordoEat_ImmediateReachedTarget
    {
        static bool Prefix(GordoEat __instance)
        {
            if (!Globals.IsClient) return true;
            // Start can also reach here when loading a snapshot at the threshold.
            __instance.GetComponent<NetworkGordo>()?.Burst();
            return false;
        }

        static void Postfix(GordoEat __instance)
        {
            if (!Globals.IsServer || Globals.HandlePacket) return;
            var gordo = __instance.GetComponent<NetworkGordo>();
            if (gordo != null && !string.IsNullOrEmpty(gordo.ID))
                new PacketGordoEat { ID = gordo.ID, Count = -1 }.Send();
        }
    }
}
