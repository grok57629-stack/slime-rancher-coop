using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(RanchModel))]
    [HarmonyPatch("SelectPalette")]
    class RanchModel_SelectPalette
    {
        static void Prefix(RanchDirector.PaletteType type, RanchDirector.Palette pal)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket || !Globals.ClientLoaded) return;

            new PacketWorldSelectPalette()
            {
                Type = (byte)type,
                Pal = (ushort)pal
            }.Send();
        }
    }
}