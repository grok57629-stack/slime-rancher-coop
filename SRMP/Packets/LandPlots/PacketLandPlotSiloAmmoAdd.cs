using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlotSiloAmmoAdd)]
    public class PacketLandPlotSiloAmmoAdd : Packet
    {
        public string ID;
        public byte Type;
        public ushort Ident;
        public int Count;
        public int Slot;
        public bool Overflow;
        public SlimeEmotionData Emotions { get; set; }

        public PacketLandPlotSiloAmmoAdd() { }
        public PacketLandPlotSiloAmmoAdd(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Emotions != null);
            if (Emotions != null)
            {
                om.Write(Emotions.Count);
                foreach (var emotion in Emotions)
                {
                    om.Write((byte)emotion.Key);
                    om.Write(emotion.Value);
                }
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            if (im.ReadBoolean())
            {
                Emotions = new SlimeEmotionData();
                int emotionCount = im.ReadInt32();
                for (int i = 0; i < emotionCount; i++)
                {
                    Emotions.Add((SlimeEmotions.Emotion)im.ReadByte(), im.ReadFloat());
                }
            }
        }
    }
}
