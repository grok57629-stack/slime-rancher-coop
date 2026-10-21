using HarmonyLib;
using SRMultiplayer.Networking;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(AttachFashions))]
    [HarmonyPatch("Attach")]
    class AttachFashions_Attach
    {
        static void Postfix(AttachFashions __instance, Fashion fashion)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var packet = new PacketFashionAttach()
            {
                Fashion = (ushort)fashion.GetComponent<Identifiable>().id
            };
            if (__instance.slimeModel != null)
            {
                packet.IDInt = __instance.slimeModel.transform.GetComponent<NetworkActor>().ID;
                packet.Type = 1;
            }
            else if (__instance.animalModel != null)
            {
                packet.IDInt = __instance.animalModel.transform.GetComponent<NetworkActor>().ID;
                packet.Type = 1;
            }
            else if (__instance.gordoModel != null)
            {
                packet.IDString = __instance.gordoModel.gameObj.GetComponent<GordoEat>().id;
                packet.Type = 3;
            }
            else if (__instance.droneModel != null)
            {
                packet.IDString = __instance.droneModel.siteId;
                packet.Type = 4;
            }
            else if (__instance.snareModel != null)
            {
                packet.IDString = __instance.snareModel.siteId;
                packet.Type = 4;
            }
            packet.Send();
        }
    }

    [HarmonyPatch(typeof(AttachFashions))]
    [HarmonyPatch("DetachAll")]
    class AttachFashions_DetachAll
    {
        static void Postfix(AttachFashions __instance, FashionRemover remover)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            var packet = new PacketFashionDetachAll()
            {
                Position = remover.transform.position,
                Rotation = remover.transform.rotation
            };
            if (__instance.slimeModel != null)
            {
                packet.IDInt = __instance.slimeModel.transform.GetComponent<NetworkActor>().ID;
                packet.Type = 1;
            }
            else if (__instance.animalModel != null)
            {
                packet.IDInt = __instance.animalModel.transform.GetComponent<NetworkActor>().ID;
                packet.Type = 1;
            }
            else if (__instance.gordoModel != null)
            {
                packet.IDString = __instance.gordoModel.gameObj.GetComponent<GordoEat>().id;
                packet.Type = 3;
            }
            else if (__instance.droneModel != null)
            {
                packet.IDString = __instance.droneModel.siteId;
                packet.Type = 4;
            }
            else if (__instance.snareModel != null)
            {
                packet.IDString = __instance.snareModel.siteId;
                packet.Type = 4;
            }
            packet.Send();
        }
    }
}