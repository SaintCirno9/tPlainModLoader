using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.HeldItemModify
{
    /// <summary>
    /// 修复智能选择（Smart Select / 按 Shift）推断距离无法匹配自定义物品交互距离的问题（基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    internal static class SmartSelectRangeHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.SmartSelect_GetAvailableToolRanges += Hook_SmartSelect_GetAvailableToolRanges;
            On_Player.SmartSelect_GetToolStrategy += Hook_SmartSelect_GetToolStrategy;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.SmartSelect_GetAvailableToolRanges -= Hook_SmartSelect_GetAvailableToolRanges;
            On_Player.SmartSelect_GetToolStrategy -= Hook_SmartSelect_GetToolStrategy;
            _registered = false;
        }

        /// <summary>
        /// 当开启物品使用距离（tileBoost）增强时，将加成同步给背包中所有工具的推断范围
        /// </summary>
        private static void Hook_SmartSelect_GetAvailableToolRanges(
            On_Player.orig_SmartSelect_GetAvailableToolRanges orig,
            Player self,
            out int pickRange,
            out int axeRange,
            out int hammerRange,
            out int cannonRange,
            out int extractItemRange,
            out int paintScraperRange)
        {
            orig(self, out pickRange, out axeRange, out hammerRange, out cannonRange, out extractItemRange, out paintScraperRange);

            if (self != Main.LocalPlayer) return;
            if (!ValSet.tileBoost.val) return;

            int boost = ValSet.tileBoost_val.val;
            if (boost <= 0) return;

            if (pickRange != -10) pickRange += boost;
            if (axeRange != -10) axeRange += boost;
            if (hammerRange != -10) hammerRange += boost;
            if (cannonRange != -10) cannonRange += boost;
            if (extractItemRange != -10) extractItemRange += boost;
            if (paintScraperRange != -10) paintScraperRange += boost;
        }

        /// <summary>
        /// 增强远距离树木推断：当光标指向树冠树叶或边缘空气时，向下/邻近容差探测树干，确保在远处按 Shift 顺畅掏出斧子
        /// </summary>
        private static void Hook_SmartSelect_GetToolStrategy(
            On_Player.orig_SmartSelect_GetToolStrategy orig,
            Player self,
            int tX,
            int tY,
            out int toolStrategy,
            out bool wetTile)
        {
            orig(self, tX, tY, out toolStrategy, out wetTile);

            if (self != Main.LocalPlayer) return;
            // 如果原版已经选出了破坏工具(1=锤, 2=斧, 3=镐, 6=大炮, 7=提炼机, 8=刮漆)，则无需额外探测
            if (toolStrategy != 0 && toolStrategy != 4 && toolStrategy != 5) return;

            // 检查背包中是否有斧子并计算可达范围
            int axeRange = -10;
            for (int i = 0; i < 50; i++)
            {
                if (self.inventory[i].axe > 0)
                {
                    axeRange = self.inventory[i].tileBoost;
                    if (ValSet.tileBoost.val && ValSet.tileBoost_val.val > 0)
                    {
                        axeRange += ValSet.tileBoost_val.val;
                    }
                    break;
                }
            }

            if (axeRange == -10) return;

            // 在鼠标周围 (左右2格，向下最多8格，向上最多2格) 探测树木/可用斧子破坏的物块
            for (int dy = 0; dy <= 8; dy++)
            {
                int scanY = tY + dy;
                if (scanY < 10 || scanY >= Main.maxTilesY - 10) continue;

                for (int dx = -2; dx <= 2; dx++)
                {
                    int scanX = tX + dx;
                    if (scanX < 10 || scanX >= Main.maxTilesX - 10) continue;

                    Tile tile = Main.tile[scanX, scanY];
                    if (tile == null || !tile.active()) continue;

                    int type = tile.type;
                    if (Main.tileAxe[type])
                    {
                        if (self.IsInTileInteractionRange(scanX, scanY, TileReachCheckSettings.Simple, axeRange))
                        {
                            toolStrategy = 2; // 斧子策略
                            return;
                        }
                    }
                }
            }

            for (int dy = -1; dy >= -2; dy--)
            {
                int scanY = tY + dy;
                if (scanY < 10 || scanY >= Main.maxTilesY - 10) continue;

                for (int dx = -2; dx <= 2; dx++)
                {
                    int scanX = tX + dx;
                    if (scanX < 10 || scanX >= Main.maxTilesX - 10) continue;

                    Tile tile = Main.tile[scanX, scanY];
                    if (tile == null || !tile.active()) continue;

                    int type = tile.type;
                    if (Main.tileAxe[type])
                    {
                        if (self.IsInTileInteractionRange(scanX, scanY, TileReachCheckSettings.Simple, axeRange))
                        {
                            toolStrategy = 2; // 斧子策略
                            return;
                        }
                    }
                }
            }
        }
    }
}
