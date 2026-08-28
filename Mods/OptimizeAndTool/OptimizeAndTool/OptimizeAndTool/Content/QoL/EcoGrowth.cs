using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 生态生长加速（对齐 ImproveGame 语义）：
    /// 1. 南瓜迅速生长：GrowPumpkin 每触发一次即连续推进多个阶段直至封顶；
    /// 2. 生命果迅速生长：原版仅在硬模式击败任意机械 Boss 后以 1/40（专家 1/30）概率
    ///    经 PlaceJunglePlant 生成（WorldGen.cs:75059），开启后每次生成额外在附近补种。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class EcoGrowth
    {
        public static GetSetReset<bool> EnablePumpkinFastGrow = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableLifeFruitFastGrow = new GetSetReset<bool>(false, false);

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
    }

    /// <summary>
    /// 南瓜迅速生长：GrowPumpkin（WorldGen.cs:52958）单次调用推进一阶段（内部 num4 &gt;= 4 封顶），
    /// Postfix 防递归连调 3 次，等效一次触发长满 4 阶段。
    /// </summary>
    [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.GrowPumpkin))]
    internal static class Patch_PumpkinFastGrow
    {
        private static bool inRecursion = false;

        [HarmonyPostfix]
        internal static void Postfix(int i, int j, int type)
        {
            if (inRecursion || !EcoGrowth.EnablePumpkinFastGrow.val) return;
            inRecursion = true;
            try
            {
                for (int k = 0; k < 3; k++)
                {
                    WorldGen.GrowPumpkin(i, j, type);
                }
            }
            finally
            {
                inRecursion = false;
            }
        }
    }

    /// <summary>
    /// 生命果迅速生长：原版生成概率极低（1/40/1/30），Postfix 在每次成功种下生命果后
    /// 以 60% 概率在附近随机偏移再补种 1 个（沿用原版 PlaceJunglePlant 自身的位置校验）。
    /// </summary>
    [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.PlaceJunglePlant))]
    internal static class Patch_LifeFruitFastGrow
    {
        private static bool inRecursion = false;

        [HarmonyPostfix]
        internal static void Postfix(int X2, int Y2, ushort type)
        {
            if (inRecursion || !EcoGrowth.EnableLifeFruitFastGrow.val) return;
            if (type != TileID.LifeFruit) return;
            if (Main.rand.Next(5) >= 3) return; // 60% 补种
            inRecursion = true;
            try
            {
                int nx = X2 + Main.rand.Next(-8, 9);
                int ny = Y2 + Main.rand.Next(-2, 3);
                if (nx >= 5 && nx < Main.maxTilesX - 5 && ny >= 5 && ny < Main.maxTilesY - 5)
                {
                    WorldGen.PlaceJunglePlant(nx, ny, TileID.LifeFruit, Main.rand.Next(3), 0, inheritPaint: true);
                }
            }
            finally
            {
                inRecursion = false;
            }
        }
    }
}
