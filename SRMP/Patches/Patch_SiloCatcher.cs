using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SiloCatcher))]
    [HarmonyPatch("OnTriggerEnter")]
    class SiloCatcher_OnTriggerEnter
    {
        static bool Prefix(SiloCatcher __instance, Collider collider)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var entity = collider.GetComponent<NetworkActor>();
            return (entity != null && entity.IsLocal);
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes.InsertRange(codes.Count - 1, new CodeInstruction[] 
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SiloCatcher_OnTriggerEnter), "Transpiler_Execute"))
            });

            return codes.AsEnumerable();
        }
        
        static void Transpiler_Execute(SiloCatcher instance, Identifiable.Id id)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;
            
            string plotid = "";
            if (instance.type == SiloCatcher.Type.DECORIZER)
                plotid = instance.storageDecorizer.id;
            else if (instance.type == SiloCatcher.Type.VIKTOR_STORAGE)
                plotid = instance.storageGlitch.id;
            else if (instance.type == SiloCatcher.Type.SILO_OUTPUT_ONLY || instance.type == SiloCatcher.Type.SILO_DEFAULT)
            {
                return;
            }

            new PacketLandPlotSiloInsert()
            {
                ID = plotid,
                CatcherType = (byte)instance.type,
                Slot = instance.slotIdx,
                Ident = (ushort)id
            }.Send();
        }
    }

    [HarmonyPatch(typeof(SiloCatcher))]
    [HarmonyPatch("Remove")]
    class SiloCatcher_Remove
    {
        static void Postfix(SiloCatcher __instance, ref bool __result, ref Identifiable.Id id)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                string plotid = "";
                if (__instance.type == SiloCatcher.Type.DECORIZER)
                    plotid = __instance.storageDecorizer.id;
                else if (__instance.type == SiloCatcher.Type.VIKTOR_STORAGE)
                    plotid = __instance.storageGlitch.id;
                else if (__instance.type == SiloCatcher.Type.SILO_OUTPUT_ONLY || __instance.type == SiloCatcher.Type.SILO_DEFAULT)
                {
                    return;
                }

                new PacketLandPlotSiloRemove()
                {
                    ID = plotid,
                    SiloType = (byte)(__instance.storageSilo == null ? 0 : __instance.storageSilo.type),
                    CatcherType = (byte)__instance.type,
                    Ident = (ushort)id,
                    Slot = __instance.slotIdx
                }.Send();
            }
        }
    }
}