using FishingMachine.Content.Tiles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using TPML.Content.IO;

namespace FishingMachine.Content.IO
{
    /// <summary>
    /// 自动钓鱼机世界伴随存档
    /// 作者: SaintCirno9
    /// </summary>
    public class FishingMachineFileData
    {
        public string WorldName { get; set; }
        public int WorldID { get; set; }
        public List<FishingMachineSaveEntry> Machines { get; set; } = new List<FishingMachineSaveEntry>();
    }

    public class FishingMachineSaveEntry
    {
        public int X;
        public int Y;
        public int LocateX;
        public int LocateY;

        public ItemData FishingPole { get; set; }
        public ItemData Bait { get; set; }
        public ItemData Accessory { get; set; }
        public List<ItemData> Fish { get; set; } = new List<ItemData>();

        public bool CatchCrates;
        public bool CatchAccessories;
        public bool CatchTools;
        public bool CatchWhiteRarityCatches;
        public bool CatchNormalCatches;
        public bool AutoDeposit;
        public bool InfiniteBait;
        public List<int> ExcludedItems = new List<int>();
    }

    public class ItemData
    {
        public int Type;
        public int Stack;
        public int Prefix;
        public bool Favorited;
        public int Slot = -1;
    }

    /// <summary>
    /// 自动钓鱼机序列化管理器
    /// </summary>
    public static class FishingMachineSaveManager
    {
        private static string GetSavePath()
        {
            string worldName = SidecarSaveManager.CleanFileName(Main.worldName ?? "unknown");
            string fileName = $"FishingMachine_{worldName}_{Main.worldID}.tpml_data";
            return Path.Combine(SidecarSaveManager.SaveDirectory, fileName);
        }

        public static void SaveMachines()
        {
            try
            {
                FishingMachineFileData data = new FishingMachineFileData
                {
                    WorldName = Main.worldName,
                    WorldID = Main.worldID
                };

                foreach (var kvp in FishingMachineTileManager.ActiveEntities)
                {
                    TEFishingMachine m = kvp.Value;
                    if (m == null) continue;

                    FishingMachineSaveEntry entry = new FishingMachineSaveEntry
                    {
                        X = m.Position.X,
                        Y = m.Position.Y,
                        LocateX = m.locatePoint.X,
                        LocateY = m.locatePoint.Y,
                        FishingPole = SerializeItem(m.fishingPole),
                        Bait = SerializeItem(m.bait),
                        Accessory = SerializeItem(m.accessory),
                        CatchCrates = m.CatchCrates,
                        CatchAccessories = m.CatchAccessories,
                        CatchTools = m.CatchTools,
                        CatchWhiteRarityCatches = m.CatchWhiteRarityCatches,
                        CatchNormalCatches = m.CatchNormalCatches,
                        AutoDeposit = m.AutoDeposit,
                        InfiniteBait = m.InfiniteBait,
                        ExcludedItems = new List<int>(m.ExcludedItems ?? new List<int>())
                    };

                    for (int i = 0; i < m.fish.Length; i++)
                    {
                        ItemData fd = SerializeItem(m.fish[i]);
                        if (fd != null)
                        {
                            fd.Slot = i;
                            entry.Fish.Add(fd);
                        }
                    }

                    data.Machines.Add(entry);
                }

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception ex)
            {
                TPML.Core.Logging.LogManager.GetLogger("FishingMachine").Error("自动钓鱼机世界存档失败", ex);
            }
        }

        public static void LoadMachines()
        {
            try
            {
                FishingMachineTileManager.ClearAll();
                string path = GetSavePath();
                if (!File.Exists(path)) return;

                FishingMachineFileData data = JsonConvert.DeserializeObject<FishingMachineFileData>(File.ReadAllText(path));
                if (data?.Machines == null) return;

                foreach (FishingMachineSaveEntry entry in data.Machines)
                {
                    Point16 pos = new Point16(entry.X, entry.Y);
                    if (FishingMachineTileManager.ActiveEntities.ContainsKey(pos)) continue;

                    Tile t = Framing.GetTileSafely(pos.X, pos.Y);
                    if (!t.active())
                    {
                        // 物块已随世界丢失时不再还原实体
                        continue;
                    }

                    TEFishingMachine entity = new TEFishingMachine(pos);
                    entity.fishingPole = DeserializeItem(entry.FishingPole);
                    entity.bait = DeserializeItem(entry.Bait);
                    entity.accessory = DeserializeItem(entry.Accessory);
                    entity.CatchCrates = entry.CatchCrates;
                    entity.CatchAccessories = entry.CatchAccessories;
                    entity.CatchTools = entry.CatchTools;
                    entity.CatchWhiteRarityCatches = entry.CatchWhiteRarityCatches;
                    entity.CatchNormalCatches = entry.CatchNormalCatches;
                    entity.AutoDeposit = entry.AutoDeposit;
                    entity.InfiniteBait = entry.InfiniteBait;
                    entity.ExcludedItems = new List<int>(entry.ExcludedItems ?? new List<int>());

                    if (entry.LocateX >= 0 && entry.LocateY >= 0 && Framing.GetTileSafely(entry.LocateX, entry.LocateY).liquid > 0)
                    {
                        entity.locatePoint = new Point16(entry.LocateX, entry.LocateY);
                        entity.RefreshPond();
                    }
                    else
                    {
                        entity.FindNearbyWater();
                    }

                    int fishIndex = 0;
                    foreach (ItemData fd in entry.Fish)
                    {
                        Item item = DeserializeItem(fd);
                        if (item == null) continue;

                        int slot = fd.Slot;
                        if (slot < 0 || slot >= entity.fish.Length)
                        {
                            if (fishIndex >= entity.fish.Length) continue;
                            slot = fishIndex;
                        }

                        entity.fish[slot] = item;
                        fishIndex++;
                    }

                    FishingMachineTileManager.ActiveEntities[pos] = entity;
                }
            }
            catch (Exception ex)
            {
                TPML.Core.Logging.LogManager.GetLogger("FishingMachine").Error("自动钓鱼机世界存档读取失败", ex);
            }
        }

        private static ItemData SerializeItem(Item item)
        {
            if (item == null || item.IsAir || item.type <= 0) return null;
            return new ItemData
            {
                Type = item.type,
                Stack = item.stack,
                Prefix = item.prefix,
                Favorited = item.favorited
            };
        }

        private static Item DeserializeItem(ItemData data)
        {
            if (data == null || data.Type <= 0) return new Item();
            try
            {
                Item item = new Item();
                item.SetDefaults(data.Type);
                item.stack = Math.Max(1, Math.Min(data.Stack, item.maxStack));
                if (data.Prefix > 0) item.Prefix(data.Prefix);
                item.favorited = data.Favorited;
                return item;
            }
            catch
            {
                return new Item();
            }
        }
    }
}