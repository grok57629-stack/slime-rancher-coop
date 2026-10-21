using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(GordoSnare))]
    [HarmonyPatch("OnTriggerEnter")]
    class GordoSnare_OnTriggerEnter
    {
        static bool Prefix(GordoSnare __instance, Collider col)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return true;

            var netActor = col.GetComponent<NetworkActor>();
            return (netActor != null && netActor.IsLocal);
        }
    }

    [HarmonyPatch(typeof(GordoSnare))]
    [HarmonyPatch("SnareGordo", new Type[] { typeof(Identifiable.Id) })]
    class GordoSnare_SnareGordo
    {
        static void Prefix(GordoSnare __instance, Identifiable.Id id)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGadgetSnareGordo()
            {
                ID = __instance.gameObject.GetComponentInParent<GadgetSite>().id,
                Ident = (ushort)id
            }.Send();
        }

        static void Postfix(GordoSnare __instance)
        {
            var gordo = __instance.GetComponentInChildren<GordoEat>(true);
            if (gordo != null)
            {
                var netGordo = gordo.gameObject.GetOrAddComponent<NetworkGordo>();
                netGordo.Gordo = gordo;
                netGordo.Region = __instance.GetComponentInParent<NetworkRegion>(true);

                Globals.Gordos.Add(netGordo.ID, netGordo);
            }
        }
    }

    [HarmonyPatch(typeof(GordoSnare))]
    [HarmonyPatch("AttachBait", new Type[] { typeof(Identifiable.Id) })]
    class GordoSnare_AttachBait
    {
        static void Prefix(GordoSnare __instance, Identifiable.Id id)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketGadgetSnareAttach()
            {
                ID = __instance.gameObject.GetComponentInParent<GadgetSite>().id,
                Ident = (ushort)id
            }.Send();
        }
    }
}