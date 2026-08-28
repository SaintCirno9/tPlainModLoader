using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 提炼机加速（对齐 ImproveGame 语义）：极大地加快使用提炼机的速度。
    /// 原理：PlaceThing_ItemInExtractinator 通过 ApplyItemTime(item, num) 设定使用间隔
    /// （Player.cs:42000/42011，num=1，叶绿=0.33），时长由 item.useTime 决定。
    /// Prefix 临时将 useTime 缩小 10 倍，Postfix 恢复，使用间隔随之缩短。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class FasterExtractinator
    {
        /// <summary>提炼机使用间隔缩小倍数</summary>
        public const int SpeedDivisor = 10;

        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("fasterExtractinator", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "极大地加快提炼机（含叶绿提炼机）处理物品的速度", "Images/Item_5296", "加速提炼机")
            };
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlaceThing_ItemInExtractinator))]
    internal static class Patch_FasterExtractinator
    {
        private static Item modifiedItem = null;
        private static int originalUseTime = 0;

        [HarmonyPrefix]
        internal static void Prefix(Player __instance)
        {
            if (!FasterExtractinator.Enable.val) return;
            Item item = __instance.inventory[__instance.selectedItem];
            if (item == null || item.type <= 0) return;
            if (ItemID.Sets.ExtractinatorMode[item.type] < 0) return;
            modifiedItem = item;
            originalUseTime = item.useTime;
            item.useTime = Math.Max(1, originalUseTime / FasterExtractinator.SpeedDivisor);
        }

        [HarmonyFinalizer]
        internal static void Finalizer()
        {
            if (modifiedItem != null)
            {
                modifiedItem.useTime = originalUseTime;
                modifiedItem = null;
            }
        }
    }
}
