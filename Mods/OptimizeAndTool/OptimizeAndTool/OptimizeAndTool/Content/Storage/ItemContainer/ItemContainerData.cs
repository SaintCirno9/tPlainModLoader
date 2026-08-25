using System;
using System.Collections.Generic;
using System.IO;
using OptimizeAndTool.Content.QoL;
using tContentPatch;
using tContentPatch.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 收纳袋单格数据
    /// </summary>
    public class ContainerSlotData
    {
        public int type;
        public int prefix;
        public int stack;
        public string modName;
        public string itemName;
    }

    /// <summary>
    /// 收纳袋持久化 JSON 结构
    /// </summary>
    public class ContainerJsonData
    {
        public bool AutoStorage = true;
        public bool AutoSort = false;
        public List<ContainerSlotData> Items = new List<ContainerSlotData>();
    }

    /// <summary>
    /// 通用大型物品容器基类（药水袋与旗帜盒共用核心数据引擎）
    /// 继承 ModSetting 接入统一配置与存档管道，同时支持基于角色 UUID 在 SaveData/ 下保存独立 JSON 文件，提供存取、收集、整理、提取与自动吸入
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ItemContainerStorage : ModSetting
    {
        public abstract string StorageKey { get; }
        public abstract int Capacity { get; }
        public abstract override string Title { get; }

        public override bool HasUI => false;
        public override string FilePath => $"{StorageKey}.json";
        public override Type DataType => typeof(ContainerJsonData);

        public Item[] Slots { get; protected set; }
        public bool AutoStorage { get; set; } = true;
        public bool AutoSortEnabled { get; set; } = false;

        /// <summary>
        /// 正在向个人背包转移/取出物品的防重入标志（彻底防止一键取出或快速提取时被自动收纳误吸回）
        /// </summary>
        public static bool IsTransferringOut = false;

        public event Action OnSlotsChanged;

        protected string currentLoadedUUID = null;

        public ItemContainerStorage(int capacity)
        {
            Slots = new Item[capacity];
            for (int i = 0; i < capacity; i++) Slots[i] = new Item();
        }

        public abstract bool MeetEntryCriteria(Item item);

        public static string GetCurrentPlayerUUID()
        {
            try
            {
                if (Main.ActivePlayerFileData != null)
                {
                    if (!string.IsNullOrEmpty(Main.ActivePlayerFileData.Path))
                    {
                        return Path.GetFileNameWithoutExtension(Main.ActivePlayerFileData.Path);
                    }
                    if (!string.IsNullOrEmpty(Main.ActivePlayerFileData.Name))
                    {
                        return Main.ActivePlayerFileData.Name;
                    }
                }
                if (Main.LocalPlayer?.name != null && !string.IsNullOrEmpty(Main.LocalPlayer.name))
                {
                    return Main.LocalPlayer.name;
                }
                if (!string.IsNullOrEmpty(Main.clientUUID))
                {
                    return Main.clientUUID;
                }
            }
            catch { }
            return "default";
        }

        public void EnsurePlayerLoaded()
        {
            string uuid = GetCurrentPlayerUUID();
            if (currentLoadedUUID != uuid)
            {
                LoadForPlayer(uuid);
            }
        }

        public override void Load(object v)
        {
            if (v is ContainerJsonData jsonData)
            {
                ApplyData(jsonData);
            }

            EnsurePlayerLoaded();
        }

        public void LoadForPlayer(string uuid)
        {
            currentLoadedUUID = uuid;
            string fileName = $"SaveData/{StorageKey}_{uuid}.json";
            ContainerJsonData data = null;

            bool loaded = ModFile.ReadFileTry(fileName, file =>
            {
                data = MyJson1.Get2(file, typeof(ContainerJsonData)) as ContainerJsonData;
                return true;
            });

            // 回退到无 UUID 的旧存档路径
            if (data == null)
            {
                ModFile.ReadFileTry($"SaveData/{StorageKey}.json", file =>
                {
                    data = MyJson1.Get2(file, typeof(ContainerJsonData)) as ContainerJsonData;
                    return true;
                });
            }

            if (data == null)
            {
                ModFile.ReadFileTry($"{StorageKey}.json", file =>
                {
                    data = MyJson1.Get2(file, typeof(ContainerJsonData)) as ContainerJsonData;
                    return true;
                });
            }

            if (data != null)
            {
                ApplyData(data);
            }
            else
            {
                for (int i = 0; i < Capacity; i++) Slots[i] = new Item();
                OnSlotsChanged?.Invoke();
            }
        }

        private void ApplyData(ContainerJsonData data)
        {
            if (data == null) return;

            AutoStorage = data.AutoStorage;
            AutoSortEnabled = data.AutoSort;

            for (int i = 0; i < Capacity; i++)
            {
                if (data.Items != null && i < data.Items.Count && data.Items[i] != null)
                {
                    var d = data.Items[i];
                    int itemType = d.type;

                    if (!string.IsNullOrEmpty(d.itemName))
                    {
                        int resolved = ItemLoader.ItemType(d.modName, d.itemName);
                        if (resolved > 0) itemType = resolved;
                    }

                    if (itemType > 0)
                    {
                        Item it = new Item();
                        it.SetDefaults(itemType);
                        if (d.prefix > 0) it.Prefix(d.prefix);
                        it.stack = Math.Max(1, Math.Min(d.stack, it.maxStack));
                        Slots[i] = it;
                    }
                    else
                    {
                        Slots[i] = new Item();
                    }
                }
                else
                {
                    Slots[i] = new Item();
                }
            }

            OnSlotsChanged?.Invoke();
        }

        public override object GetSaveData()
        {
            ContainerJsonData data = new ContainerJsonData
            {
                AutoStorage = AutoStorage,
                AutoSort = AutoSortEnabled,
                Items = new List<ContainerSlotData>(Capacity)
            };

            for (int i = 0; i < Capacity; i++)
            {
                Item item = Slots[i];
                var d = new ContainerSlotData();
                if (item != null && !item.IsAir && item.type > 0 && item.stack > 0)
                {
                    d.type = item.type;
                    d.prefix = item.prefix;
                    d.stack = item.stack;

                    if (item.type >= ItemID.Count)
                    {
                        ModItem modItem = ItemLoader.GetModItem(item.type);
                        if (modItem != null)
                        {
                            d.modName = modItem.Mod?.Name ?? "TPML";
                            d.itemName = modItem.Name;
                        }
                    }
                }
                data.Items.Add(d);
            }

            return data;
        }

        public void SaveNow()
        {
            string uuid = GetCurrentPlayerUUID();
            currentLoadedUUID = uuid;
            string fileName = $"SaveData/{StorageKey}_{uuid}.json";

            ContainerJsonData data = (ContainerJsonData)GetSaveData();

            // 1. 保存到 ModSetting (potionBag.json / bannerChest.json)
            NeedSave = true;
            Save();

            // 2. 保存到角色独立 UUID 存档
            ModFile.SaveFileTry(fileName, file =>
            {
                MyJson1.Save(data, file);
                return true;
            });
        }

        public int GetStoredCount()
        {
            int count = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && !Slots[i].IsAir && Slots[i].stack > 0) count++;
            }
            return count;
        }

        public List<Item> GetStoredItems()
        {
            var list = new List<Item>();
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && !Slots[i].IsAir && Slots[i].stack > 0)
                {
                    list.Add(Slots[i]);
                }
            }
            return list;
        }

        public bool TryDeposit(Item item, bool sort = true)
        {
            if (item == null || item.IsAir || item.stack <= 0) return false;
            if (!MeetEntryCriteria(item)) return false;

            bool depositedAny = false;

            // 1. 同类堆叠
            for (int i = 0; i < Slots.Length; i++)
            {
                Item target = Slots[i];
                if (target != null && !target.IsAir && target.type == item.type && target.stack < target.maxStack && Item.CanStack(target, item))
                {
                    int take = Math.Min(item.stack, target.maxStack - target.stack);
                    target.stack += take;
                    item.stack -= take;
                    depositedAny = true;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                        break;
                    }
                }
            }

            // 2. 放入空格
            if (item.stack > 0)
            {
                for (int i = 0; i < Slots.Length; i++)
                {
                    Item target = Slots[i];
                    if (target == null || target.IsAir)
                    {
                        Slots[i] = item.Clone();
                        item.TurnToAir();
                        depositedAny = true;
                        break;
                    }
                }
            }

            if (depositedAny)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                if (AutoSortEnabled && sort)
                {
                    SortInternal();
                }
                SaveNow();
                OnSlotsChanged?.Invoke();
            }

            return depositedAny;
        }

        public bool TryDepositFromSlot(Item[] inv, int slot, bool justCheck)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return false;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited || item.stack <= 0) return false;
            if (!MeetEntryCriteria(item)) return false;

            if (justCheck)
            {
                for (int i = 0; i < Slots.Length; i++)
                {
                    Item target = Slots[i];
                    if (target == null || target.IsAir) return true;
                    if (target.type == item.type && target.stack < target.maxStack && Item.CanStack(target, item)) return true;
                }
                return false;
            }

            bool res = TryDeposit(item, sort: true);
            if (item.stack <= 0) inv[slot] = new Item();
            return res;
        }

        public void AutoCollect(Player player)
        {
            CollectFromAllInventories(player);
        }

        public void CollectFromAllInventories(Player player)
        {
            if (player == null) return;
            bool movedAny = false;

            // 收集背包 (0..49, 跳过 favorited)
            if (player.inventory != null)
            {
                for (int i = 0; i < 50; i++)
                {
                    Item pItem = player.inventory[i];
                    if (pItem != null && !pItem.IsAir && !pItem.favorited && MeetEntryCriteria(pItem))
                    {
                        int orig = pItem.stack;
                        TryDeposit(pItem, sort: false);
                        if (pItem.stack <= 0) player.inventory[i] = new Item();
                        if (pItem.stack != orig) movedAny = true;
                    }
                }
            }

            // 收集随身存钱罐/保险箱等 (bank 1..4)
            Item[][] banks = new[] { player.bank?.item, player.bank2?.item, player.bank3?.item, player.bank4?.item };
            foreach (var bank in banks)
            {
                if (bank == null) continue;
                for (int i = 0; i < bank.Length; i++)
                {
                    Item bItem = bank[i];
                    if (bItem != null && !bItem.IsAir && !bItem.favorited && MeetEntryCriteria(bItem))
                    {
                        int orig = bItem.stack;
                        TryDeposit(bItem, sort: false);
                        if (bItem.stack <= 0) bank[i] = new Item();
                        if (bItem.stack != orig) movedAny = true;
                    }
                }
            }

            if (movedAny)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                if (AutoSortEnabled) SortInternal();
                SaveNow();
                OnSlotsChanged?.Invoke();
            }
        }

        public void QuickStackFromPlayer(Player player)
        {
            if (player == null || player.inventory == null) return;
            bool movedAny = false;

            for (int i = 10; i < 50; i++)
            {
                Item pItem = player.inventory[i];
                if (pItem == null || pItem.IsAir || pItem.favorited || !MeetEntryCriteria(pItem)) continue;

                for (int j = 0; j < Slots.Length; j++)
                {
                    Item target = Slots[j];
                    if (target != null && !target.IsAir && target.type == pItem.type && target.stack < target.maxStack && Item.CanStack(target, pItem))
                    {
                        int take = Math.Min(pItem.stack, target.maxStack - target.stack);
                        target.stack += take;
                        pItem.stack -= take;
                        movedAny = true;
                        if (pItem.stack <= 0)
                        {
                            player.inventory[i] = new Item();
                            break;
                        }
                    }
                }
            }

            if (movedAny)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                if (AutoSortEnabled) SortInternal();
                SaveNow();
                OnSlotsChanged?.Invoke();
            }
        }

        public void WithdrawAll(Player player)
        {
            if (player == null || player.inventory == null) return;
            bool movedAny = false;

            try
            {
                IsTransferringOut = true;
                for (int i = 0; i < Slots.Length; i++)
                {
                    Item item = Slots[i];
                    if (item == null || item.IsAir) continue;

                    int orig = item.stack;
                    Slots[i] = player.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                    if (Slots[i] == null) Slots[i] = new Item();
                    if (Slots[i].stack != orig) movedAny = true;
                }
            }
            finally
            {
                IsTransferringOut = false;
            }

            if (movedAny)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                SaveNow();
                OnSlotsChanged?.Invoke();
            }
        }

        public void AutoSort()
        {
            SortInternal();
            SoundEngine.PlaySound(SoundID.Grab);
            SaveNow();
            OnSlotsChanged?.Invoke();
        }

        private void SortInternal()
        {
            // 1. 合并堆叠
            for (int i = 0; i < Slots.Length; i++)
            {
                Item a = Slots[i];
                if (a == null || a.IsAir || a.stack >= a.maxStack) continue;
                for (int j = i + 1; j < Slots.Length; j++)
                {
                    Item b = Slots[j];
                    if (b == null || b.IsAir || b.type != a.type || !Item.CanStack(a, b)) continue;
                    int take = Math.Min(b.stack, a.maxStack - a.stack);
                    a.stack += take;
                    b.stack -= take;
                    if (b.stack <= 0) Slots[j] = new Item();
                    if (a.stack >= a.maxStack) break;
                }
            }

            // 2. 排序
            var items = new List<Item>();
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && !Slots[i].IsAir && Slots[i].type > 0)
                {
                    items.Add(Slots[i]);
                }
            }

            items.Sort((x, y) =>
            {
                if (x.type != y.type) return x.type.CompareTo(y.type);
                if (x.prefix != y.prefix) return x.prefix.CompareTo(y.prefix);
                return y.stack.CompareTo(x.stack);
            });

            for (int i = 0; i < Slots.Length; i++)
            {
                if (i < items.Count) Slots[i] = items[i];
                else Slots[i] = new Item();
            }
        }
    }

    /// <summary>
    /// 药水袋数据存储单例 (200格)
    /// </summary>
    public class PotionBagStorage : ItemContainerStorage
    {
        private static PotionBagStorage instance;
        public static PotionBagStorage Instance => instance ?? (instance = new PotionBagStorage());

        public override string StorageKey => "potionBag";
        public override int Capacity => 200;
        public override string Title => "药水袋";

        public PotionBagStorage() : base(200)
        {
            instance = this;
        }

        public override bool MeetEntryCriteria(Item item)
        {
            if (item == null || item.IsAir || item.type <= 0) return false;
            if (item.buffType > 0)
            {
                return item.consumable;
            }
            return false;
        }
    }

    /// <summary>
    /// 旗帜盒数据存储单例 (500格)
    /// </summary>
    public class BannerChestStorage : ItemContainerStorage
    {
        private static BannerChestStorage instance;
        public static BannerChestStorage Instance => instance ?? (instance = new BannerChestStorage());

        public override string StorageKey => "bannerChest";
        public override int Capacity => 500;
        public override string Title => "旗帜盒";

        public BannerChestStorage() : base(500)
        {
            instance = this;
        }

        public override bool MeetEntryCriteria(Item item)
        {
            if (item == null || item.IsAir || item.type <= 0) return false;
            return InfinitePotionAndBuff.ItemToBanner(item) >= 0;
        }
    }
}
