using HarmonyLib;
using MonomiPark.SlimeRancher.Regions;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(Region))]
    [HarmonyPatch("Proxy")]
    class Region_Proxy
    {
        static void Postfix(Region __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket || !Globals.GameLoaded) return;

            var netRegion = __instance.GetComponent<NetworkRegion>();
            if (Globals.IsClient)
            {
                new PacketRegionChange()
                {
                    ID = netRegion.ID,
                    Load = false
                }.Send();
            }
            else if (Globals.LocalPlayer != null)
            {
                netRegion.RemovePlayer(Globals.LocalPlayer);
            }

            if (netRegion.IsLocal)
            {
                netRegion.DropOwnership();
            }
        }
    }
    [HarmonyPatch(typeof(Region))]
    [HarmonyPatch("Awake")]
    class Region_Awake
    {
        static void Postfix(Region __instance)
        {
            var netRegion = __instance.gameObject.AddComponent<NetworkRegion>();
            netRegion.ID = NetworkIdentity.ForScenePath(__instance.gameObject.GetGameObjectPath());
            netRegion.Region = __instance;
            netRegion.FastForwarder = __instance.gameObject.GetComponent<RanchCellFastForwarder>();

            Globals.Regions.Add(netRegion.ID, netRegion);


            foreach (var landPlotLocation in __instance.gameObject.GetComponentsInChildren<LandPlotLocation>(true))
            {
                var netLandplot = landPlotLocation.gameObject.GetOrAddComponent<NetworkLandplot>();
                netLandplot.Plot = landPlotLocation.GetComponentInChildren<LandPlot>(true);
                netLandplot.Location = landPlotLocation;
                netLandplot.Region = netRegion;

                Globals.LandPlots.Add(netLandplot.Location.id, netLandplot);
            }

            foreach (var accessDoor in __instance.gameObject.GetComponentsInChildren<AccessDoor>(true))
            {
                var netAccessDoor = accessDoor.gameObject.GetOrAddComponent<NetworkAccessDoor>();
                netAccessDoor.Door = accessDoor;
                netAccessDoor.Region = netRegion;

                Globals.AccessDoors.Add(accessDoor.id, netAccessDoor);
            }

            foreach (var gordo in __instance.gameObject.GetComponentsInChildren<GordoEat>(true))
            {
                var netGordo = gordo.gameObject.GetOrAddComponent<NetworkGordo>();
                netGordo.Gordo = gordo;
                netGordo.Region = netRegion;

                Globals.Gordos.Add(netGordo.ID, netGordo);
            }

            foreach (var puzzleSlot in __instance.gameObject.GetComponentsInChildren<PuzzleSlot>(true))
            {
                var netPuzzleSlot = puzzleSlot.gameObject.GetOrAddComponent<NetworkPuzzleSlot>();
                netPuzzleSlot.Slot = puzzleSlot;
                netPuzzleSlot.Region = netRegion;

                Globals.PuzzleSlots.Add(puzzleSlot.id, netPuzzleSlot);
            }

            foreach (var masterSwitch in __instance.gameObject.GetComponentsInChildren<WorldStateMasterSwitch>(true))
            {
                var netSwitch = masterSwitch.gameObject.GetOrAddComponent<NetworkWorldStateMasterSwitch>();
                netSwitch.Switch = masterSwitch;
                netSwitch.Region = netRegion;

                Globals.Switches.Add(masterSwitch.id, netSwitch);
            }

            foreach (var gadgetSite in __instance.gameObject.GetComponentsInChildren<GadgetSite>(true))
            {
                var netGadgetSite = gadgetSite.gameObject.GetOrAddComponent<NetworkGadgetSite>();
                netGadgetSite.Site = gadgetSite;
                netGadgetSite.Region = netRegion;

                Globals.GadgetSites.Add(gadgetSite.id, netGadgetSite);
            }

            foreach (var treaturePod in __instance.gameObject.GetComponentsInChildren<TreasurePod>(true))
            {
                var netTreasurePod = treaturePod.gameObject.GetOrAddComponent<NetworkTreasurePod>();
                netTreasurePod.Pod = treaturePod;
                netTreasurePod.Region = netRegion;

                Globals.TreasurePods.Add(treaturePod.id, netTreasurePod);
            }

            foreach (var spawner in __instance.gameObject.GetComponentsInChildren<DirectedActorSpawner>(true))
            {
                var netSpawner = spawner.gameObject.GetOrAddComponent<NetworkDirectedActorSpawner>();
                netSpawner.ID = NetworkIdentity.ForScenePath(spawner.gameObject.GetGameObjectPath());
                netSpawner.Spawner = spawner;
                netSpawner.Region = netRegion;

                Globals.Spawners.Add(netSpawner.ID, netSpawner);
            }

            foreach (var exchangeAcceptor in __instance.gameObject.GetComponentsInChildren<ExchangeAcceptor>(true))
            {
                var netAcceptor = exchangeAcceptor.gameObject.GetOrAddComponent<NetworkExchangeAcceptor>();
                netAcceptor.ID = NetworkIdentity.ForScenePath(exchangeAcceptor.gameObject.GetGameObjectPath());
                netAcceptor.Acceptor = exchangeAcceptor;
                netAcceptor.Region = netRegion;

                Globals.ExchangeAcceptors.Add(netAcceptor.ID, netAcceptor);
            }

            foreach (var fireColumn in __instance.gameObject.GetComponentsInChildren<FireColumn>(true))
            {
                var netColumn = fireColumn.gameObject.GetOrAddComponent<NetworkFireColumn>();
                netColumn.ID = NetworkIdentity.ForScenePath(fireColumn.gameObject.GetGameObjectPath());
                netColumn.Column = fireColumn;
                netColumn.Region = netRegion;

                Globals.FireColumns.Add(netColumn.ID, netColumn);
            }

            foreach (var kookadobaPatchNode in __instance.gameObject.GetComponentsInChildren<KookadobaPatchNode>(true))
            {
                var netNode = kookadobaPatchNode.gameObject.GetOrAddComponent<NetworkKookadobaPatchNode>();
                netNode.ID = NetworkIdentity.ForScenePath(kookadobaPatchNode.gameObject.GetGameObjectPath());
                netNode.Node = kookadobaPatchNode;
                netNode.Region = netRegion;

                Globals.Kookadobas.Add(netNode.ID, netNode);
            }

            foreach (var nutcracker in __instance.gameObject.GetComponentsInChildren<Nutcracker>(true))
            {
                var netCracker = nutcracker.gameObject.GetOrAddComponent<NetworkNutcracker>();
                netCracker.ID = NetworkIdentity.ForScenePath(nutcracker.gameObject.GetGameObjectPath());
                netCracker.Cracker = nutcracker;

                Globals.Nutcrackers.Add(netCracker.ID, netCracker);
            }

            foreach (var trigger in __instance.gameObject.GetComponentsInChildren<QuicksilverAmmoReplacer>(true))
            {
                var netTrigger = trigger.gameObject.GetOrAddComponent<NetworkRaceTrigger>();
                netTrigger.ID = NetworkIdentity.ForScenePath(trigger.gameObject.GetGameObjectPath());
                netTrigger.Ammo = trigger;

                Globals.RaceTriggers.Add(netTrigger.ID, netTrigger);
            }

            foreach (var trigger in __instance.gameObject.GetComponentsInChildren<QuicksilverEnergyCheckpoint>(true))
            {
                var netTrigger = trigger.gameObject.GetOrAddComponent<NetworkRaceTrigger>();
                netTrigger.ID = NetworkIdentity.ForScenePath(trigger.gameObject.GetGameObjectPath());
                netTrigger.Checkpoint = trigger;

                Globals.RaceTriggers.Add(netTrigger.ID, netTrigger);
            }

            foreach (var trigger in __instance.gameObject.GetComponentsInChildren<QuicksilverEnergyReplacer>(true))
            {
                var netTrigger = trigger.gameObject.GetOrAddComponent<NetworkRaceTrigger>();
                netTrigger.ID = NetworkIdentity.ForScenePath(trigger.gameObject.GetGameObjectPath());
                netTrigger.Energy = trigger;

                Globals.RaceTriggers.Add(netTrigger.ID, netTrigger);
            }

            foreach (var spawnResource in __instance.gameObject.GetComponentsInChildren<SpawnResource>(true))
            {
                var netSpawnResource = spawnResource.gameObject.GetOrAddComponent<NetworkSpawnResource>();
                netSpawnResource.ID = NetworkIdentity.ForScenePath(spawnResource.gameObject.GetGameObjectPath());
                netSpawnResource.SpawnResource = spawnResource;
                netSpawnResource.Region = netRegion;
                netSpawnResource.LandPlot = spawnResource.GetComponentInParent<NetworkLandplot>(true);

                if (!Globals.SpawnResources.ContainsKey(netSpawnResource.ID))
                {
                    Globals.SpawnResources.Add(netSpawnResource.ID, netSpawnResource);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Region))]
    [HarmonyPatch("Unproxy")]
    class Region_Unproxy
    {
        static void Postfix(Region __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket || !Globals.GameLoaded) return;

            var netRegion = __instance.GetComponent<NetworkRegion>();
            if (Globals.IsClient)
            {
                new PacketRegionChange()
                {
                    ID = netRegion.ID,
                    Load = true
                }.Send();
            }
            else if(Globals.LocalPlayer != null)
            {
                netRegion.AddPlayer(Globals.LocalPlayer);
            }

            if (netRegion.Owner == 0 || Globals.IsServer)
            {
                netRegion.TakeOwnership();
            }
        }
    }
}
