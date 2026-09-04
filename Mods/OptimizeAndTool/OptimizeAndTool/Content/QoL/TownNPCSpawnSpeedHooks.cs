using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 城镇 NPC 刷新速度乘数门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：入住节奏由 WorldGen.npcSpawnPeriod
    /// 控制（WorldGen.cs:75481 的 ++npcSpawnDelay >= npcSpawnPeriod），倍率 >1 加速入住。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class TownNPCSpawnSpeedHooks
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> Multiplier = new GetSetReset<float>(2f, 2f, v => v < 0.1f ? 0.1f : (v > 20f ? 20f : v));

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_WorldGen.TrySpawningTownNPC += Hook_TrySpawningTownNPC;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_WorldGen.TrySpawningTownNPC -= Hook_TrySpawningTownNPC;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get1("townNPCSpawnSpeed", Enable, Multiplier)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get1(Enable, Multiplier, float.Parse, "城镇 NPC 入住/刷新速度乘数：1=原版，2=两倍速度<float>", "Images/Item_4910", "城镇 NPC 刷新速度乘数")
            };
        }

        private static void Hook_TrySpawningTownNPC(On_WorldGen.orig_TrySpawningTownNPC orig, int x, int y)
        {
            int originalPeriod = 0;
            if (Enable.val && Main.netMode != 1)
            {
                originalPeriod = WorldGen.npcSpawnPeriod;
                int v = (int)(originalPeriod / Multiplier.val);
                WorldGen.npcSpawnPeriod = v < 1 ? 1 : v;
            }

            try
            {
                orig(x, y);
            }
            finally
            {
                if (originalPeriod > 0)
                {
                    WorldGen.npcSpawnPeriod = originalPeriod;
                }
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class TownNPCSpawnSpeed
    {
        public static GetSetReset<bool> Enable => TownNPCSpawnSpeedHooks.Enable;
        public static GetSetReset<float> Multiplier => TownNPCSpawnSpeedHooks.Multiplier;

        public static List<CommandObject> GetCO() => TownNPCSpawnSpeedHooks.GetCO();
        public static List<UIElement> GetUI() => TownNPCSpawnSpeedHooks.GetUI();
    }
}
