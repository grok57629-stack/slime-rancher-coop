using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.LandPlots)]
    public class PacketLandplots : Packet
    {
        public struct LandPlotData
        {
            public string ID;
            public LandPlotModel Model;
        }

        public List<LandPlotData> LandPlots { get; set; }

        public PacketLandplots() { }
        public PacketLandplots(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(LandPlots.Count);
            foreach(var landPlot in LandPlots)
            {
                om.Write(landPlot.ID);
                om.Write(landPlot.Model.nextFeedingTime);
                om.Write(landPlot.Model.remainingFeedOperations);
                om.Write((byte)landPlot.Model.feederCycleSpeed);
                om.Write(landPlot.Model.collectorNextTime);
                om.Write(landPlot.Model.attachedDeathTime);
                om.Write((byte)landPlot.Model.typeId);
                om.Write((byte)landPlot.Model.attachedId);
                om.Write((ushort)landPlot.Model.attachedResourceId);
                om.Write(landPlot.Model.ashUnits);

                om.Write(landPlot.Model.upgrades.Count);
                foreach (var upgrade in landPlot.Model.upgrades)
                {
                    om.Write((byte)upgrade);
                }

                om.Write(landPlot.Model.siloStorageIndices.Length);
                foreach (var indice in landPlot.Model.siloStorageIndices)
                {
                    om.Write(indice);
                }

                om.Write(landPlot.Model.siloAmmo.Count);
                foreach (var ammo in landPlot.Model.siloAmmo)
                {
                    om.Write((byte)ammo.Key);

                    om.Write(ammo.Value.usableSlots);
                    om.Write(ammo.Value.slots.Length);
                    foreach (var slot in ammo.Value.slots)
                    {
                        om.Write(slot != null);
                        if (slot != null)
                        {
                            om.Write((ushort)slot.id);
                            om.Write(slot.count);
                            om.Write(slot.emotions != null);
                            if (slot.emotions != null)
                            {
                                om.Write(slot.emotions.Count);
                                foreach (var emotion in slot.emotions)
                                {
                                    om.Write((byte)emotion.Key);
                                    om.Write(emotion.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            LandPlots = new List<LandPlotData>();
            int plotCount = im.ReadInt32();
            for (int i = 0; i < plotCount; i++)
            {
                var plotData = new LandPlotData();

                plotData.ID = im.ReadString();
                plotData.Model = new LandPlotModel();
                plotData.Model.nextFeedingTime = im.ReadDouble();
                plotData.Model.remainingFeedOperations = im.ReadInt32();
                plotData.Model.feederCycleSpeed = (SlimeFeeder.FeedSpeed)im.ReadByte();
                plotData.Model.collectorNextTime = im.ReadDouble();
                plotData.Model.attachedDeathTime = im.ReadDouble();
                plotData.Model.typeId = (LandPlot.Id)im.ReadByte();
                plotData.Model.attachedId = (SpawnResource.Id)im.ReadByte();
                plotData.Model.attachedResourceId = (Identifiable.Id)im.ReadUInt16();
                plotData.Model.ashUnits = im.ReadFloat();

                int upgradeCount = im.ReadInt32();
                for (int j = 0; j < upgradeCount; j++)
                {
                    plotData.Model.upgrades.Add((LandPlot.Upgrade)im.ReadByte());
                }

                int indicesCount = im.ReadInt32();
                plotData.Model.siloStorageIndices = new int[indicesCount];
                for (int j = 0; j < indicesCount; j++)
                {
                    plotData.Model.siloStorageIndices[j] = im.ReadInt32();
                }

                int ammoCount = im.ReadInt32();
                for (int j = 0; j < ammoCount; j++)
                {
                    var ammoModel = new AmmoModel();

                    var key = im.ReadByte();
                    ammoModel.usableSlots = im.ReadInt32();
                    var slotCount = im.ReadInt32();
                    ammoModel.slots = new Ammo.Slot[slotCount];
                    for (int k = 0; k < slotCount; k++)
                    {
                        if (im.ReadBoolean())
                        {
                            ammoModel.slots[k] = new Ammo.Slot((Identifiable.Id)im.ReadUInt16(), im.ReadInt32());
                            if (im.ReadBoolean())
                            {
                                int emotionCount = im.ReadInt32();
                                ammoModel.slots[k].emotions = new SlimeEmotionData();
                                for (int l = 0; l < emotionCount; l++)
                                {
                                    ammoModel.slots[k].emotions.Add((SlimeEmotions.Emotion)im.ReadByte(), im.ReadFloat());
                                }
                            }
                        }
                    }
                    plotData.Model.siloAmmo.Add((SiloStorage.StorageType)key, ammoModel);
                }
                LandPlots.Add(plotData);
            }
        }
    }
}
