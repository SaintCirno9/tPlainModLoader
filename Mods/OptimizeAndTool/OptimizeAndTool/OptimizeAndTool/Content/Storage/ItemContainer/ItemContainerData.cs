using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;
using TPML.Content.IO;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 实体收纳容器接口定义（继承自通用 IBagInventory）
    /// </summary>
    public interface IItemContainer : IBagInventory
    {
        bool AutoStorage { get; set; }
        bool AutoSortEnabled { get; set; }

        bool MeetEntryCriteria(Item item);
        void CollectFromAllInventories(Player player);
        void QuickStackFromPlayer(Player player);
        void WithdrawAll(Player player, Func<Item, bool> filter = null);
        void AutoSort();
        int GetStoredCount();
        List<Item> GetStoredItems();
    }

    /// <summary>
    /// 通用大型实体收纳容器物品基类（药水袋与旗帜盒共用核心数据与持久化引擎）
    /// 完全基于 TPML 伴随存档 (Sidecar) CustomData 序列化实现独立实体容器数据绑定。
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ItemContainerItem : ModItem, IItemContainer, IToolbarCustomActions
    {
        public abstract int Capacity { get; }
        public abstract string ContainerTitle { get; }
        public string Title => ContainerTitle;

        public Item[] Slots { get; protected set; }
        public bool AutoStorage { get; set; } = true;
        public bool AutoSortEnabled { get; set; } = false;

        public virtual bool CanFavorite => true;
        public virtual bool ShowModSidebar => true;
        public virtual bool ShowFilterBar => true;

        /// <summary>
        /// 正在向个人背包转移/取出物品的防重入标志（彻底防止一键取出或快速提取时被自动收纳误吸回）
        /// </summary>
        public static bool IsTransferringOut = false;

        public event Action OnSlotsChanged;
        public void TriggerSlotsChanged()
        {
            OnSlotsChanged?.Invoke();
            if (Main.netMode != 2 && !Main.gameMenu)
            {
                Recipe.UpdateRecipeList();
            }
        }

        public ItemContainerItem()
        {
            InitSlots();
        }

        protected void InitSlots()
        {
            if (Slots == null || Slots.Length != Capacity)
            {
                Slots = new Item[Capacity];
                for (int i = 0; i < Capacity; i++) Slots[i] = new Item();
            }
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            InitSlots();
        }

        public abstract bool MeetEntryCriteria(Item item);
        public bool MeetEntryCriteria(Item item, int targetSlot = -1) => MeetEntryCriteria(item);

        /// <summary>
        /// 当玩家拾取物品时触发此容器的拦截处理（如自动存入、自动售卖或销毁）
        /// </summary>
        /// <param name="player">拾取物品的玩家</param>
        /// <param name="item">被拾取的物品（若被吸收/处理完毕需设置 stack=0 或 TurnToAir）</param>
        /// <returns>若成功拦截且物品已被完全消耗返回 true；否则返回 false</returns>
        public virtual bool OnPickupIntercept(Player player, Item item)
        {
            if (!AutoStorage || item == null || item.IsAir || !MeetEntryCriteria(item)) return false;

            int origStack = item.stack;
            TryDeposit(item, sort: true);
            int absorbed = origStack - item.stack;
            if (absorbed > 0)
            {
                PopupText.NewText(PopupTextContext.RegularItemPickup, item, player.Center, absorbed, false, false);
            }
            return item.stack <= 0;
        }

        public int GetStoredCount()
        {
            if (Slots == null) return 0;
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
            if (Slots == null) return list;
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
            InitSlots();

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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                if (AutoSortEnabled && sort)
                {
                    SortInternal();
                }
                TriggerSlotsChanged();
            }

            return depositedAny;
        }

        public bool TryDepositFromSlot(Item[] inv, int slot, bool justCheck)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return false;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited || item.stack <= 0) return false;
            if (!MeetEntryCriteria(item)) return false;
            InitSlots();

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

        public void DepositAll(Player player) => CollectFromAllInventories(player);
        public void QuickStack(Player player) => QuickStackFromPlayer(player);
        public void LootAll(Player player, Func<Item, bool> filter = null) => WithdrawAll(player, filter);
        public void Sort() => AutoSort();
        public string GetCapacityText() => $"已存: {GetStoredCount()}/{Capacity}";
        public virtual bool IsDynamicCapacity => false;
        public virtual void EnsureTrailingEmptySlots(int trailingCount = 10) { }
        public virtual void ExpandCapacity(int addedCount) { }

        public IEnumerable<BagToolbarButton> GetCustomToolbarButtons()
        {
            yield return new BagToolbarButton(
                () => $"拾取时自动吸入收纳: {(AutoStorage ? "[开启]" : "[关闭]")}",
                () => "Images/Item_5010",
                () =>
                {
                    AutoStorage = !AutoStorage;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    TriggerSlotsChanged();
                },
                () => AutoStorage ? Color.White : Color.Gray * 0.5f
            );
        }

        public void CollectFromAllInventories(Player player)
        {
            if (player == null) return;
            InitSlots();
            bool movedAny = false;

            // 收集背包 (0..49, 跳过 favorited 与 容器自身)
            if (player.inventory != null)
            {
                for (int i = 0; i < 50; i++)
                {
                    Item pItem = player.inventory[i];
                    if (pItem != null && !pItem.IsAir && !pItem.favorited && MeetEntryCriteria(pItem) && pItem != Item)
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
                    if (bItem != null && !bItem.IsAir && !bItem.favorited && MeetEntryCriteria(bItem) && bItem != Item)
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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                if (AutoSortEnabled) SortInternal();
                TriggerSlotsChanged();
            }
        }

        public void QuickStackFromPlayer(Player player)
        {
            if (player == null || player.inventory == null) return;
            InitSlots();
            bool movedAny = false;

            for (int i = 10; i < 50; i++)
            {
                Item pItem = player.inventory[i];
                if (pItem == null || pItem.IsAir || pItem.favorited || !MeetEntryCriteria(pItem) || pItem == Item) continue;

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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                if (AutoSortEnabled) SortInternal();
                TriggerSlotsChanged();
            }
        }

        public void WithdrawAll(Player player) => WithdrawAll(player, null);

        public void WithdrawAll(Player player, Func<Item, bool> filter = null)
        {
            if (player == null || player.inventory == null) return;
            InitSlots();
            bool movedAny = false;

            try
            {
                IsTransferringOut = true;
                for (int i = 0; i < Slots.Length; i++)
                {
                    Item item = Slots[i];
                    if (item == null || item.IsAir || item.favorited) continue;
                    if (filter != null && !filter(item)) continue;

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
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
                TriggerSlotsChanged();
            }
        }

        public void AutoSort()
        {
            SortInternal();
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab);
            TriggerSlotsChanged();
        }

        private void SortInternal()
        {
            InitSlots();
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

            // 2. 排序 (保留 favorited 位置)
            var items = new List<Item>();
            var favPositions = new Dictionary<int, Item>();

            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && !Slots[i].IsAir && Slots[i].type > 0)
                {
                    if (Slots[i].favorited) favPositions[i] = Slots[i];
                    else items.Add(Slots[i]);
                }
            }

            items.Sort((x, y) =>
            {
                if (x.type != y.type) return x.type.CompareTo(y.type);
                if (x.prefix != y.prefix) return x.prefix.CompareTo(y.prefix);
                return y.stack.CompareTo(x.stack);
            });

            int listIdx = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (favPositions.ContainsKey(i))
                {
                    Slots[i] = favPositions[i];
                }
                else if (listIdx < items.Count)
                {
                    Slots[i] = items[listIdx++];
                }
                else
                {
                    Slots[i] = new Item();
                }
            }
        }

        public override void SaveData(TagCompound tag)
        {
            tag["autoStorage"] = AutoStorage;
            tag["autoSort"] = AutoSortEnabled;

            var list = new List<TagCompound>();
            if (Slots != null)
            {
                for (int i = 0; i < Capacity; i++)
                {
                    Item it = Slots[i];
                    if (it != null && !it.IsAir && it.stack > 0)
                    {
                        var entryTag = new TagCompound
                        {
                            ["slot"] = i,
                            ["id"] = it.type,
                            ["stack"] = it.stack,
                            ["prefix"] = it.prefix,
                            ["fav"] = it.favorited
                        };
                        if (it.type >= ItemID.Count)
                        {
                            ModItem modIt = ItemLoader.GetModItem(it.type);
                            if (modIt != null)
                            {
                                entryTag["mod"] = modIt.Mod?.Name ?? "TPML";
                                entryTag["name"] = modIt.Name;
                            }
                        }
                        list.Add(entryTag);
                    }
                }
            }
            tag["items"] = list;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag == null) return;
            AutoStorage = tag.GetBool("autoStorage");
            AutoSortEnabled = tag.GetBool("autoSort");

            InitSlots();
            if (Slots != null)
            {
                for (int i = 0; i < Slots.Length; i++) Slots[i] = new Item();
            }

            if (tag.TryGetValue("items", out object obj))
            {
                if (obj is Newtonsoft.Json.Linq.JArray jArr)
                {
                    foreach (var token in jArr)
                    {
                        int slot = token["slot"]?.ToObject<int>() ?? -1;
                        int id = token["id"]?.ToObject<int>() ?? 0;
                        int stack = token["stack"]?.ToObject<int>() ?? 1;
                        int prefix = token["prefix"]?.ToObject<int>() ?? 0;
                        bool fav = token["fav"]?.ToObject<bool>() ?? false;
                        string mod = token["mod"]?.ToString();
                        string name = token["name"]?.ToString();

                        if (!string.IsNullOrEmpty(mod) && !string.IsNullOrEmpty(name))
                        {
                            int resolved = ItemLoader.ItemType(mod, name);
                            if (resolved > 0) id = resolved;
                        }

                        if (id > 0 && Slots != null && slot >= 0 && slot < Slots.Length)
                        {
                            Item it = new Item();
                            it.SetDefaults(id);
                            it.stack = Math.Max(1, Math.Min(stack, it.maxStack));
                            if (prefix > 0) it.Prefix(prefix);
                            it.favorited = fav;
                            Slots[slot] = it;
                        }
                    }
                }
                else if (obj is List<TagCompound> tagList)
                {
                    foreach (var itemTag in tagList)
                    {
                        int slot = itemTag.GetInt("slot");
                        int id = itemTag.GetInt("id");
                        int stack = itemTag.GetInt("stack");
                        int prefix = itemTag.GetInt("prefix");
                        bool fav = itemTag.GetBool("fav");
                        string mod = itemTag.GetString("mod");
                        string name = itemTag.GetString("name");

                        if (!string.IsNullOrEmpty(mod) && !string.IsNullOrEmpty(name))
                        {
                            int resolved = ItemLoader.ItemType(mod, name);
                            if (resolved > 0) id = resolved;
                        }

                        if (id > 0 && Slots != null && slot >= 0 && slot < Slots.Length)
                        {
                            Item it = new Item();
                            it.SetDefaults(id);
                            it.stack = Math.Max(1, Math.Min(stack, it.maxStack));
                            if (prefix > 0) it.Prefix(prefix);
                            it.favorited = fav;
                            Slots[slot] = it;
                        }
                    }
                }
            }

            TriggerSlotsChanged();
        }

        public override ModItem Clone(Item newEntity)
        {
            ItemContainerItem clone = (ItemContainerItem)base.Clone(newEntity);
            clone.AutoStorage = AutoStorage;
            clone.AutoSortEnabled = AutoSortEnabled;
            clone.Slots = new Item[Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                clone.Slots[i] = (Slots != null && i < Slots.Length && Slots[i] != null) ? Slots[i].Clone() : new Item();
            }
            return clone;
        }
    }
}
