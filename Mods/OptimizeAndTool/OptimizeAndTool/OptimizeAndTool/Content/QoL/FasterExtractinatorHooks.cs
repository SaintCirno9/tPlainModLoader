using CommandHelp;
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
    /// 提炼机加速门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：极大地加快使用提炼机的速度。
    /// 原理：PlaceThing_ItemInExtractinator 通过 ApplyItemTime(item, num) 设定使用间隔
    /// （Player.cs:42000/42011，num=1，叶绿=0.33），时长由 item.useTime 决定。
    /// Prefix 临时将 useTime 缩小 10 倍，Postfix 恢复，使用间隔随之缩短。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class FasterExtractinatorHooks
    {
        /// <summary>提炼机使用间隔缩小倍数</summary>
        public const int SpeedDivisor = 10;

        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.PlaceThing_ItemInExtractinator += Hook_PlaceThing_ItemInExtractinator;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.PlaceThing_ItemInExtractinator -= Hook_PlaceThing_ItemInExtractinator;
            _registered = false;
        }

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

        private static void Hook_PlaceThing_ItemInExtractinator(On_Player.orig_PlaceThing_ItemInExtractinator orig, Player self, ref Player.ItemCheckContext context)
        {
            Item modifiedItem = null;
            int originalUseTime = 0;

            if (Enable.val)
            {
                Item item = self.inventory[self.selectedItem];
                if (item != null && item.type > 0 && ItemID.Sets.ExtractinatorMode[item.type] >= 0)
                {
                    modifiedItem = item;
                    originalUseTime = item.useTime;
                    item.useTime = Math.Max(1, originalUseTime / SpeedDivisor);
                }
            }

            try
            {
                orig(self, ref context);
            }
            finally
            {
                if (modifiedItem != null)
                {
                    modifiedItem.useTime = originalUseTime;
                }
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class FasterExtractinator
    {
        public const int SpeedDivisor = FasterExtractinatorHooks.SpeedDivisor;
        public static GetSetReset<bool> Enable => FasterExtractinatorHooks.Enable;

        public static List<CommandObject> GetCO() => FasterExtractinatorHooks.GetCO();
        public static List<UIElement> GetUI() => FasterExtractinatorHooks.GetUI();
    }
}
