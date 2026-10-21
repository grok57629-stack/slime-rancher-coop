using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SplashOnTrigger))]
    [HarmonyPatch("SpawnAndPlayFX")]
    class SplashOnTrigger_SpawnAndPlayFX
    {
        static void Prefix(SplashOnTrigger __instance, GameObject prefab, Collider collider)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            Ray ray = new Ray(collider.gameObject.transform.position, Vector3.down);
            float num = float.PositiveInfinity;
            Vector3 position = collider.gameObject.transform.position;
            Collider[] array = __instance.splashColliders;
            for (int i = 0; i < array.Length; i++)
            {
                RaycastHit raycastHit;
                if (array[i].Raycast(ray, out raycastHit, 2f) && raycastHit.distance < num)
                {
                    num = raycastHit.distance;
                    position = raycastHit.point;
                }
            }

            new PacketGlobalFX()
            {
                Name = prefab.name,
                Position = position
            }.Send();
        }
    }
}