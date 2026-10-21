using HarmonyLib;
using SRMultiplayer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Patches
{
    [HarmonyPatch(typeof(MailDirector))]
    [HarmonyPatch("SendMail")]
    class MailDirector_SendMail
    {
        static void Postfix(MailDirector.Type type, string key, ref bool __result)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            if (__result)
            {
                new PacketWorldMailSend()
                {
                    Type = (byte)type,
                    Key = key
                }.Send();
            }
        }
    }

    [HarmonyPatch(typeof(MailDirector))]
    [HarmonyPatch("MarkRead")]
    class MailDirector_MarkRead
    {
        static void Postfix(MailDirector.Mail mail)
        {
            if (!Globals.IsMultiplayer || Globals.HandlePacket) return;

            new PacketWorldMailRead()
            {
                Type = (byte)mail.type,
                Key = mail.key
            }.Send();
        }
    }
}