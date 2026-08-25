using HarmonyLib;
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
    /// 容器交互补丁：
    /// 1. 物品栏右键点击药水袋/旗帜盒开闭对应实体界面
    /// 2. 鼠标悬停中键快捷开闭对应实体界面
    /// 3. 手持药水/旗帜左键点击收纳袋直接存入该实体
    /// 4. 容器打开时 Shift+左键 快速存入当前打开的容器实体
    /// 5. 拾取物品时若背包有对应开启自动收纳的袋子/盒子实体则自动吸入
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_ItemContainerInteractions
    {
        public static int PotionBagType => ModContent.ItemType<PotionBagItem>();
        public static int BannerChestType => ModContent.ItemType<BannerChestItem>();

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.RightClick), typeof(Item[]), typeof(int), typeof(int))]
        [HarmonyPrefix]
        public static bool RightClickPrefix(Item[] inv, int context, int slot)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return true;
            Item item = inv[slot];
            if (item == null || item.IsAir) return true;

            int pbType = PotionBagType;
            int bcType = BannerChestType;

            // 不是我们的容器物品，正常放行原版
            if ((pbType <= 0 || item.type != pbType) && (bcType <= 0 || item.type != bcType))
            {
                return true;
            }

            // 当玩家按下鼠标右键时
            if (Main.mouseRight)
            {
                // 仅在单次点击的上升沿帧执行界面开关
                if (Main.mouseRightRelease && Main.player[Main.myPlayer].itemAnimation <= 0)
                {
                    Main.mouseRightRelease = false; // 消费本次点击，防止连续触发

                    IItemContainer container = ItemLoader.GetModItem(item) as IItemContainer;
                    if (container != null)
                    {
                        ItemContainerWindow.Toggle(container);
                        SoundEngine.PlaySound(SoundID.MenuOpen);
                    }
                }

                // 核心保障：只要是容器物品且正处于右键按下状态，全程返回 false，彻底阻断原版抓取
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.LeftClick), typeof(Item[]), typeof(int), typeof(int))]
        [HarmonyPrefix]
        public static bool LeftClickPrefix(Item[] inv, int context, int slot)
        {
            if (inv == null || slot < 0 || slot >= inv.Length) return true;
            Item item = inv[slot];
            if (item == null) return true;

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
                            return false;
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
                        return false;
                    }
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.GetAlternateClickAction))]
        [HarmonyPostfix]
        public static void GetAlternateClickActionPostfix(Item[] inv, int context, int slot, ref ItemSlot.AlternateClickAction? __result)
        {
            if (Main.player[Main.myPlayer].chest != -1) return;
            if (context != ItemSlot.Context.InventoryItem && context != ItemSlot.Context.InventoryCoin && context != ItemSlot.Context.InventoryAmmo) return;
            if (inv == null || slot < 0 || slot >= inv.Length) return;
            Item item = inv[slot];
            if (item == null || item.IsAir || item.favorited) return;

            if (ItemSlot.ShiftInUse)
            {
                if (ItemContainerWindow.IsOpen && ItemContainerWindow.Instance.Container != null && ItemContainerWindow.Instance.Container.TryDepositFromSlot(inv, slot, justCheck: true))
                {
                    __result = ItemSlot.AlternateClickAction.TransferToChest;
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.MouseHover), typeof(Item[]), typeof(int), typeof(int))]
        [HarmonyPostfix]
        public static void MouseHoverPostfix(Item[] inv, int context, int slot)
        {
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

        [HarmonyPatch(typeof(Player), nameof(Player.GetItem), typeof(Item), typeof(GetItemSettings))]
        [HarmonyPrefix]
        public static bool GetItemPrefix(Player __instance, Item newItem, GetItemSettings settings, ref Item __result)
        {
            if (__instance == null || __instance.whoAmI != Main.myPlayer) return true;
            if (newItem == null || newItem.IsAir || newItem.type <= 0) return true;

            // 当正在从收纳袋执行一键取出或单个提取时，放行原版正常进入背包，严禁重新吸入
            if (ItemContainerItem.IsTransferringOut) return true;

            int pbType = PotionBagType;
            int bcType = BannerChestType;
            if (pbType <= 0 && bcType <= 0) return true;

            // 扫描背包与银行中所有开启自动收纳的容器实体
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
                            int orig = newItem.stack;
                            container.TryDeposit(newItem, sort: true);
                            int absorbed = orig - newItem.stack;
                            if (absorbed > 0)
                            {
                                PopupText.NewText(PopupTextContext.RegularItemPickup, newItem, __instance.Center, absorbed, false, false);
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

            if (TryAutoStoreInArray(__instance.inventory) ||
                (__instance.bank?.item != null && TryAutoStoreInArray(__instance.bank.item)) ||
                (__instance.bank2?.item != null && TryAutoStoreInArray(__instance.bank2.item)) ||
                (__instance.bank3?.item != null && TryAutoStoreInArray(__instance.bank3.item)) ||
                (__instance.bank4?.item != null && TryAutoStoreInArray(__instance.bank4.item)))
            {
                __result = new Item();
                return false;
            }

            return true;
        }
    }
}
