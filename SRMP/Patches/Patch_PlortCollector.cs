using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;

namespace SRMultiplayer.Patches
{
    /// <summary>
    /// Centralizes collector ownership checks. During region initialization the
    /// NetworkLandplot/Region link can briefly be missing; in that case the host
    /// is the only safe authority instead of either crashing or letting every
    /// peer run collection independently.
    /// </summary>
    internal static class PlortCollectorAuthority
    {
        public static bool IsAuthoritative(PlortCollector collector)
        {
            if (collector == null || collector.model == null || collector.model.gameObj == null)
                return Globals.IsServer;

            NetworkLandplot netLandplot = collector.model.gameObj.GetComponent<NetworkLandplot>();
            if (netLandplot == null || netLandplot.Region == null || netLandplot.Region.Owner == 0)
                return Globals.IsServer;

            return netLandplot.IsLocal;
        }

        public static void StartRequested(PlortCollector collector)
        {
            if (collector == null || !IsAuthoritative(collector) || collector.timeDir == null ||
                collector.joints.Count != 0 || !collector.timeDir.HasReached(collector.forceCollectUntil)) return;
            bool previous = Globals.HandlePacket;
            Globals.HandlePacket = false;
            try { collector.DoCollection(); }
            finally { Globals.HandlePacket = previous; }
        }
    }

    [HarmonyPatch(typeof(PlortCollector), "Update")]
    class PlortCollector_Update
    {
        static bool Prefix(PlortCollector __instance)
        {
            // A region handoff must retire the old owner's physical joints.
            // The original Update still animates the replicated collection time.
            if (!Globals.IsMultiplayer) return true;
            if (!PlortCollectorAuthority.IsAuthoritative(__instance))
            {
                foreach (var joint in __instance.joints) joint.Destroy();
                __instance.joints.Clear();
                return true;
            }
            bool ready = true;
            foreach (var joint in __instance.joints)
            {
                var actor = joint.vacuumable != null ? joint.vacuumable.GetComponent<NetworkActor>() : null;
                if (actor == null || actor.IsLocal) continue;
                actor.TakeOwnership(true);
                ready = false;
            }
            // A plort already at the collector mouth must not enter storage
            // before the host acknowledges ownership of the physical actor.
            return ready;
        }
    }

    [HarmonyPatch(typeof(PlortCollector))]
    [HarmonyPatch("DoCollection")]
    class PlortCollector_DoCollection
    {
        static bool Prefix(PlortCollector __instance)
        {
            if (!Globals.IsMultiplayer)
                return true;

            return PlortCollectorAuthority.IsAuthoritative(__instance);
        }

        static void Postfix(PlortCollector __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket || !PlortCollectorAuthority.IsAuthoritative(__instance))
                return;

            if (__instance == null || __instance.model == null || __instance.model.gameObj == null)
                return;

            LandPlotLocation location = __instance.model.gameObj.GetComponent<LandPlotLocation>();
            if (location == null)
                return;

            new PacketLandPlotCollect()
            {
                ID = location.id,
                collectorNextTime = __instance.model.collectorNextTime,
                endCollectAt = __instance.endCollectAt,
                forceCollectUntil = __instance.forceCollectUntil
            }.Send();
        }
    }

    [HarmonyPatch(typeof(PlortCollector))]
    [HarmonyPatch("StartCollection")]
    class PlortCollector_StartCollection
    {
        static void Prefix(PlortCollector __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket ||
                PlortCollectorAuthority.IsAuthoritative(__instance))
                return;

            if (__instance == null || __instance.model == null || __instance.model.gameObj == null)
                return;

            if (__instance.joints.Count == 0 && __instance.timeDir.HasReached(__instance.forceCollectUntil))
            {
                LandPlotLocation location = __instance.model.gameObj.GetComponent<LandPlotLocation>();
                if (location == null)
                    return;

                new PacketLandPlotStartCollection()
                {
                    ID = location.id
                }.Send();
            }
        }
    }

}
