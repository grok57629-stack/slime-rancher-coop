using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
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
    [HarmonyPatch(typeof(EchoNet))]
    [HarmonyPatch("ResetSpawnTime")]
    class EchoNet_ResetSpawnTime
    {
        static void Postfix(EchoNet __instance)
        {
            if (!Globals.IsMultiplayer) return;

            var gadget = __instance?.GetComponentInParent<NetworkGadgetSite>();
            if (gadget != null)
            {
                new PacketGadgetEchoNetTime()
                {
                    ID = gadget.Site.id
                }.Send();
            }
        }
    }
}