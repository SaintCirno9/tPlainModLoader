using CommandHelp;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.IO;
using Terraria.UI;
using TPML.Content.IO;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品收纳箱 & 属性挂载库：
    /// 兼具实体物品收纳（左键拿放换/Shift快捷存取/右键半取单放/一键存取/智能整理）与被动属性挂载双重功能
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBox : PatchPlayer
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnablePassive = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> Capacity = new GetSetReset<int>(100, 100);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("accessoryBox", Enable),
                CommandBuild.get2("accessoryBoxPassive", EnablePassive),
                CommandBuild.get1("accessoryBoxCapacity", Enable, Capacity, new CommandInt())
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get1(Enable, Capacity, int.Parse, "开启独立 40~500 格随身饰品与物品收纳箱（输入框配置容量）", "Images/Item_3813", "随身饰品收纳箱"),
                UIBuild.get2(EnablePassive, "箱内所有饰品属性与被动词缀全量生效，无需穿在身上", "Images/Item_158", "饰品被动属性挂载")
            };
        }

        public static Item[] Slots { get; private set; } = NewSlots(100);
        public static event Action OnCapacityChanged;

        /// <summary>外部通知饰品箱数据发生变化并刷新 UI</summary>
        public static void NotifySlotsChanged() => OnCapacityChanged?.Invoke();

        static AccessoryBox()
        {
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

        /// <summary>
        /// 重置所有槽位为空物品
        /// </summary>
        public static void ResetSlots()
        {
            int count = Math.Max(40, Math.Min(500, Capacity.val));
            Slots = NewSlots(count);
            EnsureCapacitySafety();
            OnCapacityChanged?.Invoke();
        }

        private static Item[] NewSlots(int count)
        {
            Item[] slots = new Item[count];
            for (int i = 0; i < count; i++) slots[i] = new Item();
            return slots;
        }

        public override void SavePlayerPrefix(PlayerFileData playerFile, bool skipMapSave)
        {
            if (playerFile?.Player != null)
            {
                // 严格校验：只有被保存的角色正是当前内存加载激活的角色时，才允许写入内存槽位数据，杜绝新建角色被污染
                if (!string.IsNullOrEmpty(AccessoryBoxStorage.ActivePlayerName) && playerFile.Player.name == AccessoryBoxStorage.ActivePlayerName)
                {
                    ModItemSidecarEngine.SavePlayerContainer(playerFile.Player, AccessoryBoxStorage.ContainerKey, Slots);
                }
            }
        }

        public override void LoadPlayerPostfix(PlayerFileData playerFile)
        {
            if (playerFile?.Player != null)
            {
                AccessoryBoxStorage.LoadForPlayer(playerFile.Player);
            }
        }

        public override void SetAsActivePostfix(PlayerFileData playerFile)
        {
            if (playerFile?.Player != null)
            {
                AccessoryBoxStorage.LoadForPlayer(playerFile.Player);
            }
        }

        public override void UpdateEquipsPostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer || Main.dedServ) return;
            if (!Enable.val || !EnablePassive.val) return;

            Item[] slots = Slots;
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                Item item = slots[i];
                if (item == null || item.IsAir || item.type <= ItemID.None) continue;

                if (item.accessory || item.prefix > 0)
                {
                    This.GrantPrefixBenefits(item);
                }

                This.GrantArmorBenefits(item);
                This.ApplyEquipFunctional(3, item);

                if (item.wingSlot > 0)
                {
                    if (!This.hideVisibleAccessory[3] || (This.velocity.Y != 0f && This.mount.CanUseWings))
                    {
                        This.wings = item.wingSlot;
                    }
                    This.wingsLogic = item.wingSlot;
                }

                This.ApplyEquipVanity(13, item);
            }
        }

        public static void DepositAllFromPlayer(Player player)
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

        public static void SortAccessoryBox()
        {
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
            if (item.accessory) return 1;
            if (item.defense > 0 || item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1) return 2;
            if (item.damage > 0 && item.useStyle != 0) return 3;
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0) return 4;
            if (item.potion || item.healLife > 0 || item.healMana > 0 || item.buffType > 0) return 5;
            if (item.ammo > 0) return 6;
            if (item.createTile > -1 || item.createWall > -1) return 7;
            if (item.material) return 8;
            return 9;
        }

        public static bool TryPlacingInAccessoryBox(Item[] inv, int slot, bool justCheck)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return false;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return false;

            Item[] slots = Slots;
            bool transferred = false;

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
