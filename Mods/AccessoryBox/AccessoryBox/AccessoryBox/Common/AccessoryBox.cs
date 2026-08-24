using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.IO;
using Terraria.UI;

namespace AccessoryBox.Common
{
    /// <summary>
    /// 随身饰品收纳箱 & 属性挂载库：
    /// 兼具实体物品收纳（左键拿放换/Shift快捷存取/右键半取单放/一键存取/智能整理）与被动属性挂载双重功能
    /// 作者: SaintCirno9
    /// </summary>
    internal class AccessoryBox : PatchPlayer
    {
        public static bool EnableMod => Config.Instance?.GetEnableMod() ?? true;

        public static bool EnablePassive
        {
            get => Config.Instance?.GetEnablePassive() ?? true;
            set => Config.Instance?.SetEnablePassive(value);
        }

        public static int Capacity => Config.Instance?.GetCapacity() ?? 100;

        /// <summary>存储槽数组</summary>
        public static Item[] Slots { get; private set; } = NewSlots(100);

        /// <summary>容量或内容变动后通知窗口重建格子</summary>
        public static event Action OnCapacityChanged;

        static AccessoryBox()
        {
            AccessoryBoxKeybind.Initialize();
            EnsureCapacitySafety();
        }

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

            EnsureCapacitySafety();
            OnCapacityChanged?.Invoke();
            AccessoryBoxStorage.SaveNow();
        }

        public static void SetItems(Item[] slots)
        {
            if (slots == null) return;

            Slots = slots;
            EnsureCapacitySafety();
            OnCapacityChanged?.Invoke();
        }

        private static Item[] NewSlots(int count)
        {
            Item[] slots = new Item[count];
            for (int i = 0; i < count; i++) slots[i] = new Item();
            return slots;
        }

        public override void SavePlayerPostfix(PlayerFileData playerFile, bool skipMapSave)
        {
            AccessoryBoxStorage.SaveNow();
        }

        /// <summary>
        /// 饰品箱核心属性与被动挂载逻辑：当 EnablePassive 开启时，箱内所有有效装备/饰品对玩家生效
        /// </summary>
        public override void UpdateEquipsPostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ) return;
            if (!EnableMod || !EnablePassive) return;

            Item[] slots = Slots;
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                Item item = slots[i];
                if (item == null || item.IsAir || item.type <= ItemID.None) continue;

                // 1. 词条前缀收益 (如 护佑 +4防御、险恶 +4%伤害 等)
                if (item.accessory || item.prefix > 0)
                {
                    This.GrantPrefixBenefits(item);
                }

                // 2. 基础护甲与防具属性
                This.GrantArmorBenefits(item);

                // 3. 饰品功能被动效果全量生效
                This.ApplyEquipFunctional(3, item);

                // 4. 翅膀飞行逻辑
                if (item.wingSlot > 0)
                {
                    if (!This.hideVisibleAccessory[3] || (This.velocity.Y != 0f && This.mount.CanUseWings))
                    {
                        This.wings = item.wingSlot;
                    }
                    This.wingsLogic = item.wingSlot;
                }

                // 5. 时装与外观效果
                This.ApplyEquipVanity(13, item);
            }
        }

        /// <summary>
        /// 一键存入：将玩家个人背包 10~49 格非收藏物品存入饰品箱并自动堆叠
        /// </summary>
        public static void DepositAllFromPlayer(Player player)
        {
            if (player == null || player.inventory == null) return;

            bool movedAny = false;
            Item[] inv = player.inventory;

            // 1. 先尝试向饰品箱已有同类物品堆叠（跳过快捷栏 0~9）
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

            // 2. 剩余物品放入饰品箱空格子
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
                SoundEngine.PlaySound(SoundID.Grab);
                AccessoryBoxStorage.SaveNow();
                OnCapacityChanged?.Invoke();
            }
        }

        /// <summary>
        /// 快速堆叠：将玩家个人背包中与箱内已有的同类物品快速补齐堆叠
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
                SoundEngine.PlaySound(SoundID.Grab);
                AccessoryBoxStorage.SaveNow();
                OnCapacityChanged?.Invoke();
            }
        }

        /// <summary>
        /// 一键取出：将饰品箱内所有物品取出至玩家背包
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
                SoundEngine.PlaySound(SoundID.Grab);
                AccessoryBoxStorage.SaveNow();
                OnCapacityChanged?.Invoke();
            }
        }

        /// <summary>
        /// 整理饰品箱：合并同类未满堆叠并按饰品/装备优先智能排序
        /// </summary>
        public static void SortAccessoryBox()
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

            SoundEngine.PlaySound(SoundID.Grab);
            AccessoryBoxStorage.SaveNow();
            OnCapacityChanged?.Invoke();
        }

        private static int GetItemSortRank(Item item)
        {
            if (item.accessory) return 1; // 饰品最高优先级
            if (item.defense > 0 || item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1) return 2; // 防具
            if (item.damage > 0 && item.useStyle != 0) return 3; // 武器
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0) return 4; // 工具
            if (item.potion || item.healLife > 0 || item.healMana > 0 || item.buffType > 0) return 5; // 药水/消耗品
            if (item.ammo > 0) return 6; // 弹药
            if (item.createTile > -1 || item.createWall > -1) return 7; // 方块/家具
            if (item.material) return 8; // 材料
            return 9; // 其他杂项
        }

        /// <summary>
        /// 尝试将玩家背包中的物品转移至饰品箱（Shift+左键快捷存入）
        /// </summary>
        public static bool TryPlacingInAccessoryBox(Item[] inv, int slot, bool justCheck)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return false;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return false;

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
                        if (justCheck)
                        {
                            if (canTake > 0) return true;
                        }
                        else
                        {
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
            }

            // 2. 如果还有剩余，放入空格子
            if (item.stack > 0)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    Item target = slots[i];
                    if (target == null || target.IsAir)
                    {
                        if (justCheck) return true;

                        slots[i] = item.Clone();
                        inv[slot] = new Item();
                        transferred = true;
                        break;
                    }
                }
            }

            if (transferred && !justCheck)
            {
                AccessoryBoxStorage.SaveNow();
                OnCapacityChanged?.Invoke();
            }

            return transferred;
        }
    }
}
