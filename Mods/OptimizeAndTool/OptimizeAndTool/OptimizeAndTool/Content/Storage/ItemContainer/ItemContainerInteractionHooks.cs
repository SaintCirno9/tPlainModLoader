using OptimizeAndTool.Content.QoL;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 容器交互门控（基于 HookGen 强类型 On_ 门控）：
    /// 1. 物品栏右键点击药水袋/旗帜盒开闭对应实体界面
    /// 2. 鼠标悬停中键快捷开闭对应实体界面
    /// 3. 手持药水/旗帜左键点击收纳袋直接存入该实体
    /// 4. 容器打开时 Shift+左键 快速存入当前打开的容器实体
    /// 5. 拾取物品时若背包有对应开启自动收纳的袋子/盒子实体则自动吸入
    /// 作者: SaintCirno9
    /// </summary>
    internal static class ItemContainerInteractionHooks
    {
        public static int PotionBagType => ModContent.ItemType<PotionBagItem>();
        public static int BannerChestType => ModContent.ItemType<BannerChestItem>();
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_ItemSlot.RightClick += Hook_RightClick;
            On_ItemSlot.LeftClick += Hook_LeftClick;
            On_ItemSlot.GetAlternateClickAction += Hook_GetAlternateClickAction;
            On_ItemSlot.MouseHover_ItemArray_int_int += Hook_MouseHover;
            On_Player.GetItem += Hook_GetItem;
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
            _registered = false;
        }

        private static void Hook_RightClick(On_ItemSlot.orig_RightClick orig, Item[] inv, int context, int slot)
        {
            if (inv != null && slot >= 0 && slot < inv.Length)
            {
                Item item = inv[slot];
                if (item != null && !item.IsAir)
                {
                    int pbType = PotionBagType;
                    int bcType = BannerChestType;

                    if ((pbType > 0 && item.type == pbType) || (bcType > 0 && item.type == bcType))
                    {
                        if (Main.mouseRight)
                        {
                            if (Main.mouseRightRelease && Main.player[Main.myPlayer].itemAnimation <= 0)
                            {
                                Main.mouseRightRelease = false;
                                IItemContainer container = ItemLoader.GetModItem(item) as IItemContainer;
                                if (container != null)
                                {
                                    ItemContainerWindow.Toggle(container);
                                    SoundEngine.PlaySound(SoundID.MenuOpen);
                                }
                            }
                            return;
                        }
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
                    int pbType = PotionBagType;
                    int bcType = BannerChestType;

                    // 1. 手持物品左键点击收纳袋：存入对应容器实体（杜绝触发原版 MouseItemSwap 交换位置）
                    if (!Main.mouseItem.IsAir && Main.mouseLeft && Main.mouseLeftRelease && !ItemSlot.ShiftInUse && !ItemSlot.ControlInUse)
                    {
                        if ((pbType > 0 && item.type == pbType) || (bcType > 0 && item.type == bcType))
                        {
                            IItemContainer container = ItemLoader.GetModItem(item) as IItemContainer;
                            if (container != null && container.MeetEntryCriteria(Main.mouseItem))
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
                int pbType = PotionBagType;
                int bcType = BannerChestType;
                if ((pbType > 0 && item.type == pbType) || (bcType > 0 && item.type == bcType))
                {
                    Terraria.GameInput.PlayerInput.MouseInfoOld = Terraria.GameInput.PlayerInput.MouseInfo;
                    IItemContainer container = ItemLoader.GetModItem(item) as IItemContainer;
                    if (container != null)
                    {
                        ItemContainerWindow.Toggle(container);
                        SoundEngine.PlaySound(SoundID.MenuOpen);
                    }
                }
            }
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

            int pbType = PotionBagType;
            int bcType = BannerChestType;
            if (pbType <= 0 && bcType <= 0)
            {
                return orig(self, newItem, settings);
            }

            bool TryAutoStoreInArray(Item[] array)
            {
                if (array == null) return false;
                for (int i = 0; i < array.Length; i++)
                {
                    Item it = array[i];
                    if (it != null && !it.IsAir && (it.type == pbType || it.type == bcType))
                    {
                        IItemContainer container = ItemLoader.GetModItem(it) as IItemContainer;
                        if (container != null && container.AutoStorage && container.MeetEntryCriteria(newItem))
                        {
                            int origStack = newItem.stack;
                            container.TryDeposit(newItem, sort: true);
                            int absorbed = origStack - newItem.stack;
                            if (absorbed > 0)
                            {
                                PopupText.NewText(PopupTextContext.RegularItemPickup, newItem, self.Center, absorbed, false, false);
                            }
                            if (newItem.stack <= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            if (TryAutoStoreInArray(self.inventory) ||
                (self.bank?.item != null && TryAutoStoreInArray(self.bank.item)) ||
                (self.bank2?.item != null && TryAutoStoreInArray(self.bank2.item)) ||
                (self.bank3?.item != null && TryAutoStoreInArray(self.bank3.item)) ||
                (self.bank4?.item != null && TryAutoStoreInArray(self.bank4.item)))
            {
                return new Item();
            }

            return orig(self, newItem, settings);
        }
    }
}
