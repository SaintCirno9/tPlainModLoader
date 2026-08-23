using CommandHelp;
using HarmonyLib;
using BigBagMod = OptimizeAndTool.Content.BigBag.BigBag;
using OptimizeAndTool.Content.BigBag;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 随身容器增强补丁
    /// 存钱罐/保险箱/护卫熔炉/虚空袋中的材料参与制作系统（可用判定与消耗扣除）；
    /// 物品栏中键悬停容器物品直接打开对应容器界面。
    /// 原版私有成员（由 Publicizer 强类型直连）：Recipe._recipeChests、Recipe.CollectItems、Player.OpenChest。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class PortableContainer
    {
        public static GetSetReset<bool> EnableContainerCraft = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableMiddleClickOpen = new GetSetReset<bool>(true, true);

        /// <summary>
        /// 容器物品 -> chest 界面编号（-2 存钱罐 / -3 保险箱 / -4 护卫熔炉 / -5 虚空库）
        /// </summary>
        private static readonly Dictionary<int, int> openChestMap = new Dictionary<int, int>
        {
            { ItemID.PiggyBank, -2 },
            { ItemID.MoneyTrough, -2 },
            { ItemID.Safe, -3 },
            { ItemID.DefendersForge, -4 },
            { ItemID.ClosedVoidBag, -5 },
            { ItemID.VoidVault, -5 },
        };

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("portableContainerCraft", EnableContainerCraft),
                CommandBuild.get2("portableContainerMiddleClick", EnableMiddleClickOpen)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableContainerCraft, "存钱罐、保险箱、护卫熔炉、虚空袋中的材料自动参与制作判定与消耗扣除", "Images/Item_346", "随身容器材料制作"),
                UIBuild.get2(EnableMiddleClickOpen, "物品栏中鼠标悬停存钱罐、保险箱、护卫熔炉、虚空袋物品时按中键直接打开对应容器", "Images/Item_87", "中键快捷打开容器")
            };
        }

        /// <summary>
        /// 将随身容器与巨大背包加入制作材料来源列表并累加材料计数
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Recipe), "CollectItemsFromChests")]
        public static void CollectItemsFromChestsPostfix(Player player)
        {
            if (player == null) return;

            if (EnableContainerCraft.val)
            {
                AddBankToRecipe(player.bank);
                AddBankToRecipe(player.bank2);
                AddBankToRecipe(player.bank3);
                AddBankToRecipe(player.bank4);
            }

            // 巨大额外背包（随身 Chest 包装）的材料独立判定纳入
            if (BigBagMod.EnableBigBag.val && BigBagMod.EnableBigBagCraft.val)
            {
                AddBankToRecipe(BigBagMod.BagChest);
            }
        }

        private static void AddBankToRecipe(Chest bank)
        {
            if (bank?.item == null) return;

            // 原版虚空袋启用时已加入 bank4，重复加入会导致材料双计数
            if (Recipe._recipeChests != null && !Recipe._recipeChests.Contains(bank))
            {
                Recipe._recipeChests.Add(bank);
            }

            Recipe.CollectItems(bank.item, bank.maxItems);
        }

        /// <summary>
        /// 制作物品消耗材料后，即时同步保存巨大背包
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Main), "CraftItem_GrantItem")]
        public static void CraftItem_GrantItemPostfix()
        {
            if (BigBagMod.EnableBigBag.val && BigBagMod.EnableBigBagCraft.val)
            {
                BigBagStorage.SaveNow();
            }
        }

        /// <summary>
        /// 物品栏中键悬停容器物品直接打开对应容器界面
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Main), "DrawInventory")]
        public static void DrawInventoryPostfix()
        {
            if (!EnableMiddleClickOpen.val) return;
            if (!Main.playerInventory || !Main.mouseItem.IsAir) return;
            if (Main.npcShop != -1) return;
            if (Main.HoverItem.type <= 0) return;
            if (!openChestMap.TryGetValue(Main.HoverItem.type, out int chestId)) return;

            // 中键按下沿
            if (PlayerInput.MouseInfo.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                PlayerInput.MouseInfoOld.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
            {
                Main.LocalPlayer.OpenChest(0, 0, chestId);
            }
        }
    }
}
