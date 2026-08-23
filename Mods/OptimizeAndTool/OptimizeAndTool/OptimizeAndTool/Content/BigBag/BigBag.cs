using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 巨大额外背包：随身大容量仓库
    /// 存储数组包装为随身 Chest（bankChest=true, index=-6），经 PortableContainer 纳入制作材料来源；
    /// 格子交互复用原版 ItemSlot（context=BankItem），材料消耗为纯本地扣除。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class BigBag
    {
        public static readonly GetSetReset<bool> EnableBigBag = new GetSetReset<bool>(true, true);
        public static readonly GetSetReset<bool> EnableBigBagCraft = new GetSetReset<bool>(true, true);
        public static readonly GetSetReset<bool> AutoStackOnPickup = new GetSetReset<bool>(true, true);
        public static readonly GetSetReset<string> HotKey = new GetSetReset<string>("X", "X");
        public static readonly GetSetReset<int> Capacity = new GetSetReset<int>(100, 100);

        /// <summary>存储槽数组</summary>
        public static Item[] Slots { get; private set; } = NewSlots(100);

        /// <summary>随身 Chest 包装（制作系统消耗扣除入口，SetCapacity 时原地更新字段引用不变）</summary>
        public static Chest BagChest { get; } = CreateChest(Slots);

        /// <summary>容量变化后通知窗口重建格子</summary>
        public static event Action OnCapacityChanged;

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("bigBag", EnableBigBag),
                CommandBuild.get1("bigBagCapacity", EnableBigBag, Capacity, new CommandInt()),
                CommandBuild.get1("bigBagHotKey", EnableBigBag, HotKey, new CommandString()),
                CommandBuild.get2("bigBagCraft", EnableBigBagCraft),
                CommandBuild.get2("bigBagAutoStack", AutoStackOnPickup)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableBigBag, "随身巨大额外背包，快捷键或悬浮工具栏按钮打开", "Images/Item_4131", "巨大背包"),
                UIBuild.get1(EnableBigBag, HotKey, s => s?.Trim().ToUpper() ?? "X", "打开巨大背包的快捷键（如 X、B、Z、O、F 等，留空禁用）", "Images/UI/Cursor_0", "背包快捷键"),
                UIBuild.get1(EnableBigBag, Capacity, int.Parse, "背包容量格数（40~500，缩容时溢出物品自动回收背包）", "Images/Item_87", "背包容量"),
                UIBuild.get2(EnableBigBagCraft, "巨大背包中的材料参与制作判定与消耗扣除", "Images/Item_346", "背包材料制作"),
                UIBuild.get2(AutoStackOnPickup, "拾取物品时若巨大背包已有同类物品则自动堆入", "Images/Item_5010", "拾取自动堆叠")
            };
        }

        static BigBag()
        {
            Capacity.OnValUpdate += v => SetCapacity(v);
            EnsureCapacitySafety();
        }

        /// <summary>
        /// 原版 ItemSlot/CoinSlot 底层静态数组扩容至 1000，防止大容量大背包发生越界崩溃
        /// </summary>
        public static void EnsureCapacitySafety()
        {
            try
            {
                if (ItemSlot.inventoryGlowTimeChest != null && ItemSlot.inventoryGlowTimeChest.Length < 1000)
                {
                    Array.Resize(ref ItemSlot.inventoryGlowTimeChest, 1000);
                }

                if (ItemSlot.inventoryGlowHueChest != null && ItemSlot.inventoryGlowHueChest.Length < 1000)
                {
                    Array.Resize(ref ItemSlot.inventoryGlowHueChest, 1000);
                }

                if (CoinSlot.ChestEntries != null && CoinSlot.ChestEntries.Length < 1000)
                {
                    Array.Resize(ref CoinSlot.ChestEntries, 1000);
                }
            }
            catch { }
        }

        /// <summary>
        /// 调整容量（40~500），扩容补空格，缩容溢出物回收背包（放不下掉落）
        /// </summary>
        public static void SetCapacity(int capacity)
        {
            capacity = Math.Max(40, Math.Min(500, capacity));
            if (capacity == Slots.Length) return;

            Item[] old = Slots;
            Slots = NewSlots(capacity);

            int keep = Math.Min(old.Length, capacity);
            for (int i = 0; i < keep; i++) Slots[i] = old[i];

            Player player = Main.LocalPlayer;
            if (player?.active == true)
            {
                for (int i = capacity; i < old.Length; i++)
                {
                    if (old[i] == null || old[i].IsAir) continue;
                    player.GetOrDropItem(old[i], GetItemSettings.RefundConsumedItem);
                }
            }

            BagChest.item = Slots;
            BagChest.maxItems = Slots.Length;

            OnCapacityChanged?.Invoke();
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

            // 2. 剩余物品放入大背包空格子（跳过快捷栏 0~9，仅遍历 10~49）
            for (int i = 10; i < 50; i++)
            {
                Item pItem = inv[i];
                if (pItem == null || pItem.IsAir || pItem.favorited) continue;

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

            if (movedAny)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                BigBagStorage.SaveNow();
                OnCapacityChanged?.Invoke();
            }
        }

        /// <summary>
        /// 一键将大背包物品取出到玩家个人背包
        /// </summary>
        public static void LootAllToPlayer(Player player)
        {
            if (player == null || player.inventory == null) return;

            bool movedAny = false;
            for (int i = 0; i < Slots.Length; i++)
            {
                Item bItem = Slots[i];
                if (bItem == null || bItem.IsAir) continue;

                int origStack = bItem.stack;
                Slots[i] = player.GetItem(bItem, GetItemSettings.QuickTransferFromSlot);

                if (Slots[i] == null) Slots[i] = new Item();
                if (Slots[i].stack != origStack)
                {
                    movedAny = true;
                }
            }

            if (movedAny)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                BigBagStorage.SaveNow();
                OnCapacityChanged?.Invoke();
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

            if (movedAny)
            {
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
                BigBagStorage.SaveNow();
                OnCapacityChanged?.Invoke();
            }
        }

        /// <summary>
        /// 整理大背包：合并同类未满堆叠并按类别/ID 排序紧凑排列
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

            // 2. 收集非空物品并按类型及 ID 排序
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
                int rankX = GetItemSortRank(x);
                int rankY = GetItemSortRank(y);
                if (rankX != rankY) return rankX.CompareTo(rankY);
                if (x.type != y.type) return x.type.CompareTo(y.type);
                if (x.prefix != y.prefix) return x.prefix.CompareTo(y.prefix);
                return y.stack.CompareTo(x.stack);
            });

            // 3. 写回 Slots 并补齐空格
            for (int i = 0; i < Slots.Length; i++)
            {
                if (i < items.Count) Slots[i] = items[i];
                else Slots[i] = new Item();
            }

            Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
            BigBagStorage.SaveNow();
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
            if (newItem == null || newItem.IsAir || newItem.type <= 0) return false;
            if (!EnableBigBag.val || !AutoStackOnPickup.val) return false;

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
                BigBagStorage.SaveNow();
                // 生成拾取飘字
                Vector2 pos = Main.LocalPlayer?.Center ?? Vector2.Zero;
                PopupText.NewText(PopupTextContext.RegularItemPickup, newItem, pos, totalStacked, false, false);
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Grab);
            }

            return newItem.stack <= 0;
        }

        /// <summary>
        /// 直接替换存储数组（持久化加载时使用）
        /// </summary>
        public static void SetItems(Item[] slots)
        {
            if (slots == null) return;

            Slots = slots;
            BagChest.item = Slots;
            BagChest.maxItems = Slots.Length;

            EnsureCapacitySafety();
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
    /// 巨大背包物品持久化（bigBag.json），容量真值存于 setting.json 的 BigBagCapacity
    /// </summary>
    internal class BigBagStorage : ModSetting
    {
        public class SlotData
        {
            public int type;
            public int prefix;
            public int stack;
        }

        public override bool HasUI => false;
        public override string FilePath => "bigBag.json";
        public override Type DataType => typeof(List<SlotData>);

        private static BigBagStorage instance = null;
        private List<SlotData> date = null;

        public override void Load(object v)
        {
            instance = this;

            date = v as List<SlotData>;
            if (date == null)
            {
                date = new List<SlotData>();
                NeedSave = true;
                Save();
            }

            // 先按 items 数量构建（后续 Capacity 配置加载触发 SetCapacity 校准）
            int n = Math.Max(BigBag.Capacity.val, Math.Min(500, date.Count));
            Item[] slots = new Item[n];
            for (int i = 0; i < n; i++) slots[i] = new Item();

            for (int i = 0; i < date.Count && i < n; i++)
            {
                SlotData d = date[i];
                if (d == null || d.type <= 0) continue;

                Item item = new Item();
                item.SetDefaults(d.type);
                if (d.prefix > 0) item.Prefix(d.prefix);
                if (d.stack > 1) item.stack = Math.Min(d.stack, item.maxStack);
                slots[i] = item;
            }

            BigBag.SetItems(slots);
        }

        public override object GetSaveData() => date;

        /// <summary>
        /// 立即落盘（窗口交互后/关闭时调用）
        /// </summary>
        public static void SaveNow()
        {
            if (instance == null) return;

            List<SlotData> list = new List<SlotData>(BigBag.Slots.Length);
            foreach (Item item in BigBag.Slots)
            {
                SlotData d = new SlotData();
                if (item != null && !item.IsAir && item.type > 0)
                {
                    d.type = item.type;
                    d.prefix = item.prefix;
                    d.stack = item.stack;
                }
                list.Add(d);
            }

            instance.date = list;
            instance.NeedSave = true;
            instance.Save();
        }
    }
}
