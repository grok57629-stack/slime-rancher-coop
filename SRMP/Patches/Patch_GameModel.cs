using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(GameModel))]
    [HarmonyPatch("RegisterStartingActor")]
    class GameModel_RegisterStartingActor
    {
        static bool Prefix(GameObject actorObj)
        {
            Identifiable.Id id = Identifiable.GetId(actorObj);

            if (!Globals.IsClient || Identifiable.SCENE_OBJECTS.Contains(id) || id == Identifiable.Id.NONE) return true;

            // A client normally suppresses scene-start actors because the host
            // already owns and replicates them. When the host has never loaded
            // this biome, however, the authoritative client is the only peer
            // able to populate the region. Let its starting actors enter the
            // normal RegisterActor path so they receive network IDs and are
            // sent to the server; clients without region authority still skip
            // them to prevent duplicate populations.
            var member = actorObj != null ? actorObj.GetComponent<RegionMember>() : null;
            bool ownsAnyMemberRegion = member != null && member.regions != null && member.regions.Any(region =>
                region != null && region.GetComponent<NetworkRegion>()?.IsLocal == true);
            if (ownsAnyMemberRegion) return true;

            GameObject.Destroy(actorObj);
            return false;
        }
    }

    [HarmonyPatch(typeof(GameModel))]
    [HarmonyPatch("RegisterActor")]
    class GameModel_RegisterActor
    {
        static void Postfix(long actorId, GameObject gameObj, RegionRegistry.RegionSetId regionSetId)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            Identifiable.Id id = Identifiable.GetId(gameObj);
            if (id != Identifiable.Id.NONE && id != Identifiable.Id.PLAYER && !Identifiable.SCENE_OBJECTS.Contains(id))
            {
                var netActor = gameObj.AddComponent<NetworkActor>();
                netActor.ID = Utils.GetRandomActorID();
                netActor.Owner = Globals.LocalID;
                netActor.Ident = (ushort)id;
                netActor.RegionSet = (byte)regionSetId;
                netActor.PositionRotationUpdate(gameObj.transform.position, gameObj.transform.rotation, true);
                if (Globals.IsServer)
                {
                    netActor.KnownPlayers.AddRange(Globals.Players.Values.Where(p => p.HasLoaded));
                }

                new PacketActorSpawn()
                {
                    ID = netActor.ID,
                    Ident = (ushort)id,
                    Owner = netActor.Owner,
                    RegionSet = (byte)regionSetId,
                    Position = gameObj.transform.position,
                    Rotation = gameObj.transform.rotation
                }.Send();

                Globals.Actors.Add(netActor.ID, netActor);
            }
        }
    }

    [HarmonyPatch(typeof(GameModel))]
    [HarmonyPatch("RegisterLandPlot")]
    class GameModel_RegisterLandPlot
    {
        static void Postfix(string plotId, GameObject plotLocObj)
        {
            var netLandplot = plotLocObj.GetOrAddComponent<NetworkLandplot>();
            netLandplot.Plot = plotLocObj.GetComponentInChildren<LandPlot>(true);
        }
    }

    [HarmonyPatch(typeof(GameModel))]
    [HarmonyPatch("RegisterResourceSpawner")]
    class GameModel_RegisterResourceSpawner
    {
        static void Postfix(Vector3 pos, SpawnResourceModel.Participant part)
        {
            var netSpawnResource = ((SpawnResource)part).gameObject.GetOrAddComponent<NetworkSpawnResource>();
            netSpawnResource.ID = NetworkIdentity.ForScenePath(((SpawnResource)part).gameObject.GetGameObjectPath());
            netSpawnResource.SpawnResource = ((SpawnResource)part);
            if(netSpawnResource.LandPlot == null)
                netSpawnResource.LandPlot = ((SpawnResource)part).GetComponentInParent<NetworkLandplot>(true);
            if(netSpawnResource.Region == null)
                netSpawnResource.Region = ((SpawnResource)part).GetComponentInParent<NetworkRegion>(true);

            if(!Globals.SpawnResources.ContainsKey(netSpawnResource.ID))
            {
                Globals.SpawnResources.Add(netSpawnResource.ID, netSpawnResource);
            }
        }
    }
}
