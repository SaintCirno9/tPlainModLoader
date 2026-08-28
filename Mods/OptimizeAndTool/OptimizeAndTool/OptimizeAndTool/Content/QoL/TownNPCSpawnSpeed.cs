using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 城镇 NPC 刷新速度乘数（对齐 ImproveGame 语义）：入住节奏由 WorldGen.npcSpawnPeriod
    /// 控制（WorldGen.cs:75481 的 ++npcSpawnDelay &gt;= npcSpawnPeriod），倍率 &gt;1 加速入住。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class TownNPCSpawnSpeed
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> Multiplier = new GetSetReset<float>(2f, 2f, v => v < 0.1f ? 0.1f : (v > 20f ? 20f : v));

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
    }

    /// <summary>
    /// 刷新加速：TrySpawningTownNPC 每 tick 判定 ++npcSpawnDelay &gt;= npcSpawnPeriod，
    /// Prefix 仅在本帧把周期临时缩小（/倍率），Postfix 恢复，避免累积。
    /// </summary>
    [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.TrySpawningTownNPC))]
    internal static class Patch_TownNPCSpawnSpeed
    {
        private static int originalPeriod = 0;

        [HarmonyPrefix]
        internal static void Prefix()
        {
            if (!TownNPCSpawnSpeed.Enable.val || Main.netMode == 1) return;
            originalPeriod = WorldGen.npcSpawnPeriod;
            int v = (int)(originalPeriod / TownNPCSpawnSpeed.Multiplier.val);
            WorldGen.npcSpawnPeriod = v < 1 ? 1 : v;
        }

        [HarmonyFinalizer]
        internal static void Finalizer()
        {
            if (originalPeriod > 0)
            {
                WorldGen.npcSpawnPeriod = originalPeriod;
                originalPeriod = 0;
            }
        }
    }
}
