using System;
using System.Linq;

namespace SRMultiplayer.Networking
{
    internal static class NetworkDiagnostics
    {
        internal static void LogPacketFailure(string message, Exception exception)
        {
            string diagnostic = message + ": " + exception;
            try
            {
                SRMP.Log(diagnostic);
            }
            catch (Exception loggingException)
            {
                // Do not let a file/path failure hide or rethrow the packet error.
                try { UnityEngine.Debug.LogError(diagnostic + "\n[Network] File logger failed: " + loggingException); }
                catch { }
            }
        }

        // Explicit /netstate capture only: never add per-frame logging here.
        internal static void Capture()
        {
            if (!Globals.GameLoaded || !Globals.IsMultiplayer) return;
            SRMP.Log($"[NetState] BEGIN build={Globals.Version} role={(Globals.IsServer ? "host" : "client")} local={Globals.LocalID} ready={Globals.ClientLoaded} handling={Globals.HandlePacket}");
            var scene = SRSingleton<SceneContext>.Instance;
            SRMP.Log($"[NetState] time={scene.TimeDirector.WorldTime():R} currency={scene.PlayerState.GetCurrency()} actors={Globals.Actors.Count}");
            foreach (var region in Globals.Regions.Values.Where(r => r != null).OrderBy(r => r.ID))
                SRMP.Log($"[NetState] region={region.ID} owner={region.Owner} active={region.Region.root.activeInHierarchy}");
            foreach (var spawner in Globals.Spawners.Values.Where(s => s != null && s.Spawner != null)
                         .OrderBy(s => s.ID).Take(256))
            {
                var region = spawner.Region;
                var actorSpawner = spawner.Spawner;
                int regionId = region != null ? region.ID : 0;
                byte regionOwner = region != null ? region.Owner : (byte)0;
                bool active = actorSpawner != null && actorSpawner.gameObject != null
                    && actorSpawner.gameObject.activeInHierarchy && actorSpawner.enabled;
                SRMP.Log($"[NetState] spawner={spawner.ID} region={regionId} " +
                         $"owner={regionOwner} local={spawner.IsLocal} active={active}");
            }
            var actors = Globals.Actors.Values.Where(a => a != null).OrderBy(a => a.ID).ToArray();
            for (int offset = 0; offset < actors.Length; offset += 64)
                SRMP.Log("[NetState] actors id:owner:type:region " + string.Join(",", actors.Skip(offset).Take(64).Select(a => $"{a.ID}:{a.Owner}:{a.Ident}:{a.RegionSet}")));
            foreach (var gordo in Globals.Gordos.Values.Where(g => g != null && g.Gordo != null).OrderBy(g => g.ID))
                SRMP.Log($"[NetState] gordo={gordo.ID} count={gordo.Gordo.GetEatenCount()}/{gordo.Gordo.GetTargetCount()}");
            foreach (var site in Globals.GadgetSites.Values.Where(s => s != null).OrderBy(s => s.Site.id))
            {
                var drone = site.GetComponentInChildren<Drone>(true);
                if (drone == null || drone.droneModel == null || drone.ammo == null) continue;
                SRMP.Log($"[NetState] drone={site.Site.id} owner={site.Region?.Owner} ammo=" +
                    string.Join(",", drone.ammo.Slots.Select(slot => slot == null ? "empty" : $"{slot.id}:{slot.count}")));
            }
            foreach (var plot in Globals.LandPlots.Values.Where(p => p != null && p.Plot != null).OrderBy(p => p.Location.id))
            {
                var collector = plot.Plot.GetComponentInChildren<PlortCollector>(true);
                if (collector == null || collector.model == null) continue;
                SRMP.Log($"[NetState] collector={plot.Location.id} owner={plot.Region?.Owner} joints={collector.joints.Count} next={collector.model.collectorNextTime:R} end={collector.endCollectAt:R}");
            }
            SRMP.Log("[NetState] END");
            ChatUI.Instance?.AddChatMessage("Local network state written to the SRMP log.");
        }
    }
}
