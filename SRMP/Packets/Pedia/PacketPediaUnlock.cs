using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.PediaUnlock)]
    public class PacketPediaUnlock : Packet
    {
        public List<ushort> IDs { get; set; }

        public PacketPediaUnlock() { }
        public PacketPediaUnlock(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(IDs.Count);
            foreach (var id in IDs)
            {
                om.Write(id);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            int count = im.ReadInt32();
            IDs = new List<ushort>();
            for (int i = 0; i < count; i++)
            {
                IDs.Add(im.ReadUInt16());
            }
        }
    }
}
