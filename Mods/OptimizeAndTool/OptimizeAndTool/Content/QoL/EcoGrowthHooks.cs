using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 生态生长加速门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：
    /// 1. 南瓜迅速生长：GrowPumpkin 每触发一次即连续推进多个阶段直至封顶；
    /// 2. 生命果迅速生长：原版仅在硬模式击败任意机械 Boss 后以 1/40（专家 1/30）概率
    ///    经 PlaceJunglePlant 生成（WorldGen.cs:75059），开启后每次生成额外在附近补种。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class EcoGrowthHooks
    {
        public static GetSetReset<bool> EnablePumpkinFastGrow = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableLifeFruitFastGrow = new GetSetReset<bool>(false, false);

        private static bool _registered = false;
        private static bool inPumpkinRecursion = false;
        private static bool inLifeFruitRecursion = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_WorldGen.GrowPumpkin += Hook_GrowPumpkin;
            On_WorldGen.PlaceJunglePlant += Hook_PlaceJunglePlant;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_WorldGen.GrowPumpkin -= Hook_GrowPumpkin;
            On_WorldGen.PlaceJunglePlant -= Hook_PlaceJunglePlant;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("pumpkinFastGrow", EnablePumpkinFastGrow),
                CommandBuild.get2("lifeFruitFastGrow", EnableLifeFruitFastGrow)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnablePumpkinFastGrow, "南瓜藤生长速度大幅提升（每次生长直接推进至多 4 个阶段）", "Images/Item_1725", "南瓜迅速生长"),
                UIBuild.get2(EnableLifeFruitFastGrow, "生命果生成后以概率在附近补种，加快累积速度", "Images/Item_1291", "生命果迅速生长")
            };
        }

        private static void Hook_GrowPumpkin(On_WorldGen.orig_GrowPumpkin orig, int i, int j, int type)
        {
            orig(i, j, type);

            if (inPumpkinRecursion || !EnablePumpkinFastGrow.val) return;
            inPumpkinRecursion = true;
            try
            {
                for (int k = 0; k < 3; k++)
                {
                    orig(i, j, type);
                }
            }
            finally
            {
                inPumpkinRecursion = false;
            }
        }

        private static void Hook_PlaceJunglePlant(On_WorldGen.orig_PlaceJunglePlant orig, int X2, int Y2, ushort type, int styleX, int styleY, bool inheritPaint)
        {
            orig(X2, Y2, type, styleX, styleY, inheritPaint);

            if (inLifeFruitRecursion || !EnableLifeFruitFastGrow.val) return;
            if (type != TileID.LifeFruit) return;
            if (Main.rand.Next(5) >= 3) return; // 60% 补种
            inLifeFruitRecursion = true;
            try
            {
                int nx = X2 + Main.rand.Next(-8, 9);
                int ny = Y2 + Main.rand.Next(-2, 3);
                if (nx >= 5 && nx < Main.maxTilesX - 5 && ny >= 5 && ny < Main.maxTilesY - 5)
                {
                    orig(nx, ny, TileID.LifeFruit, Main.rand.Next(3), 0, true);
                }
            }
            finally
            {
                inLifeFruitRecursion = false;
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class EcoGrowth
    {
        public static GetSetReset<bool> EnablePumpkinFastGrow => EcoGrowthHooks.EnablePumpkinFastGrow;
        public static GetSetReset<bool> EnableLifeFruitFastGrow => EcoGrowthHooks.EnableLifeFruitFastGrow;

        public static List<CommandObject> GetCO() => EcoGrowthHooks.GetCO();
        public static List<UIElement> GetUI() => EcoGrowthHooks.GetUI();
    }
}
