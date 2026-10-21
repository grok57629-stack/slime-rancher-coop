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
    [HarmonyPatch(typeof(QuicksilverAmmoReplacer))]
    [HarmonyPatch("OnTriggerEnter")]
    class QuicksilverAmmoReplacer_OnTriggerEnter
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            codes.InsertRange(codes.Count - 1, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(QuicksilverAmmoReplacer_OnTriggerEnter), "Transpiler_Execute"))
            });

            return codes.AsEnumerable();
        }

        static void Transpiler_Execute(QuicksilverAmmoReplacer instance)
        {
            if (!Globals.IsMultiplayer) return;

            new PacketRaceTrigger()
            {
                ID = instance.GetComponent<NetworkRaceTrigger>().ID
            }.Send();
        }
    }
}
