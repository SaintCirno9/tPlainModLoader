using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 床与睡眠规则（对齐 ImproveGame 语义）：
    /// 1. 任意位置设置重生点（不需要完整房间判定，保留床判定）；
    /// 2. 无视睡觉限制（危险/血月/日食/手持物品均不阻断入睡）；
    /// 3. 睡觉时间速率（默认 5，可自定义倍率）；
    /// 4. 一人睡觉即可加速（多人无需全员入睡）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class BedRules
    {
        public static GetSetReset<bool> EnableBedAnywhere = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableNoSleepRestrictions = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableBedTimeRate = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> BedTimeRate = new GetSetReset<float>(10f, 10f, v => v < 1f ? 1f : (v > 100f ? 100f : v));
        public static GetSetReset<bool> EnableOnePlayerSleep = new GetSetReset<bool>(false, false);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("bedAnywhere", EnableBedAnywhere),
                CommandBuild.get2("noSleepRestrictions", EnableNoSleepRestrictions),
                CommandBuild.get1("bedTimeRate", EnableBedTimeRate, BedTimeRate),
                CommandBuild.get2("onePlayerSleep", EnableOnePlayerSleep)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableBedAnywhere, "床在任何情况下均可设置重生点，不再要求完整房间", "Images/Item_2129", "任意位置设置重生点"),
                UIBuild.get2(EnableNoSleepRestrictions, "睡觉时间加速不再被危险/血月/日食/手持物品等阻断", "Images/Item_2129", "无视睡觉限制"),
                UIBuild.get1(EnableBedTimeRate, BedTimeRate, float.Parse, "睡觉时的时间流速倍率（原版为 5 倍）<float>", "Images/Item_2129", "睡觉时间速率"),
                UIBuild.get2(EnableOnePlayerSleep, "多人模式下只需一名玩家睡觉即可加速时间", "Images/Item_2129", "一人睡觉即可加速")
            };
        }
    }

    /// <summary>
    /// 任意重生点：Player.CheckSpawn（Player.cs:55190）的完整房间判定（StartRoomCheck）放宽为仅需床。
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.CheckSpawn))]
    internal static class Patch_BedAnywhere
    {
        [HarmonyPrefix]
        internal static bool Prefix(int x, int y, ref bool __result)
        {
            if (!BedRules.EnableBedAnywhere.val) return true;
            if (x < 0 || y <= 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return true;
            if (Main.tile[x, y - 1] != null && Main.tile[x, y - 1].active() && Main.tile[x, y - 1].type == TileID.Beds)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 无视睡觉限制：PlayerSleepingHelper.DoesPlayerHaveReasonToActUpInBed（L45-64）恒返回 false，
    /// 使 timeSleeping 不再被清零，2 秒后必然 FullyFallenAsleep。
    /// </summary>
    [HarmonyPatch(typeof(PlayerSleepingHelper), "DoesPlayerHaveReasonToActUpInBed")]
    internal static class Patch_NoSleepRestrictions
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref bool __result)
        {
            if (!BedRules.EnableNoSleepRestrictions.val) return true;
            __result = false;
            return false;
        }
    }

    /// <summary>
    /// 睡觉时间速率 + 一人睡觉加速：两者都作用于 Main.UpdateTimeRate（Main.cs:6377），
    /// 合并为单一 Patch 类避免多个 Postfix 的执行顺序不确定性：
    /// Prefix 在"一人睡觉"开启时伪造全睡计数（6387 全睡判定），
    /// Postfix 先按当前计数（含伪造值）应用自定义倍率，再恢复计数（下一帧 UpdateWorld_Players 重算）。
    /// </summary>
    [HarmonyPatch(typeof(Main), nameof(Main.UpdateTimeRate))]
    internal static class Patch_SleepTimeRate
    {
        private static int savedSleeping = 0;
        private static bool modifiedCount = false;

        [HarmonyPrefix]
        internal static void Prefix()
        {
            if (BedRules.EnableOnePlayerSleep.val
                && Main.CurrentFrameFlags.SleepingPlayersCount > 0
                && Main.CurrentFrameFlags.SleepingPlayersCount < Main.CurrentFrameFlags.ActivePlayersCount)
            {
                savedSleeping = Main.CurrentFrameFlags.SleepingPlayersCount;
                Main.CurrentFrameFlags.SleepingPlayersCount = Main.CurrentFrameFlags.ActivePlayersCount;
                modifiedCount = true;
            }
        }

        [HarmonyFinalizer]
        internal static void Finalizer()
        {
            if (BedRules.EnableBedTimeRate.val)
            {
                bool allSleeping = Main.CurrentFrameFlags.SleepingPlayersCount == Main.CurrentFrameFlags.ActivePlayersCount
                    && Main.CurrentFrameFlags.SleepingPlayersCount > 0;
                if (allSleeping)
                {
                    Main.dayRate = (int)(Main.dayRate / 5f * BedRules.BedTimeRate.val);
                    if (Main.dayRate < 1) Main.dayRate = 1;
                }
            }

            if (modifiedCount)
            {
                Main.CurrentFrameFlags.SleepingPlayersCount = savedSleeping;
                modifiedCount = false;
            }
        }
    }
}
