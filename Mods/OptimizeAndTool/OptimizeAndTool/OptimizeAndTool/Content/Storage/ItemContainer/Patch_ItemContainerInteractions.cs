using HarmonyLib;
using OptimizeAndTool.Content.QoL;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.IO;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 容器交互补丁：
    /// 1. 物品栏右键点击药水袋/旗帜盒开闭对应界面
    /// 2. 鼠标悬停中键快捷开闭
    /// 3. 手持药水/旗帜左键点击收纳袋直接存入
    /// 4. 容器打开时 Shift+左键 快速存入
    /// 5. 拾取物品时若背包有对应袋子且开启自动收纳则直接吸入
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

                    if (pbType > 0 && item.type == pbType)
                    {
                        PotionBagWindow.Toggle();
                        SoundEngine.PlaySound(SoundID.MenuOpen);
                    }
                    else if (bcType > 0 && item.type == bcType)
                    {
                        BannerChestWindow.Toggle();
                        SoundEngine.PlaySound(SoundID.MenuOpen);
                    }
                }

                // 核心保障：只要是容器物品且正处于右键按下状态，全程返回 false，
                // 彻底阻断原版后续帧进入 PickupItemIntoMouse 将盒子本体抓到鼠标手上！
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

            // 1. 手持物品左键点击收纳袋：存入对应容器（杜绝触发原版 MouseItemSwap 交换位置）
            if (!Main.mouseItem.IsAir && Main.mouseLeft && Main.mouseLeftRelease && !ItemSlot.ShiftInUse && !ItemSlot.ControlInUse)
            {
                if (pbType > 0 && item.type == pbType && PotionBagStorage.Instance.MeetEntryCriteria(Main.mouseItem))
                {
                    if (PotionBagStorage.Instance.TryDeposit(Main.mouseItem, sort: true))
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                        if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
                        Main.mouseLeftRelease = false;
                        Recipe.UpdateRecipeList();
                        return false;
                    }
                }

                if (bcType > 0 && item.type == bcType && BannerChestStorage.Instance.MeetEntryCriteria(Main.mouseItem))
                {
                    if (BannerChestStorage.Instance.TryDeposit(Main.mouseItem, sort: true))
                    {
                        SoundEngine.PlaySound(SoundID.Grab);
                        if (Main.mouseItem.stack <= 0) Main.mouseItem.TurnToAir();
                        Main.mouseLeftRelease = false;
                        Recipe.UpdateRecipeList();
                        return false;
                    }
                }
            }

            // 2. 当收纳窗口打开时，Shift+左键背包物品快速存入
            if (Main.mouseLeft && Main.mouseLeftRelease && ItemSlot.ShiftInUse && Main.mouseItem.IsAir)
            {
                if (PotionBagWindow.IsOpen && PotionBagStorage.Instance.TryDepositFromSlot(inv, slot, justCheck: false))
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                    Main.mouseLeftRelease = false;
                    Recipe.UpdateRecipeList();
                    return false;
                }

                if (BannerChestWindow.IsOpen && BannerChestStorage.Instance.TryDepositFromSlot(inv, slot, justCheck: false))
                {
                    SoundEngine.PlaySound(SoundID.Grab);
                    Main.mouseLeftRelease = false;
                    Recipe.UpdateRecipeList();
                    return false;
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
                if (PotionBagWindow.IsOpen && PotionBagStorage.Instance.TryDepositFromSlot(inv, slot, justCheck: true))
                {
                    __result = ItemSlot.AlternateClickAction.TransferToChest;
                }
                else if (BannerChestWindow.IsOpen && BannerChestStorage.Instance.TryDepositFromSlot(inv, slot, justCheck: true))
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
                if (pbType > 0 && item.type == pbType)
                {
                    Terraria.GameInput.PlayerInput.MouseInfoOld = Terraria.GameInput.PlayerInput.MouseInfo;
                    PotionBagWindow.Toggle();
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }

                int bcType = BannerChestType;
                if (bcType > 0 && item.type == bcType)
                {
                    Terraria.GameInput.PlayerInput.MouseInfoOld = Terraria.GameInput.PlayerInput.MouseInfo;
                    BannerChestWindow.Toggle();
                    SoundEngine.PlaySound(SoundID.MenuOpen);
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
            if (ItemContainerStorage.IsTransferringOut) return true;

            // 药水袋拾取自动收纳
            int pbType = PotionBagType;
            if (pbType > 0 && PotionBagStorage.Instance.AutoStorage && __instance.HasItem(pbType) && PotionBagStorage.Instance.MeetEntryCriteria(newItem))
            {
                int orig = newItem.stack;
                PotionBagStorage.Instance.TryDeposit(newItem, sort: true);
                int absorbed = orig - newItem.stack;
                if (absorbed > 0)
                {
                    PopupText.NewText(PopupTextContext.RegularItemPickup, newItem, __instance.Center, absorbed, false, false);
                }
                if (newItem.stack <= 0)
                {
                    __result = new Item();
                    return false;
                }
            }

            // 旗帜盒拾取自动收纳
            int bcType = BannerChestType;
            if (bcType > 0 && BannerChestStorage.Instance.AutoStorage && __instance.HasItem(bcType) && BannerChestStorage.Instance.MeetEntryCriteria(newItem))
            {
                int orig = newItem.stack;
                BannerChestStorage.Instance.TryDeposit(newItem, sort: true);
                int absorbed = orig - newItem.stack;
                if (absorbed > 0)
                {
                    PopupText.NewText(PopupTextContext.RegularItemPickup, newItem, __instance.Center, absorbed, false, false);
                }
                if (newItem.stack <= 0)
                {
                    __result = new Item();
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 角色切换与存档同步生命周期钩子
    /// </summary>
    public class ItemContainerPlayerHook : PatchPlayer
    {
        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                PotionBagStorage.Instance.EnsurePlayerLoaded();
                BannerChestStorage.Instance.EnsurePlayerLoaded();
            }
        }

        public override void SavePlayerPostfix(PlayerFileData playerFile, bool skipMapSave)
        {
            PotionBagStorage.Instance.SaveNow();
            BannerChestStorage.Instance.SaveNow();
        }
    }
}
