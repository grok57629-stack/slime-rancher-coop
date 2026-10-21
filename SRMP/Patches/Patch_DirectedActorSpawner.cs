using HarmonyLib;
using SRMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SRMultiplayer.Patches
{
    internal static class NetworkSpawnerAuthority
    {
        private static readonly HashSet<int> unresolvedSpawners = new HashSet<int>();

        internal static NetworkDirectedActorSpawner Resolve(DirectedActorSpawner spawner)
        {
            var networkSpawner = spawner.gameObject.GetComponent<NetworkDirectedActorSpawner>();
            if (networkSpawner != null) return networkSpawner;

            var region = spawner.gameObject.GetInParent<NetworkRegion>();
            if (region == null)
            {
                if (unresolvedSpawners.Add(spawner.GetInstanceID()))
                    SRMP.Log($"[Region] Spawner {spawner.name} has no NetworkRegion; multiplayer spawning is safely disabled for it.");
                return null;
            }

            networkSpawner = spawner.gameObject.AddComponent<NetworkDirectedActorSpawner>();
            networkSpawner.ID = NetworkIdentity.ForScenePath(spawner.gameObject.GetGameObjectPath());
            networkSpawner.Spawner = spawner;
            networkSpawner.Region = region;
            if (!Globals.Spawners.ContainsKey(networkSpawner.ID))
                Globals.Spawners.Add(networkSpawner.ID, networkSpawner);
            SRMP.Log($"[Region] Repaired missing network binding for spawner {networkSpawner.ID} in region {region.ID}.");
            return networkSpawner;
        }
    }

    [HarmonyPatch(typeof(DirectedActorSpawner))]
    [HarmonyPatch("CanSpawn")]
    class DirectedActorSpawner_CanSpawn
    {
        static bool Prefix(DirectedActorSpawner __instance, ref bool __result)
        {
            if (!Globals.IsMultiplayer) return true;

            var spawner = NetworkSpawnerAuthority.Resolve(__instance);
            if (spawner != null && spawner.IsLocal)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(DirectedActorSpawner))]
    [HarmonyPatch("CanContinueSpawning")]
    class DirectedActorSpawner_CanContinueSpawning
    {
        static bool Prefix(DirectedActorSpawner __instance, ref bool __result)
        {
            if (!Globals.IsMultiplayer) return true;

            var spawner = NetworkSpawnerAuthority.Resolve(__instance);
            if (spawner != null && spawner.IsLocal)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }
}
