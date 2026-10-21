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
    [HarmonyPatch(typeof(Incinerate))]
    [HarmonyPatch("ProcessIncinerateResults", new Type[] { typeof(Identifiable.Id), typeof(int), typeof(Vector3), typeof(Quaternion) })]
    class Incinerate_ProcessIncinerateResults
    {
        static void Prefix(Incinerate __instance, Identifiable.Id id, int amount, Vector3 position, Quaternion rotation)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            Vacuumable component = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(id).GetComponent<Vacuumable>();

            new PacketIncinerateFX()
            {
                ID = __instance.GetComponentInParent<LandPlotLocation>().id,
                Small = component == null || component.size == Vacuumable.Size.NORMAL,
                Ash = __instance.ashTrough != null && __instance.ashTrough.isActiveAndEnabled && Identifiable.IsFood(id),
                Position = position,
                Rotation = rotation
            }.Send();
        }
    }
}