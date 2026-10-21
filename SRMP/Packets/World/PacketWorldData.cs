using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using MonomiPark.SlimeRancher.DataModel;

namespace SRMultiplayer.Packets
{
    [Packet(PacketType.WorldData)]
    public class PacketWorldData : Packet
    {
        public Dictionary<ushort, EconomyDirector.CurrValueEntry> Prices { get; set; }
        public Dictionary<ushort, float> Saturation { get; set; }
        public float Seed;
        public int Keys;
        public int Currency;
        public double WorldTime;
        public Dictionary<ushort, int> Progress { get; set; }
        public GadgetsModel GadgetsModel { get; set; }
        public List<byte> MapUnlocks { get; set; }
        public Dictionary<byte, ushort> Palette { get; set; }
        public List<byte> AvailUpgrades { get; set; }
        public List<byte> Upgrades { get; set; }
        public List<ushort> PediaUnlocks { get; set; }
        public List<MailDirector.Mail> Mails { get; set; }
        public List<string> LemonTrees { get; set; }

        public PacketWorldData() { }
        public PacketWorldData(NetIncomingMessage im) { Deserialize(im); }

        public override void Serialize(NetOutgoingMessage om)
        {
            base.Serialize(om);

            om.Write(Progress.Count);
            foreach (var progress in Progress)
            {
                om.Write(progress.Key);
                om.Write(progress.Value);
            }

            om.Write(GadgetsModel.blueprints.Count);
            foreach (var data in GadgetsModel.blueprints)
            {
                om.Write((ushort)data);
            }

            om.Write(GadgetsModel.blueprintLockData.Count);
            foreach (var data in GadgetsModel.blueprintLockData)
            {
                om.Write((ushort)data.Key);
                om.Write(data.Value != null);
                if (data.Value != null)
                {
                    om.Write(data.Value.lockedUntil);
                    om.Write(data.Value.timedLock);
                }
            }

            om.Write(GadgetsModel.availBlueprints.Count);
            foreach (var data in GadgetsModel.availBlueprints)
            {
                om.Write((ushort)data);
            }

            om.Write(GadgetsModel.registeredBlueprints.Count);
            foreach (var data in GadgetsModel.registeredBlueprints)
            {
                om.Write((ushort)data);
            }

            om.Write(GadgetsModel.gadgets.Count);
            foreach (var data in GadgetsModel.gadgets)
            {
                om.Write((ushort)data.Key);
                om.Write(data.Value);
            }

            om.Write(GadgetsModel.craftMatCounts.Count);
            foreach (var data in GadgetsModel.craftMatCounts)
            {
                om.Write((ushort)data.Key);
                om.Write(data.Value);
            }

            om.Write(GadgetsModel.placedGadgetCounts.Count);
            foreach (var data in GadgetsModel.placedGadgetCounts)
            {
                om.Write((ushort)data.Key);
                om.Write(data.Value);
            }

            om.Write(MapUnlocks.Count);
            foreach (var map in MapUnlocks)
            {
                om.Write(map);
            }

            om.Write(Palette.Count);
            foreach (var pal in Palette)
            {
                om.Write(pal.Key);
                om.Write(pal.Value);
            }

            om.Write(AvailUpgrades.Count);
            foreach (var upgrade in AvailUpgrades)
            {
                om.Write(upgrade);
            }

            om.Write(Upgrades.Count);
            foreach (var upgrade in Upgrades)
            {
                om.Write(upgrade);
            }

            om.Write(PediaUnlocks.Count);
            foreach (var unlock in PediaUnlocks)
            {
                om.Write(unlock);
            }

            om.Write(Prices.Count);
            foreach (var data in Prices)
            {
                om.Write(data.Key);
                om.Write(data.Value.currValue);
                om.Write(data.Value.prevValue);
            }
            om.Write(Saturation.Count);
            foreach (var data in Saturation)
            {
                om.Write(data.Key);
                om.Write(data.Value);
            }

            om.Write(Mails.Count);
            foreach (var mail in Mails)
            {
                om.Write(mail.key);
                om.Write(mail.read);
                om.Write((byte)mail.type);
            }

            om.Write(LemonTrees.Count);
            foreach(var tree in LemonTrees)
            {
                om.Write(tree);
            }
        }

        public override void Deserialize(NetIncomingMessage im)
        {
            base.Deserialize(im);

            Progress = new Dictionary<ushort, int>();
            int progressCount = im.ReadInt32();
            for (int i = 0; i < progressCount; i++)
            {
                Progress.Add(im.ReadUInt16(), im.ReadInt32());
            }

            int count = 0;
            GadgetsModel = new GadgetsModel();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                GadgetsModel.blueprints.Add((Gadget.Id)im.ReadUInt16());
            }

            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var key = im.ReadUInt16();
                GadgetDirector.BlueprintLockData data = null;
                if (im.ReadBoolean())
                {
                    data = new GadgetDirector.BlueprintLockData() { lockedUntil = im.ReadDouble(), timedLock = im.ReadBoolean() };
                }
                GadgetsModel.blueprintLockData.Add((Gadget.Id)key, data);
            }

            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                GadgetsModel.availBlueprints.Add((Gadget.Id)im.ReadUInt16());
            }

            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                GadgetsModel.registeredBlueprints.Add((Gadget.Id)im.ReadUInt16());
            }

            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                GadgetsModel.gadgets.Add((Gadget.Id)im.ReadUInt16(), im.ReadInt32());
            }

            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                GadgetsModel.craftMatCounts.Add((Identifiable.Id)im.ReadUInt16(), im.ReadInt32());
            }

            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                GadgetsModel.placedGadgetCounts.Add((Gadget.Id)im.ReadUInt16(), im.ReadInt32());
            }

            MapUnlocks = new List<byte>();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                MapUnlocks.Add(im.ReadByte());
            }

            Palette = new Dictionary<byte, ushort>();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Palette.Add(im.ReadByte(), im.ReadUInt16());
            }

            AvailUpgrades = new List<byte>();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                AvailUpgrades.Add(im.ReadByte());
            }

            Upgrades = new List<byte>();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Upgrades.Add(im.ReadByte());
            }

            PediaUnlocks = new List<ushort>();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                PediaUnlocks.Add(im.ReadUInt16());
            }

            Prices = new Dictionary<ushort, EconomyDirector.CurrValueEntry>();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Prices.Add(im.ReadUInt16(), new EconomyDirector.CurrValueEntry(0, im.ReadFloat(), im.ReadFloat(), 0));
            }

            Saturation = new Dictionary<ushort, float>();
            count = im.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Saturation.Add(im.ReadUInt16(), im.ReadFloat());
            }

            Mails = new List<MailDirector.Mail>();
            int mailCount = im.ReadInt32();
            for (int i = 0; i < mailCount; i++)
            {
                Mails.Add(new MailDirector.Mail()
                {
                    key = im.ReadString(),
                    read = im.ReadBoolean(),
                    type = (MailDirector.Type)im.ReadByte()
                });
            }

            LemonTrees = new List<string>();
            count = im.ReadInt32();
            for(int i = 0; i < count; i++)
            {
                LemonTrees.Add(im.ReadString());
            }
        }
    }
}
