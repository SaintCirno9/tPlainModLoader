using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;
using TPML.Content.Fusion;
using TPML.Content.IO;
using OptimizeAndTool.Content.Storage.ItemContainer;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 巨大额外背包：随身大容量仓库
    /// 存储数组包装为随身 Chest（bankChest=true, index=-6），经 PortableContainer 纳入制作材料来源；
    /// 格子交互复用原版 ItemSlot（context=BankItem），材料消耗为纯本地扣除。
    /// 作者: SaintCirno9
    /// </summary>
    public static class BigBag
    {
        public static readonly GetSetReset<bool> EnableBigBag = new GetSetReset<bool>(true, true);
        public static readonly GetSetReset<bool> EnableBigBagCraft = new GetSetReset<bool>(true, true);
        public static readonly GetSetReset<bool> AutoStackOnPickup = new GetSetReset<bool>(true, true);
        public static readonly GetSetReset<bool> PickupOverflowToBigBag = new GetSetReset<bool>(true, true);
        public static readonly GetSetReset<string> HotKey = new GetSetReset<string>("X", "X");
        public static readonly GetSetReset<int> Capacity = new GetSetReset<int>(100, 100);

        /// <summary>存储槽数组</summary>
        public static Item[] Slots { get; private set; } = NewSlots(100);

        /// <summary>随身 Chest 包装（制作系统消耗扣除入口，SetCapacity 时原地更新字段引用不变）</summary>
        public static Chest BagChest { get; } = CreateChest(Slots);

        /// <summary>通用背包融合数据源提供者单例</summary>
        public static readonly BigBagFusionSource FusionSource = new BigBagFusionSource();

        /// <summary>通用容器 IBagInventory 适配器单例</summary>
        public static readonly BigBagInventoryAdapter Inventory = new BigBagInventoryAdapter();

        /// <summary>容量变化后通知窗口重建格子</summary>
        public static event Action OnCapacityChanged;

        /// <summary>外部通知大背包数据发生变化并刷新 UI</summary>
        public static void NotifySlotsChanged() => OnCapacityChanged?.Invoke();
        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("bigBag", EnableBigBag),
                CommandBuild.get1("bigBagCapacity", EnableBigBag, Capacity, new CommandInt()),
                CommandBuild.get1("bigBagHotKey", EnableBigBag, HotKey, new CommandString()),
                CommandBuild.get2("bigBagCraft", EnableBigBagCraft),
                CommandBuild.get2("bigBagAutoStack", AutoStackOnPickup),
                CommandBuild.get2("bigBagPickupOverflow", PickupOverflowToBigBag)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableBigBag, "随身巨大额外背包（已接入统一快捷键系统，可在【设置->控件】中自定义按键）", "Images/Item_4131", "巨大背包"),
                UIBuild.get1(EnableBigBag, Capacity, int.Parse, "背包保底容量格数（无限动态扩容，末尾始终保留 10 个空位）", "Images/Item_87", "保底容量"),
                UIBuild.get2(EnableBigBagCraft, "巨大背包中的材料参与制作判定与消耗扣除", "Images/Item_346", "背包材料制作"),
                UIBuild.get2(AutoStackOnPickup, "拾取物品时若巨大背包已有同类物品则自动堆入", "Images/Item_5010", "拾取自动堆叠"),
                UIBuild.get2(PickupOverflowToBigBag, "本体背包装满时拾取自动存入巨大背包", "Images/Item_3813", "满包拾取溢出")
            };
        }

        static BigBag()
        {
            Capacity.OnValUpdate += v => SetCapacity(v);
            BigBagKeybind.Initialize();
            EnsureCapacitySafety();
            EnsureTrailingEmptySlots(10);
            InventoryFusionManager.RegisterSource(FusionSource);
        }

        /// <summary>
        /// 原版 ItemSlot/CoinSlot 底层静态数组动态扩容，防止大容量大背包发生越界崩溃
        /// </summary>
        public static void EnsureCapacitySafety(int requiredSize = 1000)
        {
            try
            {
                int targetSize = Math.Max(1000, requiredSize + 50);
                if (ItemSlot.inventoryGlowTimeChest != null && ItemSlot.inventoryGlowTimeChest.Length < targetSize)
                {
                    Array.Resize(ref ItemSlot.inventoryGlowTimeChest, targetSize);
                }

                if (ItemSlot.inventoryGlowHueChest != null && ItemSlot.inventoryGlowHueChest.Length < targetSize)
                {
                    Array.Resize(ref ItemSlot.inventoryGlowHueChest, targetSize);
                }

                if (CoinSlot.ChestEntries != null && CoinSlot.ChestEntries.Length < targetSize)
                {
                    Array.Resize(ref CoinSlot.ChestEntries, targetSize);
                }
            }
            catch { }
        }

        /// <summary>
        /// 确保大背包末尾始终保留指定数量的空位（若不足则自动扩充底层槽位数组）
        /// </summary>
        public static void EnsureTrailingEmptySlots(int trailingCount = 10)
        {
            if (trailingCount <= 0) trailingCount = 10;
            if (Slots == null)
            {
                Slots = NewSlots(Math.Max(Capacity.val, trailingCount));
                BagChest.item = Slots;
                BagChest.maxItems = Slots.Length;
                EnsureCapacitySafety(Slots.Length);
                OnCapacityChanged?.Invoke();
                return;
            }

            int trailingEmpty = 0;
            for (int i = Slots.Length - 1; i >= 0; i--)
            {
                if (Slots[i] == null || Slots[i].IsAir || Slots[i].stack <= 0)
                {
                    trailingEmpty++;
                }
                else
                {
                    break;
                }
            }

            if (trailingEmpty < trailingCount)
            {
                int needAdd = trailingCount - trailingEmpty;
                ExpandCapacityInternal(needAdd);
            }
        }

        /// <summary>
        /// 主动向大背包末尾扩增指定数量的空槽位
        /// </summary>
        public static void ExpandCapacity(int addedCount)
        {
            if (addedCount <= 0) return;
            ExpandCapacityInternal(addedCount);
        }

        private static void ExpandCapacityInternal(int addedCount)
        {
            int oldLen = Slots != null ? Slots.Length : 0;
            int newLen = oldLen + addedCount;
            Item[] newSlots = new Item[newLen];
            for (int i = 0; i < oldLen; i++) newSlots[i] = Slots[i];
            for (int i = oldLen; i < newLen; i++) newSlots[i] = new Item();

            Slots = newSlots;
            BagChest.item = Slots;
            BagChest.maxItems = Slots.Length;
            EnsureCapacitySafety(Slots.Length);
            OnCapacityChanged?.Invoke();
        }

        /// <summary>
        /// 设置保底最小容量（无限动态扩容，末尾始终保留 10 个空位）
        /// </summary>
        public static void SetCapacity(int minCapacity)
        {
            minCapacity = Math.Max(10, minCapacity);
            if (Slots == null || Slots.Length < minCapacity)
            {
                int add = minCapacity - (Slots?.Length ?? 0);
                if (add > 0) ExpandCapacityInternal(add);
            }
            EnsureTrailingEmptySlots(10);
        }

        /// <summary>
        /// 一键将玩家背包中所有非收藏、非快捷栏(10~49)、非钱币/弹药物品存入大背包
        /// </summary>
        public static void DepositAllFromPlayer(Player player)
        {
            if (player == null || player.inventory == null) return;

            bool movedAny = false;
            Item[] inv = player.inventory;

            // 1. 先尝试向大背包已有同类物品堆叠（跳过快捷栏 0~9，仅遍历 10~49）
            for (int i = 10; i < 50; i++)
            {
                Item pItem = inv[i];
                if (pItem == null || pItem.IsAir || pItem.favorited) continue;

                for (int j = 0; j < Slots.Length; j++)
                {
                    Item bItem = Slots[j];
                    if (bItem != null && !bItem.IsAir && bItem.type == pItem.type && bItem.stack < bItem.maxStack && Item.CanStack(bItem, pItem))
                    {
                        int canTake = Math.Min(pItem.stack, bItem.maxStack - bItem.stack);
                        bItem.stack += canTake;
                        pItem.stack -= canTake;
                        movedAny = true;
                        if (pItem.stack <= 0)
                        {
                            inv[i] = new Item();
                            break;
                        }
                    }
                }
            }

            // 2. 剩余物品放入大背包空格子（动态扩容保底）
            for (int i = 10; i < 50; i++)
            {
                Item pItem = inv[i];
                if (pItem == null || pItem.IsAir || pItem.favorited) continue;

                EnsureTrailingEmptySlots(10);

                for (int j = 0; j < Slots.Length; j++)
                {
                    Item bItem = Slots[j];
                    if (bItem == null || bItem.IsAir)
                    {
                        Slots[j] = pItem.Clone();
                        inv[i] = new Item();
                        movedAny = true;
                        break;
                    }
                }
            }

            EnsureTrailingEmptySlots(10);

            if (movedAny)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                NotifySlotsChanged();
            }
        }

        /// <summary>
        /// 一键将大背包物品取出到玩家个人背包（支持按谓词筛选）
        /// </summary>
        public static void LootAllToPlayer(Player player, Func<Item, bool> filter = null)
        {
            if (player == null || player.inventory == null) return;

            bool movedAny = false;
            try
            {
                ItemContainerItem.IsTransferringOut = true;
                for (int i = 0; i < Slots.Length; i++)
                {
                    Item bItem = Slots[i];
                    if (bItem == null || bItem.IsAir) continue;
                    if (filter != null && !filter(bItem)) continue;

                    int origStack = bItem.stack;
                    Slots[i] = player.GetItem(bItem, GetItemSettings.QuickTransferFromSlot);

                    if (Slots[i] == null) Slots[i] = new Item();
                    if (Slots[i].stack != origStack)
                    {
                        movedAny = true;
                    }
                }
            }
            finally
            {
                ItemContainerItem.IsTransferringOut = false;
            }

            if (movedAny)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                NotifySlotsChanged();
            }
        }

        /// <summary>
        /// 一键将玩家背包中与大背包已有相同物品快速堆叠过去（跳过快捷栏 0~9）
        /// </summary>
        public static void QuickStackFromPlayer(Player player)
        {
            if (player == null || player.inventory == null) return;

            bool movedAny = false;
            Item[] inv = player.inventory;

            for (int i = 10; i < 50; i++)
            {
                Item pItem = inv[i];
                if (pItem == null || pItem.IsAir || pItem.favorited) continue;

                for (int j = 0; j < Slots.Length; j++)
                {
                    Item bItem = Slots[j];
                    if (bItem != null && !bItem.IsAir && bItem.type == pItem.type && bItem.stack < bItem.maxStack && Item.CanStack(bItem, pItem))
                    {
                        int canTake = Math.Min(pItem.stack, bItem.maxStack - bItem.stack);
                        bItem.stack += canTake;
                        pItem.stack -= canTake;
                        movedAny = true;
                        if (pItem.stack <= 0)
                        {
                            inv[i] = new Item();
                            break;
                        }
                    }
                }
            }

            EnsureTrailingEmptySlots(10);

            if (movedAny)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                NotifySlotsChanged();
            }
        }

        /// <summary>
        /// 整理大背包：合并同类未满堆叠并按类别/ID 排序紧凑排列，收藏(Favorited)物品排在最前，末尾精准保留 10 个空位
        /// </summary>
        public static void SortBigBag()
        {
            // 1. 合并同类物品堆叠
            for (int i = 0; i < Slots.Length; i++)
            {
                Item a = Slots[i];
                if (a == null || a.IsAir || a.stack >= a.maxStack) continue;

                for (int j = i + 1; j < Slots.Length; j++)
                {
                    Item b = Slots[j];
                    if (b == null || b.IsAir || b.type != a.type || !Item.CanStack(a, b)) continue;

                    int canTake = Math.Min(b.stack, a.maxStack - a.stack);
                    a.stack += canTake;
                    b.stack -= canTake;
                    if (b.stack <= 0) Slots[j] = new Item();
                    if (a.stack >= a.maxStack) break;
                }
            }

            // 2. 收集非空物品并按类型及 ID 排序（收藏物品优先置顶）
            List<Item> items = new List<Item>();
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && !Slots[i].IsAir && Slots[i].type > 0)
                {
                    items.Add(Slots[i]);
                }
            }

            items.Sort((x, y) =>
            {
                // 1. 收藏优先（金色锁定物品排在最前面）
                if (x.favorited != y.favorited) return x.favorited ? -1 : 1;
                // 2. 类别 Rank
                int rankX = GetItemSortRank(x);
                int rankY = GetItemSortRank(y);
                if (rankX != rankY) return rankX.CompareTo(rankY);
                // 3. 物品 ID
                if (x.type != y.type) return x.type.CompareTo(y.type);
                // 4. 前缀
                if (x.prefix != y.prefix) return x.prefix.CompareTo(y.prefix);
                // 5. 堆叠数降序
                return y.stack.CompareTo(x.stack);
            });

            // 3. 紧凑收缩至物品数 + 10 空位（且不低于保底容量）
            int targetCap = Math.Max(Math.Max(10, Capacity.val), items.Count + 10);
            Item[] newSlots = new Item[targetCap];
            for (int i = 0; i < targetCap; i++)
            {
                if (i < items.Count) newSlots[i] = items[i];
                else newSlots[i] = new Item();
            }

            Slots = newSlots;
            BagChest.item = Slots;
            BagChest.maxItems = Slots.Length;

            EnsureCapacitySafety(Slots.Length);
            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
            OnCapacityChanged?.Invoke();
        }

        private static int GetItemSortRank(Item item)
        {
            if (item.damage > 0 && item.useStyle != 0) return 1; // 武器
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0) return 2; // 工具
            if (item.defense > 0 || item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1) return 3; // 防具
            if (item.accessory) return 4; // 饰品
            if (item.potion || item.healLife > 0 || item.healMana > 0 || item.buffType > 0) return 5; // 药水/消耗品
            if (item.ammo > 0) return 6; // 弹药
            if (item.createTile > -1 || item.createWall > -1) return 7; // 方块/墙壁/家具
            if (item.material) return 8; // 材料
            return 9; // 其他杂项
        }

        /// <summary>
        /// 拾取物品时尝试自动堆叠入大背包
        /// </summary>
        /// <returns>若物品被完全吸收存入大背包返回 true；若部分堆入或未堆入返回 false</returns>
        public static bool TryAutoStackPickup(Item newItem)
        {
            if (newItem == null || newItem.IsAir || newItem.type <= 0 || newItem.stack <= 0) return false;
            if (!EnableBigBag.val || !AutoStackOnPickup.val) return false;

            Item itemInfo = newItem.Clone();
            int originalStack = newItem.stack;
            int totalStacked = 0;

            for (int i = 0; i < Slots.Length; i++)
            {
                Item slot = Slots[i];
                if (slot != null && !slot.IsAir && slot.type == newItem.type && slot.stack < slot.maxStack && Item.CanStack(slot, newItem))
                {
                    int canTake = Math.Min(newItem.stack, slot.maxStack - slot.stack);
                    slot.stack += canTake;
                    newItem.stack -= canTake;
                    totalStacked += canTake;

                    if (newItem.stack <= 0) break;
                }
            }

            if (totalStacked > 0)
            {
                EnsureTrailingEmptySlots(10);
                // 生成拾取飘字
                Vector2 pos = Main.LocalPlayer?.Center ?? Vector2.Zero;
                PopupText.NewText(PopupTextContext.RegularItemPickup, itemInfo, pos, totalStacked, false, false);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                NotifySlotsChanged();
            }

            if (newItem.stack <= 0)
            {
                newItem.TurnToAir();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 当本体背包满时，尝试将溢出未拾取的物品存入大背包（先堆叠后空格）
        /// </summary>
        /// <returns>若物品被完全存入大背包返回 true；若部分存入或未存入返回 false</returns>
        public static bool TryOverflowPickup(Item newItem)
        {
            if (newItem == null || newItem.IsAir || newItem.type <= 0 || newItem.stack <= 0) return false;
            if (!EnableBigBag.val || !PickupOverflowToBigBag.val) return false;

            Item itemInfo = newItem.Clone();
            int originalStack = newItem.stack;
            int totalPlaced = 0;

            // 1. 若可堆叠，优先寻找已有同类槽位合并
            if (newItem.maxStack > 1)
            {
                for (int i = 0; i < Slots.Length; i++)
                {
                    Item slot = Slots[i];
                    if (slot != null && !slot.IsAir && slot.type == newItem.type && slot.stack < slot.maxStack && Item.CanStack(slot, newItem))
                    {
                        int canTake = Math.Min(newItem.stack, slot.maxStack - slot.stack);
                        slot.stack += canTake;
                        newItem.stack -= canTake;
                        totalPlaced += canTake;

                        if (newItem.stack <= 0) break;
                    }
                }
            }

            // 2. 若仍有剩余，放入大背包空格子（动态扩容保底）
            if (newItem.stack > 0)
            {
                EnsureTrailingEmptySlots(10);

                for (int i = 0; i < Slots.Length; i++)
                {
                    Item slot = Slots[i];
                    if (slot == null || slot.IsAir)
                    {
                        Slots[i] = newItem.Clone();
                        totalPlaced += newItem.stack;
                        newItem.stack = 0;
                        newItem.TurnToAir();
                        break;
                    }
                }
            }

            EnsureTrailingEmptySlots(10);

            if (totalPlaced > 0)
            {
                // 生成拾取飘字
                Vector2 pos = Main.LocalPlayer?.Center ?? Vector2.Zero;
                PopupText.NewText(PopupTextContext.RegularItemPickup, itemInfo, pos, totalPlaced, false, false);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                NotifySlotsChanged();
            }

            return newItem.stack <= 0 || newItem.IsAir;
        }

        /// <summary>
        /// 检查巨大背包当前是否可接收指定物品（用于掉落物吸附判定 Player.ItemSpace）
        /// </summary>
        public static bool CanBigBagAccept(Item newItem)
        {
            if (newItem == null || newItem.IsAir || newItem.type <= 0) return false;
            if (!EnableBigBag.val) return false;

            // 动态大背包具备无限容量与保底 10 空位，只要开启满包溢出或自动堆叠即可无缝接收
            if (PickupOverflowToBigBag.val || AutoStackOnPickup.val)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试将玩家背包中的物品转移至大背包（Shift+左键快捷存入）
        /// </summary>
        /// <param name="inv">物品来源数组</param>
        /// <param name="slot">槽位索引</param>
        /// <param name="justCheck">仅校验是否有转移空间（不实际改变数据）</param>
        /// <returns>若发生转移或有空间可转移返回 true</returns>
        public static bool TryPlacingInBigBag(Item[] inv, int slot, bool justCheck)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return false;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return false;

            if (justCheck) return true;

            Item[] slots = Slots;
            bool transferred = false;

            // 1. 如果物品可堆叠，优先向已有同类槽位合并
            if (item.maxStack > 1)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    Item target = slots[i];
                    if (target != null && !target.IsAir && target.type == item.type && target.stack < target.maxStack && Item.CanStack(target, item))
                    {
                        int canTake = Math.Min(item.stack, target.maxStack - target.stack);
                        target.stack += canTake;
                        item.stack -= canTake;
                        transferred = true;
                        if (item.stack <= 0)
                        {
                            inv[slot] = new Item();
                            break;
                        }
                    }
                }
            }

            // 2. 如果还有剩余，放入空格子（动态扩容）
            if (item.stack > 0)
            {
                EnsureTrailingEmptySlots(10);
                slots = Slots;

                for (int i = 0; i < slots.Length; i++)
                {
                    Item target = slots[i];
                    if (target == null || target.IsAir)
                    {
                        slots[i] = item.Clone();
                        inv[slot] = new Item();
                        transferred = true;
                        break;
                    }
                }
            }

            EnsureTrailingEmptySlots(10);

            if (transferred)
            {
                NotifySlotsChanged();
            }

            return transferred;
        }

        /// <summary>
        /// 在大背包中查找指定物品类型的首个有效槽位索引，未找到返回 -1
        /// </summary>
        public static int FindItem(int type)
        {
            if (Slots == null || type <= 0) return -1;
            for (int i = 0; i < Slots.Length; i++)
            {
                Item it = Slots[i];
                if (it != null && !it.IsAir && it.type == type && it.stack > 0)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 将物品存入大背包（优先存入指定首选槽位，次选同类堆叠，再选空格，不足时自动动态扩容）
        /// </summary>
        public static bool DepositItem(Item item, int preferredSlot = -1)
        {
            if (item == null || item.IsAir || item.stack <= 0) return true;
            Item[] slots = Slots;
            if (slots == null) return false;

            int itemType = item.type;

            // 1. 若首选槽位为空，直接放入
            if (preferredSlot >= 0 && preferredSlot < slots.Length)
            {
                if (slots[preferredSlot] == null || slots[preferredSlot].IsAir)
                {
                    slots[preferredSlot] = item.Clone();
                    item.TurnToAir();
                    EnsureTrailingEmptySlots(10);
                    NotifySlotsChanged();
                    return true;
                }
            }

            // 2. 尝试向已有同类堆叠
            for (int i = 0; i < slots.Length; i++)
            {
                Item target = slots[i];
                if (target != null && !target.IsAir && target.type == item.type && target.stack < target.maxStack && Item.CanStack(target, item))
                {
                    int take = Math.Min(item.stack, target.maxStack - target.stack);
                    target.stack += take;
                    item.stack -= take;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                        EnsureTrailingEmptySlots(10);
                        NotifySlotsChanged();
                        return true;
                    }
                }
            }

            // 3. 放入空格
            for (int i = 0; i < slots.Length; i++)
            {
                Item target = slots[i];
                if (target == null || target.IsAir)
                {
                    slots[i] = item.Clone();
                    item.TurnToAir();
                    EnsureTrailingEmptySlots(10);
                    NotifySlotsChanged();
                    return true;
                }
            }

            // 4. 若无空格则自动扩容放入
            EnsureTrailingEmptySlots(10);
            slots = Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                Item target = slots[i];
                if (target == null || target.IsAir)
                {
                    slots[i] = item.Clone();
                    item.TurnToAir();
                    EnsureTrailingEmptySlots(10);
                    NotifySlotsChanged();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 直接替换存储数组（持久化加载时使用，自动扩容补齐末尾 10 空位）
        /// </summary>
        public static void SetItems(Item[] slots)
        {
            if (slots == null) return;

            Slots = slots;
            BagChest.item = Slots;
            BagChest.maxItems = Slots.Length;

            EnsureCapacitySafety(Slots.Length);
            EnsureTrailingEmptySlots(10);
            OnCapacityChanged?.Invoke();
        }

        /// <summary>
        /// 重置所有槽位为空物品
        /// </summary>
        public static void ResetSlots()
        {
            int count = Math.Max(40, Capacity.val);
            Slots = NewSlots(count);
            BagChest.item = Slots;
            BagChest.maxItems = Slots.Length;

            EnsureCapacitySafety(Slots.Length);
            EnsureTrailingEmptySlots(10);
            OnCapacityChanged?.Invoke();
        }

        private static Item[] NewSlots(int count)
        {
            Item[] slots = new Item[count];
            for (int i = 0; i < count; i++) slots[i] = new Item();
            return slots;
        }

        private static Chest CreateChest(Item[] slots)
        {
            Chest chest = Chest.CreateBank(-6);
            chest.item = slots;
            chest.maxItems = slots.Length;
            return chest;
        }
    }

    /// <summary>
    /// 巨大背包角色伴随存档持久化 (Sidecar Containers: "BigBag")
    /// 完全绑定角色生命周期，杜绝跨人物共享，支持原版与模组实体物品无损存读档
    /// </summary>
    public static class BigBagStorage
    {
        public const string ContainerKey = "BigBag";

        /// <summary>当前在内存中持有 BigBag.Slots 的玩家名称</summary>
        public static string ActivePlayerName { get; private set; }

        static BigBagStorage()
        {
            ModItemSidecarEngine.OnResetContainers += Reset;
            ModItemSidecarEngine.OnLoadContainers += LoadForPlayer;
        }

        /// <summary>
        /// 立即将当前活动玩家的大背包数据保存落盘至 Sidecar 伴随文件
        /// </summary>
        public static void SaveNow()
        {
            Player player = Main.LocalPlayer;
            if (player == null || string.IsNullOrEmpty(ActivePlayerName) || player.name != ActivePlayerName) return;
            ModItemSidecarEngine.SavePlayerContainer(player, ContainerKey, BigBag.Slots);
        }

        /// <summary>
        /// 为指定玩家加载其专属的大背包数据（自动补齐末尾 10 空位，绝不截断丢物）
        /// </summary>
        public static void LoadForPlayer(Player player)
        {
            if (player == null)
            {
                Reset();
                return;
            }

            ActivePlayerName = player.name;
            int cap = BigBag.Capacity.val;
            Item[] slots = ModItemSidecarEngine.LoadPlayerContainer(player, ContainerKey, cap);
            BigBag.SetItems(slots);
            BigBag.EnsureTrailingEmptySlots(10);
        }

        /// <summary>
        /// 重置大背包内存状态为空白状态
        /// </summary>
        public static void Reset()
        {
            ActivePlayerName = null;
            BigBag.ResetSlots();
        }
    }

    /// <summary>
    /// 巨大背包角色生命周期监听器：
    /// 角色保存、加载、切换时自动存取属于该角色的独立大背包数据
    /// </summary>
    public class BigBagPlayer : TPML.Content.ModPlayer
    {
        public override void SavePlayerPrefix(Terraria.IO.PlayerFileData playerFile, bool skipMapSave)
        {
            if (playerFile?.Player != null)
            {
                // 严格校验：只有被保存的角色正是当前内存加载激活的角色时，才允许写入内存槽位数据，杜绝新建角色被污染
                if (!string.IsNullOrEmpty(BigBagStorage.ActivePlayerName) && playerFile.Player.name == BigBagStorage.ActivePlayerName)
                {
                    ModItemSidecarEngine.SavePlayerContainer(playerFile.Player, BigBagStorage.ContainerKey, BigBag.Slots);
                }
            }
        }

        public override void LoadPlayerPostfix(Terraria.IO.PlayerFileData playerFile)
        {
            if (playerFile?.Player != null)
            {
                BigBagStorage.LoadForPlayer(playerFile.Player);
            }
        }

        public override void SetAsActivePostfix(Terraria.IO.PlayerFileData playerFile)
        {
            if (playerFile?.Player != null)
            {
                BigBagStorage.LoadForPlayer(playerFile.Player);
            }
        }
    }
}
