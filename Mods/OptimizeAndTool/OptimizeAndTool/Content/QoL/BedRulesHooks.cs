using CommandHelp;
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
    /// 床与睡眠规则门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：
    /// 1. 任意位置设置重生点（不需要完整房间判定，保留床判定）；
    /// 2. 无视睡觉限制（危险/血月/日食/手持物品均不阻断入睡）；
    /// 3. 睡觉时间速率（默认 5，可自定义倍率）；
    /// 4. 一人睡觉即可加速（多人无需全员入睡）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class BedRulesHooks
    {
        public static GetSetReset<bool> EnableBedAnywhere = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableNoSleepRestrictions = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableBedTimeRate = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> BedTimeRate = new GetSetReset<float>(10f, 10f, v => v < 1f ? 1f : (v > 100f ? 100f : v));
        public static GetSetReset<bool> EnableOnePlayerSleep = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.CheckSpawn += Hook_CheckSpawn;
            On_PlayerSleepingHelper.DoesPlayerHaveReasonToActUpInBed += Hook_DoesPlayerHaveReasonToActUpInBed;
            On_Main.UpdateTimeRate += Hook_UpdateTimeRate;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.CheckSpawn -= Hook_CheckSpawn;
            On_PlayerSleepingHelper.DoesPlayerHaveReasonToActUpInBed -= Hook_DoesPlayerHaveReasonToActUpInBed;
            On_Main.UpdateTimeRate -= Hook_UpdateTimeRate;
            _registered = false;
        }

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

        private static bool Hook_CheckSpawn(On_Player.orig_CheckSpawn orig, int x, int y)
        {
            if (EnableBedAnywhere.val)
            {
                if (x >= 0 && y > 0 && x < Main.maxTilesX && y < Main.maxTilesY)
                {
                    if (Main.tile[x, y - 1] != null && Main.tile[x, y - 1].active() && Main.tile[x, y - 1].type == TileID.Beds)
                    {
                        return true;
                    }
                }
            }

            return orig(x, y);
        }

        private static bool Hook_DoesPlayerHaveReasonToActUpInBed(On_PlayerSleepingHelper.orig_DoesPlayerHaveReasonToActUpInBed orig, ref PlayerSleepingHelper self, Player player)
        {
            if (EnableNoSleepRestrictions.val)
            {
                return false;
            }

            return orig(ref self, player);
        }

        private static void Hook_UpdateTimeRate(On_Main.orig_UpdateTimeRate orig)
        {
            int savedSleeping = 0;
            bool modifiedCount = false;

            if (EnableOnePlayerSleep.val
                && Main.CurrentFrameFlags.SleepingPlayersCount > 0
                && Main.CurrentFrameFlags.SleepingPlayersCount < Main.CurrentFrameFlags.ActivePlayersCount)
            {
                savedSleeping = Main.CurrentFrameFlags.SleepingPlayersCount;
                Main.CurrentFrameFlags.SleepingPlayersCount = Main.CurrentFrameFlags.ActivePlayersCount;
                modifiedCount = true;
            }

            try
            {
                orig();

                if (EnableBedTimeRate.val)
                {
                    bool allSleeping = Main.CurrentFrameFlags.SleepingPlayersCount == Main.CurrentFrameFlags.ActivePlayersCount
                        && Main.CurrentFrameFlags.SleepingPlayersCount > 0;
                    if (allSleeping)
                    {
                        Main.dayRate = (int)(Main.dayRate / 5f * BedTimeRate.val);
                        if (Main.dayRate < 1) Main.dayRate = 1;
                    }
                }
            }
            finally
            {
                if (modifiedCount)
                {
                    Main.CurrentFrameFlags.SleepingPlayersCount = savedSleeping;
                }
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class BedRules
    {
        public static GetSetReset<bool> EnableBedAnywhere => BedRulesHooks.EnableBedAnywhere;
        public static GetSetReset<bool> EnableNoSleepRestrictions => BedRulesHooks.EnableNoSleepRestrictions;
        public static GetSetReset<bool> EnableBedTimeRate => BedRulesHooks.EnableBedTimeRate;
        public static GetSetReset<float> BedTimeRate => BedRulesHooks.BedTimeRate;
        public static GetSetReset<bool> EnableOnePlayerSleep => BedRulesHooks.EnableOnePlayerSleep;

        public static List<CommandObject> GetCO() => BedRulesHooks.GetCO();
        public static List<UIElement> GetUI() => BedRulesHooks.GetUI();
    }
}
