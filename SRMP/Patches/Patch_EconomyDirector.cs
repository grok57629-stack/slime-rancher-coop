using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(EconomyDirector))]
    [HarmonyPatch("RegisterSold")]
    class EconomyDirector_RegisterSold
    {
        static void Postfix(EconomyDirector __instance, Identifiable.Id id, int count)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (Globals.IsClient)
            {
                new PacketWorldMarketSold()
                {
                    Ident = (ushort)id,
                    Count = count
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(EconomyDirector))]
    [HarmonyPatch("ResetPrices")]
    class EconomyDirector_ResetPrices
    {
        static bool Prefix(EconomyDirector __instance)
        {
            if (!Globals.IsMultiplayer) return true;

            return Globals.IsServer;
        }

        static void Postfix(EconomyDirector __instance, WorldModel worldModel)
        {
            if (!Globals.IsMultiplayer) return;

            if (Globals.IsServer)
            {
                new PacketWorldMarketPrices()
                {
                    Prices = __instance.currValueMap.ToDictionary(k => (ushort)k.Key, v => v.Value),
                    Saturation = worldModel.marketSaturation.ToDictionary(k => (ushort)k.Key, v => v.Value)
                }.Send();
            }
        }
    }
}