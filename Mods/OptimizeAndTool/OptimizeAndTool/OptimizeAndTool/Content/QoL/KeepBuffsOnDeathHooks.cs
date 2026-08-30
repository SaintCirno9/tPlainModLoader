using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 死亡保存增益门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：玩家死亡后增益不再被清除。
    /// 原版 Player.UpdateDead（Player.cs:17109-17116）每帧清空全部非持久 buff，
    /// 这里在 UpdateDead 前快照 buffType/buffTime，执行后写回（死亡期间 buff 冻结，复活后保留）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class KeepBuffsOnDeathHooks
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.UpdateDead += Hook_UpdateDead;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.UpdateDead -= Hook_UpdateDead;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("keepBuffsOnDeath", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "死亡后保留全部增益（含战斗增益），复活后继续生效", "Images/Item_316", "死亡保存增益")
            };
        }

        private static void Hook_UpdateDead(On_Player.orig_UpdateDead orig, Player self)
        {
            int[] savedBuffType = null;
            int[] savedBuffTime = null;

            if (Enable.val)
            {
                savedBuffType = (int[])self.buffType.Clone();
                savedBuffTime = (int[])self.buffTime.Clone();
            }

            try
            {
                orig(self);
            }
            finally
            {
                if (Enable.val && savedBuffType != null)
                {
                    for (int i = 0; i < self.buffType.Length; i++)
                    {
                        if (savedBuffType[i] > 0)
                        {
                            self.buffType[i] = savedBuffType[i];
                            self.buffTime[i] = savedBuffTime[i];
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class KeepBuffsOnDeath
    {
        public static GetSetReset<bool> Enable => KeepBuffsOnDeathHooks.Enable;

        public static List<CommandObject> GetCO() => KeepBuffsOnDeathHooks.GetCO();
        public static List<UIElement> GetUI() => KeepBuffsOnDeathHooks.GetUI();
    }
}
