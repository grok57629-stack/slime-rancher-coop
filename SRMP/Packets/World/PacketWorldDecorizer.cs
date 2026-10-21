using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldDecorizer)]
    public class PacketWorldDecorizer : Packet
    {
        public Dictionary<Identifiable.Id, int> Contents { get; set; }
        public Dictionary<string, ushort> Settings { get; set; }

        public PacketWorldDecorizer() { }
        public PacketWorldDecorizer(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Contents.Count);
            foreach(var content in Contents)
            {
                om.Write((ushort)content.Key);
                om.Write(content.Value);
            }

            om.Write(Settings.Count);
            foreach(var settings in Settings)
            {
                om.Write(settings.Key);
                om.Write(settings.Value);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Contents = new Dictionary<Identifiable.Id, int>();
            int contentCount = im.ReadInt32();
            for(int i = 0; i < contentCount; i++)
            {
                Contents.Add((Identifiable.Id)im.ReadUInt16(), im.ReadInt32());
            }

            Settings = new Dictionary<string, ushort>();
            int settingsCount = im.ReadInt32();
            for(int i = 0; i < settingsCount; i++)
            {
                Settings.Add(im.ReadString(), im.ReadUInt16());
            }
        }
    }
}
