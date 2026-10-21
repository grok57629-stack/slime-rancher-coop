using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.Gadgets)]
    public class PacketGadgets : Packet
    {
        private const int MaxGadgets = 10000;
        private const int MaxAmmoSlots = 1024;
        private const int MaxFashions = 1024;
        private const int MaxDronePrograms = 128;
        public struct GadgetSiteData
        {
            public string ID;
            public Gadget.Id gadgetId;
            public double waitForChargeupTime;
            public float yRotation;
            public GadgetModel Model;
        }
        public List<GadgetSiteData> Gadgets { get; set; }

        public PacketGadgets() { }
        public PacketGadgets(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            if (Gadgets == null || Gadgets.Count > MaxGadgets)
                throw new InvalidDataException("Invalid gadget count.");
            om.Write(Gadgets.Count);
            foreach(var gadgetData in Gadgets)
            {
                om.Write(gadgetData.ID);
                om.Write((ushort)gadgetData.gadgetId);
                om.Write(gadgetData.waitForChargeupTime);
                om.Write(gadgetData.yRotation);
                om.Write(gadgetData.Model is ExtractorModel);
                if(gadgetData.Model is ExtractorModel)
                {
                    om.Write(((ExtractorModel)gadgetData.Model).cyclesRemaining);
                    om.Write(((ExtractorModel)gadgetData.Model).queuedToProduce);
                    om.Write(((ExtractorModel)gadgetData.Model).cycleEndTime);
                    om.Write(((ExtractorModel)gadgetData.Model).nextProduceTime);
                }
                om.Write(gadgetData.Model is WarpDepotModel);
                if (gadgetData.Model is WarpDepotModel)
                {
                    om.Write(((WarpDepotModel)gadgetData.Model).isPrimary);
                    if (((WarpDepotModel)gadgetData.Model).ammo.slots.Length > MaxAmmoSlots)
                        throw new InvalidDataException("Invalid warp depot ammo slot count.");
                    om.Write(((WarpDepotModel)gadgetData.Model).ammo.slots.Length);
                    foreach(var slot in ((WarpDepotModel)gadgetData.Model).ammo.slots)
                    {
                        om.WriteAmmoSlot(slot);
                    }
                }
                om.Write(gadgetData.Model is SnareModel);
                if (gadgetData.Model is SnareModel)
                {
                    om.Write((ushort)((SnareModel)gadgetData.Model).baitTypeId);
                    om.Write((ushort)((SnareModel)gadgetData.Model).gordoTypeId);
                    om.Write(((SnareModel)gadgetData.Model).gordoEatenCount);
                    if (((SnareModel)gadgetData.Model).fashions.Count > MaxFashions)
                        throw new InvalidDataException("Invalid snare fashion count.");
                    om.Write(((SnareModel)gadgetData.Model).fashions.Count);
                    foreach(var fashion in ((SnareModel)gadgetData.Model).fashions)
                    {
                        om.Write((ushort)fashion);
                    }
                }
                om.Write(gadgetData.Model is EchoNetModel);
                if (gadgetData.Model is EchoNetModel)
                {
                    om.Write(((EchoNetModel)gadgetData.Model).lastSpawnTime);
                }
                om.Write(gadgetData.Model is DroneModel);
                if (gadgetData.Model is DroneModel)
                {
                    om.Write(((DroneModel)gadgetData.Model).position);
                    om.Write(((DroneModel)gadgetData.Model).rotation);
                    if (((DroneModel)gadgetData.Model).ammo.slots.Length > MaxAmmoSlots ||
                        ((DroneModel)gadgetData.Model).fashions.Count > MaxFashions ||
                        ((DroneModel)gadgetData.Model).programs.Length > MaxDronePrograms)
                        throw new InvalidDataException("Invalid drone inventory, fashion, or program count.");
                    om.Write(((DroneModel)gadgetData.Model).ammo.slots.Length);
                    foreach(var slot in ((DroneModel)gadgetData.Model).ammo.slots)
                    {
                        om.WriteAmmoSlot(slot);
                    }
                    om.Write(((DroneModel)gadgetData.Model).fashions.Count);
                    foreach (var fashion in ((DroneModel)gadgetData.Model).fashions)
                    {
                        om.Write((ushort)fashion);
                    }
                    om.Write(((DroneModel)gadgetData.Model).noClip);
                    om.Write(((DroneModel)gadgetData.Model).batteryDepleteTime);
                    om.Write(((DroneModel)gadgetData.Model).programs.Length);
                    foreach(var program in ((DroneModel)gadgetData.Model).programs)
                    {
                        om.Write(program.target);
                        om.Write(program.source);
                        om.Write(program.destination);
                    }
                }
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Gadgets = new List<GadgetSiteData>();
            int gadgetCount = ReadBoundedCount(im, "gadget", MaxGadgets);
            for(int i = 0; i < gadgetCount; i++)
            {
                var gadgetData = new GadgetSiteData()
                {
                    ID = im.ReadString(),
                    gadgetId = (Gadget.Id)im.ReadUInt16(),
                    waitForChargeupTime = im.ReadDouble(),
                    yRotation = im.ReadFloat()
                };
                if (im.ReadBoolean())
                {
                    var model = new ExtractorModel(gadgetData.gadgetId, gadgetData.ID, null);
                    model.cyclesRemaining = im.ReadInt32();
                    model.queuedToProduce = im.ReadInt32();
                    model.cycleEndTime = im.ReadDouble();
                    model.nextProduceTime = im.ReadDouble();
                    gadgetData.Model = model;
                }
                if (im.ReadBoolean())
                {
                    var model = new WarpDepotModel(gadgetData.gadgetId, gadgetData.ID, null);
                    model.isPrimary = im.ReadBoolean();
                    int slotCount = ReadBoundedCount(im, "warp depot ammo slot", MaxAmmoSlots);
                    model.ammo = new AmmoModel();
                    model.ammo.slots = new Ammo.Slot[slotCount];
                    for(int j = 0; j < slotCount; j++)
                    {
                        model.ammo.slots[j] = im.ReadAmmoSlot();
                    }
                    gadgetData.Model = model;
                }
                if (im.ReadBoolean())
                {
                    var model = new SnareModel(gadgetData.gadgetId, gadgetData.ID, null);
                    model.baitTypeId = (Identifiable.Id)im.ReadUInt16();
                    model.gordoTypeId = (Identifiable.Id)im.ReadUInt16();
                    model.gordoEatenCount = im.ReadInt32();
                    model.fashions = new List<Identifiable.Id>();
                    int fashionCount = ReadBoundedCount(im, "snare fashion", MaxFashions);
                    for(int j = 0; j < fashionCount; j++)
                    {
                        model.fashions.Add((Identifiable.Id)im.ReadUInt16());
                    }
                    gadgetData.Model = model;
                }
                if (im.ReadBoolean())
                {
                    var model = new EchoNetModel(gadgetData.gadgetId, gadgetData.ID, null);
                    model.lastSpawnTime = im.ReadDouble();
                    gadgetData.Model = model;
                }
                if (im.ReadBoolean())
                {
                    var model = new DroneModel(gadgetData.gadgetId, gadgetData.ID, null);
                    model.position = im.ReadVector3();
                    model.rotation = im.ReadQuaternion();
                    int slotCount = ReadBoundedCount(im, "drone ammo slot", MaxAmmoSlots);
                    model.ammo = new AmmoModel();
                    model.ammo.slots = new Ammo.Slot[slotCount];
                    for (int j = 0; j < slotCount; j++)
                    {
                        model.ammo.slots[j] = im.ReadAmmoSlot();
                    }
                    model.fashions = new List<Identifiable.Id>();
                    int fashionCount = ReadBoundedCount(im, "drone fashion", MaxFashions);
                    for (int j = 0; j < fashionCount; j++)
                    {
                        model.fashions.Add((Identifiable.Id)im.ReadUInt16());
                    }
                    model.noClip = im.ReadBoolean();
                    model.batteryDepleteTime = im.ReadDouble();
                    int programCount = ReadBoundedCount(im, "drone program", MaxDronePrograms);
                    model.programs = new DroneModel.ProgramData[programCount];
                    for(int j = 0; j < programCount; j++)
                    {
                        model.programs[j] = new DroneModel.ProgramData()
                        {
                            target = im.ReadString(),
                            source = im.ReadString(),
                            destination = im.ReadString()
                        };
                    }
                    gadgetData.Model = model;
                }
                Gadgets.Add(gadgetData);
            }
        }
    }
}
