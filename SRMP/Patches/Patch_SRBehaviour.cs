using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SRMultiplayer.Packets;
using UnityEngine;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(SRBehaviour))]
    [HarmonyPatch("SpawnAndPlayFX", new Type[] { typeof(GameObject), typeof(GameObject), typeof(Vector3), typeof(Quaternion) })]
    class SRBehaviour_SpawnAndPlayFX
    {
        static void Postfix(GameObject prefab, GameObject parentObject, Vector3 position, Quaternion rotation)
        {
            //SRMP.Log("SRBehaviour_SpawnAndPlayFX " + prefab.name);
        }
    }
}