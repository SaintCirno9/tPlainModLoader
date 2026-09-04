using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Content.Storage.ItemContainer;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using TPML;
using TPML.Content;
using TPML.Content.Fusion;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 全局随身实体容器统一交互门控（基于 HookGen 强类型 On_ 门控，零反射）：<br/>
    /// 统一管理随身饰品袋、随身垃圾桶、药水袋、旗帜盒等全部实体容器物品的交互：<br/>
    /// 1. 物品栏右键/中键快捷打开对应实体界面；<br/>
    /// 2. 手持匹配物品左键点击容器直接存入；<br/>
    /// 3. 容器窗口打开时 Shift+左键 背包物品快速存入当前打开的容器；<br/>
    /// 4. 彻底阻止玩家将非空的随身容器误丢弃或丢入垃圾桶；<br/>
    /// 5. 拾取物品时统一派发至各容器的自动过滤与售卖/销毁拦截逻辑；<br/>
    /// 6. 统一绑定随身实体容器背包融合源生命周期。
    /// 作者: SaintCirno9
    /// </summary>
    public static class CarriedBagInteractionHooks
    {
        public static ModKeybind ToggleAccessoryBagKey { get; private set; }
        private static bool _registered = false;
        private static readonly ItemContainerFusionSource FusionSource = new ItemContainerFusionSource();

        static CarriedBagInteractionHooks()
        {
            ToggleAccessoryBagKey = KeybindLoader.RegisterKeybind("OptimizeAndTool", "ToggleAccessoryBag", "P", "打开/关闭随身饰品袋 (AccessoryBag)");
        }

        public static void RegisterAll()
        {
            if (_registered) return;

            On_ItemSlot.RightClick += Hook_RightClick;
            On_ItemSlot.LeftClick += Hook_LeftClick;
            On_ItemSlot.GetAlternateClickAction += Hook_GetAlternateClickAction;
            On_ItemSlot.MouseHover_ItemArray_int_int += Hook_MouseHover;
            On_Player.GetItem += Hook_GetItem;
            On_Player.DropSelectedItem += Hook_DropSelectedItem;

            InventoryFusionManager.RegisterSource(FusionSource);
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;

            On_ItemSlot.RightClick -= Hook_RightClick;
            On_ItemSlot.LeftClick -= Hook_LeftClick;
            On_ItemSlot.GetAlternateClickAction -= Hook_GetAlternateClickAction;
            On_ItemSlot.MouseHover_ItemArray_int_int -= Hook_MouseHover;
            On_Player.GetItem -= Hook_GetItem;
            On_Player.DropSelectedItem -= Hook_DropSelectedItem;

            InventoryFusionManager.UnregisterSource(FusionSource.Id);
            _registered = false;
        }

        public static void UpdateKeybinds()
        {
            if (Main.gameMenu || Main.drawingPlayerChat) return;

            if (ToggleAccessoryBagKey?.JustPressed == true)
            {
                AccessoryBagItem bag = CarriedBagCacheManager.GetFirstCarriedBag<AccessoryBagItem>();
                if (bag != null)
                {
                    AccessoryBagWindow.Toggle(bag);
                }
                else if (AccessoryBagWindow.IsOpen)
                {
                    AccessoryBagWindow.Instance.Close();
                }
            }
        }

        private static void ToggleBag(IBagInventory bag)
        {
            if (bag == null) return;

            if (bag is AccessoryBagItem accBag)
            {
                AccessoryBagWindow.Toggle(accBag);
            }
            else if (bag is IItemContainer container)
            {
                ItemContainerWindow.Toggle(container);
            }
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        private static IBagInventory GetCurrentOpenBag()
        {
            if (AccessoryBagWindow.IsOpen && AccessoryBagWindow.Instance.CurrentBag != null)
            {
                return AccessoryBagWindow.Instance.CurrentBag;
            }

            if (ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container != null)
            {
                return ItemContainerWindow.Instance.Container;
            }

            return null;
        }

        private static void Hook_RightClick(On_ItemSlot.orig_RightClick orig, Item[] inv, int context, int slot)
        {
            if (inv != null && slot >= 0 && slot < inv.Length)
            {
                Item item = inv[slot];
                if (item != null && !item.IsAir && ItemLoader.GetModItem(item) is IBagInventory bag)
                {
                    if (Main.mouseRight)
                    {
                        if (Main.mouseRightRelease && Main.player[Main.myPlayer].itemAnimation <= 0)
                        {
                            Main.mouseRightRelease = false;
                            ToggleBag(bag);
                        }
                        return;
                    }
                }
            }

            orig(inv, context, slot);
        }

        private static void Hook_LeftClick(On_ItemSlot.orig_LeftClick orig, Item[] inv, int context, int slot)
        {
            if (inv != null && slot >= 0 && slot < inv.Length)
            {
                Item item = inv[slot];
                if (item != null)
                {
                    // 1. 手持物品左键点击随身容器：存入对应容器实体（杜绝触发原版 MouseItemSwap 交换位置）
                    if (!Main.mouseItem.IsAir && Main.mouseLeft && Main.mouseLeftRelease && !ItemSlot.ShiftInUse && !ItemSlot.ControlInUse)
                    {
                        if (ItemLoader.GetModItem(item) is IBagInventory bag && bag.MeetEntryCriteria(Main.mouseItem))
                        {
                            if (bag.TryDeposit(Main.mouseItem, sort: true))
                            {
                                SoundEngine.PlaySound(SoundID.Grab);
                                if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
                                Main.mouseLeftRelease = false;
                                Recipe.UpdateRecipeList();
                                return;
                            }
                        }
                    }

                    // 2. 当任意容器窗口打开时，Shift+左键 背包物品快速存入当前打开的容器实体
                    if (Main.mouseLeft && Main.mouseLeftRelease && ItemSlot.ShiftInUse && Main.mouseItem.IsAir)
                    {
                        IBagInventory openBag = GetCurrentOpenBag();
                        if (openBag != null && openBag.MeetEntryCriteria(item))
                        {
                            if (openBag.TryDepositFromSlot(inv, slot, justCheck: false))
                            {
                                SoundEngine.PlaySound(SoundID.Grab);
                                Main.mouseLeftRelease = false;
                                Recipe.UpdateRecipeList();
                                return;
                            }
                        }
                    }
                }
            }

            orig(inv, context, slot);
        }

        private static ItemSlot.AlternateClickAction? Hook_GetAlternateClickAction(On_ItemSlot.orig_GetAlternateClickAction orig, Item[] inv, int context, int slot)
        {
            ItemSlot.AlternateClickAction? result = orig(inv, context, slot);

            if (Main.player[Main.myPlayer].chest != -1) return result;
            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return result;
            if (inv == null || slot < 0 || slot >= inv.Length) return result;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return result;

            if (ItemSlot.ShiftInUse)
            {
                IBagInventory openBag = GetCurrentOpenBag();
                if (openBag != null && openBag.MeetEntryCriteria(item) && openBag.TryDepositFromSlot(inv, slot, justCheck: true))
                {
                    return ItemSlot.AlternateClickAction.TransferToChest;
                }
            }

            return result;
        }

        private static void Hook_MouseHover(On_ItemSlot.orig_MouseHover_ItemArray_int_int orig, Item[] inv, int context, int slot)
        {
            orig(inv, context, slot);

            if (inv == null || slot < 0 || slot >= inv.Length) return;
            Item item = inv[slot];
            if (item == null || item.IsAir) return;

            if (Terraria.GameInput.PlayerInput.MouseInfo.MiddleButton == ButtonState.Pressed &&
                Terraria.GameInput.PlayerInput.MouseInfoOld.MiddleButton == ButtonState.Released)
            {
                if (ItemLoader.GetModItem(item) is IBagInventory bag)
                {
                    Terraria.GameInput.PlayerInput.MouseInfoOld = Terraria.GameInput.PlayerInput.MouseInfo;
                    ToggleBag(bag);
                }
            }
        }

        private static void Hook_DropSelectedItem(On_Player.orig_DropSelectedItem orig, Player self)
        {
            if (self.inventory != null && self.selectedItem >= 0 && self.selectedItem < self.inventory.Length)
            {
                Item held = self.inventory[self.selectedItem];
                if (held != null && !held.IsAir && ItemLoader.GetModItem(held) is IBagInventory bag)
                {
                    bool hasItems = false;
                    if (bag.Slots != null)
                    {
                        for (int i = 0; i < bag.Slots.Length; i++)
                        {
                            if (bag.Slots[i] != null && !bag.Slots[i].IsAir && bag.Slots[i].stack > 0)
                            {
                                hasItems = true;
                                break;
                            }
                        }
                    }

                    if (hasItems)
                    {
                        Main.NewText($"[提示] 无法丢弃非空的 [{held.Name}]，请先清空内部物品。", 255, 200, 100);
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        return;
                    }
                }
            }
            orig(self);
        }

        private static Item Hook_GetItem(On_Player.orig_GetItem orig, Player self, Item newItem, GetItemSettings settings)
        {
            if (self == null || self.whoAmI != Main.myPlayer ||
                newItem == null || newItem.IsAir || newItem.type <= 0 ||
                ItemContainerItem.IsTransferringOut ||
                settings.NoText)
            {
                return orig(self, newItem, settings);
            }

            var containers = CarriedBagCacheManager.GetAllItemContainers(self);
            for (int i = 0; i < containers.Count; i++)
            {
                var container = containers[i];
                if (container != null && container.OnPickupIntercept(self, newItem))
                {
                    return new Item();
                }
            }

            return orig(self, newItem, settings);
        }
    }
}
