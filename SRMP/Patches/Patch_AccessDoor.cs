using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(AccessDoor))]
    [HarmonyPatch("CurrState", MethodType.Setter)]
    class AccessDoor_CurrState
    {
        static void Prefix(AccessDoor __instance, ref AccessDoor.State value)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            AccessDoor door = __instance;

            string id = door.id;

            new PacketAccessDoorOpen()
            {
                ID = id,
                State = (byte)value
            }.Send();
        }
    }
}