using Microsoft.Xna.Framework;
using OptimizeAndTool.Content.QoL;
using TPML;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 容器交互门控（基于 HookGen 强类型 On_ 门控）：
    /// 1. 物品栏右键点击任意 ItemContainerItem 打开/关闭对应实体界面
    /// 2. 鼠标悬停中键快捷开闭对应实体界面
    /// 3. 手持物品左键点击收纳袋直接存入该实体
    /// 4. 容器打开时 Shift+左键 快速存入当前打开的容器实体
    /// 5. 拾取物品时统一派发至各容器的 OnPickupIntercept 进行自动收纳或售卖销毁
    /// 6. 阻止玩家将非空的容器实体误丢弃
    /// 作者: SaintCirno9
    /// </summary>
    internal static class ItemContainerInteractionHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_ItemSlot.RightClick += Hook_RightClick;
            On_ItemSlot.LeftClick += Hook_LeftClick;
            On_ItemSlot.GetAlternateClickAction += Hook_GetAlternateClickAction;
            On_ItemSlot.MouseHover_ItemArray_int_int += Hook_MouseHover;
            On_Player.GetItem += Hook_GetItem;
            On_Player.DropSelectedItem += Hook_DropSelectedItem;
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
            _registered = false;
        }

        private static void Hook_RightClick(On_ItemSlot.orig_RightClick orig, Item[] inv, int context, int slot)
        {
            if (inv != null && slot >= 0 && slot < inv.Length)
            {
                Item item = inv[slot];
                if (item != null && !item.IsAir && ItemLoader.GetModItem(item) is ItemContainerItem container)
                {
                    if (Main.mouseRight)
                    {
                        if (Main.mouseRightRelease && Main.player[Main.myPlayer].itemAnimation <= 0)
                        {
                            Main.mouseRightRelease = false;
                            ItemContainerWindow.Toggle(container);
                            SoundEngine.PlaySound(SoundID.MenuOpen);
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
                    // 1. 手持物品左键点击收纳袋：存入对应容器实体（杜绝触发原版 MouseItemSwap 交换位置）
                    if (!Main.mouseItem.IsAir && Main.mouseLeft && Main.mouseLeftRelease && !ItemSlot.ShiftInUse && !ItemSlot.ControlInUse)
                    {
                        if (ItemLoader.GetModItem(item) is ItemContainerItem container && container.MeetEntryCriteria(Main.mouseItem))
                        {
                            if (container.TryDeposit(Main.mouseItem, sort: true))
                            {
                                SoundEngine.PlaySound(SoundID.Grab);
                                if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
                                Main.mouseLeftRelease = false;
                                Recipe.UpdateRecipeList();
                                return;
                            }
                        }
                    }

                    // 2. 当收纳窗口打开时，Shift+左键背包物品快速存入当前打开的容器实体
                    if (Main.mouseLeft && Main.mouseLeftRelease && ItemSlot.ShiftInUse && Main.mouseItem.IsAir)
                    {
                        if (ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container != null)
                        {
                            if (ItemContainerWindow.Instance.Container.TryDepositFromSlot(inv, slot, justCheck: false))
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
                if (ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container != null && ItemContainerWindow.Instance.Container.TryDepositFromSlot(inv, slot, justCheck: true))
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

            if (Terraria.GameInput.PlayerInput.MouseInfo.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                Terraria.GameInput.PlayerInput.MouseInfoOld.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
            {
                if (ItemLoader.GetModItem(item) is ItemContainerItem container)
                {
                    Terraria.GameInput.PlayerInput.MouseInfoOld = Terraria.GameInput.PlayerInput.MouseInfo;
                    ItemContainerWindow.Toggle(container);
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
            }
        }

        private static void Hook_DropSelectedItem(On_Player.orig_DropSelectedItem orig, Player self)
        {
            if (self.inventory != null && self.selectedItem >= 0 && self.selectedItem < self.inventory.Length)
            {
                Item held = self.inventory[self.selectedItem];
                if (held != null && !held.IsAir && ItemLoader.GetModItem(held) is ItemContainerItem container && container.GetStoredCount() > 0)
                {
                    Main.NewText($"[提示] 无法丢弃非空的 [{held.Name}]，请先清空内部存储物。", 255, 200, 100);
                    return;
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

            bool TryProcessContainers(Item[] array)
            {
                if (array == null) return false;
                for (int i = 0; i < array.Length; i++)
                {
                    Item it = array[i];
                    if (it != null && !it.IsAir && ItemLoader.GetModItem(it) is ItemContainerItem container)
                    {
                        if (container.OnPickupIntercept(self, newItem))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            if (TryProcessContainers(self.inventory) ||
                (self.bank?.item != null && TryProcessContainers(self.bank.item)) ||
                (self.bank2?.item != null && TryProcessContainers(self.bank2.item)) ||
                (self.bank3?.item != null && TryProcessContainers(self.bank3.item)) ||
                (self.bank4?.item != null && TryProcessContainers(self.bank4.item)))
            {
                return new Item();
            }

            return orig(self, newItem, settings);
        }
    }
}
