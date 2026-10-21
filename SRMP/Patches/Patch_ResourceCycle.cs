using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Packets;
using UnityEngine;
using SRMultiplayer.Networking;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(ResourceCycle))]
    [HarmonyPatch("Attach")]
    class ResourceCycle_Attach
    {
        static void Postfix(ResourceCycle __instance, Joint joint)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var spawnResource = joint.gameObject.GetInParent<SpawnResource>();
            if (spawnResource != null)
            {
                var index = Array.IndexOf(spawnResource.SpawnJoints, joint);
                var netSpawn = spawnResource.gameObject.GetInParent<NetworkSpawnResource>();
                new PacketActorResourceAttach()
                {
                    PlotID = netSpawn.LandPlot != null ? netSpawn.LandPlot.Plot.model.gameObj.GetComponent<LandPlotLocation>().id : "",
                    ID = __instance.GetComponent<NetworkActor>().ID,
                    ResourceID = netSpawn.ID,
                    JointIndex = index
                }.Send();
            }
            else
            {
                var networkActor = __instance.GetComponent<NetworkActor>();
                var gingerNode = joint.gameObject.GetInParent<GingerPatchNode>();
                if(networkActor != null)
                {
                    if (gingerNode != null)
                    {
                        new PacketGingerAttach()
                        {
                            ID = gingerNode.id,
                            ActorID = networkActor.ID
                        }.Send();
                    }
                    else
                    {
                        var kookadobaNode = joint.gameObject.GetInParent<NetworkKookadobaPatchNode>();
                        if (kookadobaNode != null)
                        {
                            new PacketKookadobaAttach()
                            {
                                ID = kookadobaNode.ID,
                                ActorID = networkActor.ID
                            }.Send();
                        }
                        else
                        {
                            Debug.Log("Attach didn't find target");
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ResourceCycle))]
    [HarmonyPatch("ProgressResource")]
    class ResourceCycle_ProgressResource
    {
        static bool Prefix(ResourceCycle __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netSpawnResource = __instance.gameObject.GetInParent<NetworkActor>();
            if (netSpawnResource != null && netSpawnResource.IsLocal)
            {
                return true;
            }
            if (__instance.preparingToRelease && Time.time >= __instance.releaseAt && __instance.model.state == ResourceCycle.State.RIPE)
            {
                __instance.MakeEdible();
                __instance.additionalRipenessDelegate = null;
                var rigid = __instance.GetComponent<Rigidbody>();
                rigid.isKinematic = false;
                if (__instance.preparingToRelease)
                {
                    __instance.preparingToRelease = false;
                    __instance.releaseAt = 0f;
                    __instance.toShake.localPosition = __instance.toShakeDefaultPos;
                    if (__instance.releaseCue != null)
                    {
                        SECTR_PointSource component = __instance.GetComponent<SECTR_PointSource>();
                        component.Cue = __instance.releaseCue;
                        component.Play();
                    }
                }
                rigid.WakeUp();
                __instance.Eject(rigid);
                __instance.DetachFromJoint();
                if (__instance.hasVacuumable)
                {
                    __instance.vacuumable.Pending = false;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ResourceCycle))]
    [HarmonyPatch("Ripen")]
    class ResourceCycle_Ripen
    {
        static void Postfix(ResourceCycle __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;


            var netSpawnResource = __instance.gameObject.GetInParent<NetworkActor>();
            if (netSpawnResource != null && netSpawnResource.IsLocal)
            {
                new PacketActorResourceState()
                {
                    ID = netSpawnResource.ID,
                    State = (byte)__instance.model.state,
                    ProgressTime = __instance.model.progressTime
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(ResourceCycle))]
    [HarmonyPatch("MakeEdible")]
    class ResourceCycle_MakeEdible
    {
        static void Postfix(ResourceCycle __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;


            var netSpawnResource = __instance.gameObject.GetInParent<NetworkActor>();
            if (netSpawnResource != null)
            {
                new PacketActorResourceState()
                {
                    ID = netSpawnResource.ID,
                    State = (byte)__instance.model.state,
                    ProgressTime = __instance.model.progressTime,
                    PreparingToRelease = __instance.preparingToRelease
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(ResourceCycle))]
    [HarmonyPatch("Rot")]
    class ResourceCycle_Rot
    {
        static void Postfix(ResourceCycle __instance)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;


            var netSpawnResource = __instance.gameObject.GetInParent<NetworkActor>();
            if (netSpawnResource != null && netSpawnResource.IsLocal)
            {
                new PacketActorResourceState()
                {
                    ID = netSpawnResource.ID,
                    State = (byte)__instance.model.state,
                    ProgressTime = __instance.model.progressTime
                }.Send();
            }
        }
    }
}