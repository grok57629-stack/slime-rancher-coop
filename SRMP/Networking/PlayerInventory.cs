using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SRMultiplayer.Networking
{
    internal static class PlayerInventory
    {
        private static string PathForWorld => Path.Combine(SRMP.ModDataPath,
            NetworkIdentity.InventoryFile(NetworkClient.Instance.HostUUID, Globals.CurrentGameName));

        internal static void Save()
        {
            if (!Globals.IsClient || !Globals.ClientLoaded || string.IsNullOrWhiteSpace(Globals.CurrentGameName)) return;
            try
            {
                var ammo = SRSingleton<SceneContext>.Instance.PlayerState.model.ammoDict;
                AtomicFile.Write(PathForWorld, stream =>
                {
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                    {
                        writer.Write(ammo.Count);
                        foreach (var entry in ammo)
                        {
                            writer.Write((byte)entry.Key);
                            writer.Write(entry.Value.usableSlots);
                            writer.Write(entry.Value.slots.Length);
                            foreach (var slot in entry.Value.slots)
                            {
                                writer.Write(slot != null);
                                if (slot == null) continue;
                                writer.Write((ushort)slot.id);
                                writer.Write(slot.count);
                                writer.Write(slot.emotions != null);
                                if (slot.emotions == null) continue;
                                writer.Write(slot.emotions.Count);
                                foreach (var emotion in slot.emotions)
                                {
                                    writer.Write((ushort)emotion.Key);
                                    writer.Write(emotion.Value);
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex) { SRMP.Log($"[Save] Inventory write failed; previous file retained: {ex}"); }
        }

        internal static void Load()
        {
            if (string.IsNullOrWhiteSpace(Globals.CurrentGameName)) return;
            try
            {
                string path = PathForWorld;
                string legacy = Path.Combine(SRMP.ModDataPath, Path.GetFileName(Globals.CurrentGameName) + ".player");
                bool migrating = !File.Exists(path) && File.Exists(legacy);
                if (migrating) path = legacy;
                if (!File.Exists(path)) return;
                var ammo = SRSingleton<SceneContext>.Instance.PlayerState.model.ammoDict;
                var staged = new Dictionary<PlayerState.AmmoMode, Ammo.Slot[]>();
                using (var reader = new BinaryReader(File.OpenRead(path)))
                {
                    int modes = ReadCount(reader, 32);
                    for (int i = 0; i < modes; i++)
                    {
                        var mode = (PlayerState.AmmoMode)reader.ReadByte();
                        ReadCount(reader, 64); // Capacity comes from the host's upgrades, not this inventory file.
                        int count = ReadCount(reader, 64);
                        if (!ammo.ContainsKey(mode) || ammo[mode].slots.Length != count || staged.ContainsKey(mode))
                            throw new InvalidDataException("Inventory layout differs from the current game/mods.");
                        var slots = new Ammo.Slot[count];
                        for (int j = 0; j < count; j++)
                        {
                            if (!reader.ReadBoolean()) continue;
                            var id = (Identifiable.Id)reader.ReadUInt16();
                            var slot = new Ammo.Slot(id, ReadCount(reader, 1000000));
                            if (reader.ReadBoolean())
                            {
                                slot.emotions = new SlimeEmotionData();
                                int emotions = ReadCount(reader, 32);
                                for (int k = 0; k < emotions; k++)
                                {
                                    var emotion = (SlimeEmotions.Emotion)reader.ReadUInt16();
                                    float value = reader.ReadSingle();
                                    if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Invalid emotion.");
                                    slot.emotions.Add(emotion, value);
                                }
                            }
                            slots[j] = slot;
                        }
                        staged.Add(mode, slots);
                    }
                    if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing inventory data.");
                }
                if (migrating)
                {
                    AtomicFile.Write(PathForWorld, stream =>
                    {
                        using (var source = File.OpenRead(legacy)) source.CopyTo(stream);
                    });
                    // Claim a legacy file once so a second host with the same
                    // world name cannot import the same inventory again.
                    File.Move(legacy, legacy + ".migrated-" + Guid.NewGuid().ToString("N"));
                    SRMP.Log($"[Save] Migrated legacy inventory to host {NetworkClient.Instance.HostUUID}");
                }
                // Commit only after every mode has decoded successfully.
                foreach (var entry in staged) ammo[entry.Key].slots = entry.Value;
                SRMP.Log($"[Save] Restored inventory for {Globals.CurrentGameName}");
            }
            catch (Exception ex) { SRMP.Log($"[Save] Inventory not applied: {ex}"); }
        }

        private static int ReadCount(BinaryReader reader, int maximum)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > maximum) throw new InvalidDataException("Invalid inventory count.");
            return count;
        }
    }
}
