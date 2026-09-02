using System;
using Microsoft.Xna.Framework.Input;
using TPML;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋游戏内交互门控（基于 HookGen 强类型 On_ 门控）：
    /// 1. 右键/中键开闭实体界面
    /// 2. 手持饰品点击饰品袋直接存入
    /// 3. 打开窗口时 Shift+左键 快速存入
    /// 4. 悬停快捷转移键 (默认 ]) 极速转移
    /// 5. 按键 P 直达激活
    /// 6. 禁止对非空饰品袋 Shift 丢弃到垃圾桶
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBagInteractionHooks
    {
        public static ModKeybind QuickHoverKey { get; private set; }
        public static ModKeybind ToggleBagKey { get; private set; }
        private static bool _registered = false;

        static AccessoryBagInteractionHooks()
        {
            QuickHoverKey = KeybindLoader.RegisterKeybind("OptimizeAndTool", "QuickHoverAccessoryBag", "OemCloseBrackets", "悬停饰品快捷转移 (AccessoryBag)");
            ToggleBagKey = KeybindLoader.RegisterKeybind("OptimizeAndTool", "ToggleAccessoryBag", "P", "打开/关闭随身饰品袋 (AccessoryBag)");
        }

        public static void RegisterAll()
        {
            if (_registered) return;
            On_ItemSlot.RightClick += Hook_RightClick;
            On_ItemSlot.LeftClick += Hook_LeftClick;
            On_ItemSlot.MouseHover_ItemArray_int_int += Hook_MouseHover;
            On_Player.DropSelectedItem += Hook_DropSelectedItem;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_ItemSlot.RightClick -= Hook_RightClick;
            On_ItemSlot.LeftClick -= Hook_LeftClick;
            On_ItemSlot.MouseHover_ItemArray_int_int -= Hook_MouseHover;
            On_Player.DropSelectedItem -= Hook_DropSelectedItem;
            _registered = false;
        }

        public static void UpdateKeybinds()
        {
            if (Main.gameMenu || Main.drawingPlayerChat) return;

            if (ToggleBagKey?.JustPressed == true)
            {
                AccessoryBagItem bag = AccessoryBagCacheManager.GetFirstCarriedBag();
                if (bag != null)
                {
                    AccessoryBagWindow.Toggle(bag);
                }
                else if (AccessoryBagWindow.IsOpen)
                {
                    AccessoryBagWindow.Instance.Close();
                }
            }

            if (QuickHoverKey?.JustPressed == true && AccessoryBagWindow.IsOpen && AccessoryBagWindow.Instance.CurrentBag != null)
            {
                HandleQuickHoverTransfer(AccessoryBagWindow.Instance.CurrentBag);
            }
        }

        private static void HandleQuickHoverTransfer(AccessoryBagItem bag)
        {
            Item hover = Main.HoverItem;
            if (hover == null || hover.IsAir || !AccessoryBagItem.IsValidBagItem(hover)) return;

            Player player = Main.LocalPlayer;
            if (player?.inventory == null || bag.personalInventory == null) return;

            // 1. 若光标悬停在饰品袋内部的格子上，提取回背包
            for (int i = 0; i < bag.personalInventory.Length; i++)
            {
                Item bIt = bag.personalInventory[i];
                if (bIt != null && !bIt.IsAir && bIt.type == hover.type && bIt.prefix == hover.prefix)
                {
                    Item rest = player.GetItem(bIt, GetItemSettings.QuickTransferFromSlot);
                    bag.personalInventory[i] = rest ?? new Item();
                    SoundEngine.PlaySound(SoundID.Grab);
                    bag.TriggerSlotsChanged();
                    return;
                }
            }

            // 2. 若光标悬停在背包格子上，存入饰品袋
            for (int i = 0; i < 50; i++)
            {
                Item pIt = player.inventory[i];
                if (pIt != null && !pIt.IsAir && pIt.type == hover.type && pIt.prefix == hover.prefix)
                {
                    if (bag.CheckDuplicates(pIt, -1)) return;

                    for (int j = 0; j < bag.personalInventory.Length; j++)
                    {
                        if (bag.personalInventory[j] == null || bag.personalInventory[j].IsAir)
                        {
                            bag.personalInventory[j] = pIt.Clone();
                            player.inventory[i] = new Item();
                            SoundEngine.PlaySound(SoundID.Grab);
                            bag.TriggerSlotsChanged();
                            return;
                        }
                    }
                }
            }
        }

        private static void Hook_RightClick(On_ItemSlot.orig_RightClick orig, Item[] inv, int context, int slot)
        {
            if (inv != null && slot >= 0 && slot < inv.Length)
            {
                Item item = inv[slot];
                if (item != null && !item.IsAir && ItemLoader.GetModItem(item) is AccessoryBagItem bag)
                {
                    if (Main.mouseRight)
                    {
                        if (Main.mouseRightRelease && Main.player[Main.myPlayer].itemAnimation <= 0)
                        {
                            Main.mouseRightRelease = false;
                            AccessoryBagWindow.Toggle(bag);
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
                    AccessoryBagItem bag = ItemLoader.GetModItem(item) as AccessoryBagItem;

                    // 1. 手持物品左键点击饰品袋直接存入
                    if (bag != null && !Main.mouseItem.IsAir && AccessoryBagItem.IsValidBagItem(Main.mouseItem) && Main.mouseLeft && Main.mouseLeftRelease && !ItemSlot.ShiftInUse && !ItemSlot.ControlInUse)
                    {
                        if (!bag.CheckDuplicates(Main.mouseItem, -1))
                        {
                            for (int i = 0; i < bag.personalInventory.Length; i++)
                            {
                                if (bag.personalInventory[i] == null || bag.personalInventory[i].IsAir)
                                {
                                    bag.personalInventory[i] = Main.mouseItem.Clone();
                                    Main.mouseItem.TurnToAir();
                                    SoundEngine.PlaySound(SoundID.Grab);
                                    Main.mouseLeftRelease = false;
                                    bag.TriggerSlotsChanged();
                                    return;
                                }
                            }
                        }
                        return;
                    }

                    // 2. 窗口打开时 Shift+左键 快速存入
                    if (Main.mouseLeft && Main.mouseLeftRelease && ItemSlot.ShiftInUse && Main.mouseItem.IsAir)
                    {
                        if (AccessoryBagWindow.IsOpen && AccessoryBagWindow.Instance.CurrentBag != null && AccessoryBagItem.IsValidBagItem(item))
                        {
                            AccessoryBagItem currentBag = AccessoryBagWindow.Instance.CurrentBag;
                            if (!currentBag.CheckDuplicates(item, -1))
                            {
                                for (int i = 0; i < currentBag.personalInventory.Length; i++)
                                {
                                    if (currentBag.personalInventory[i] == null || currentBag.personalInventory[i].IsAir)
                                    {
                                        currentBag.personalInventory[i] = item.Clone();
                                        inv[slot] = new Item();
                                        SoundEngine.PlaySound(SoundID.Grab);
                                        Main.mouseLeftRelease = false;
                                        currentBag.TriggerSlotsChanged();
                                        return;
                                    }
                                }
                            }
                            return;
                        }
                    }
                }
            }

            orig(inv, context, slot);
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
                AccessoryBagItem bag = ItemLoader.GetModItem(item) as AccessoryBagItem;
                if (bag != null)
                {
                    Terraria.GameInput.PlayerInput.MouseInfoOld = Terraria.GameInput.PlayerInput.MouseInfo;
                    AccessoryBagWindow.Toggle(bag);
                }
            }
        }

        private static void Hook_DropSelectedItem(On_Player.orig_DropSelectedItem orig, Player self)
        {
            if (self.whoAmI == Main.myPlayer)
            {
                Item item = self.inventory[self.selectedItem];
                if (item != null && !item.IsAir && ItemLoader.GetModItem(item) is AccessoryBagItem bag && !bag.IsEmpty() && Main.cursorOverride == 6)
                {
                    Main.NewText("[饰品袋] 饰品袋内有饰品，禁止快捷丢入垃圾桶！", Microsoft.Xna.Framework.Color.OrangeRed);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
            }

            orig(self);
        }
    }
}
