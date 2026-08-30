using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 队伍共享便携制作站门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：同队玩家互相共享便携制作站（adjTile），
    /// 即队友携带工作台等制作站时本方无需携带即可合成。
    /// 原版 Recipe.PlayerMeetsEnvironmentConditions 仅检查玩家自身 adjTile（Recipe.cs:325），
    /// 原版无任何 team 维度支持，此处拦截 Player.AdjTiles 合并同队玩家 adjTile。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class TeamShareHooks
    {
        public static GetSetReset<bool> EnableShareCraftingStation = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.AdjTiles += Hook_AdjTiles;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.AdjTiles -= Hook_AdjTiles;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("shareCraftingStation", EnableShareCraftingStation)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableShareCraftingStation, "同队玩家互相共享便携制作站：队友携带工作台/熔炉等时本方无需携带", "Images/Item_361", "队伍共享便携制作站")
            };
        }

        private static void Hook_AdjTiles(On_Player.orig_AdjTiles orig, Player self)
        {
            orig(self);

            if (self == null || !EnableShareCraftingStation.val || self.team <= 0) return;

            try
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player teammate = Main.player[i];
                    if (teammate == null || teammate == self || !teammate.active || teammate.dead || teammate.adjTile == null) continue;
                    if (teammate.team != self.team) continue;

                    int len = Math.Min(self.adjTile?.Length ?? 0, teammate.adjTile.Length);
                    for (int t = 0; t < len; t++)
                    {
                        if (teammate.SafeGetAdjTile(t))
                        {
                            self.SafeSetAdjTile(t, true);
                        }
                    }
                }
            }
            catch
            {
                // 防御性保护：避免队伍共享计算异常中断游戏循环
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class TeamShare
    {
        public static GetSetReset<bool> EnableShareCraftingStation => TeamShareHooks.EnableShareCraftingStation;

        public static List<CommandObject> GetCO() => TeamShareHooks.GetCO();
        public static List<UIElement> GetUI() => TeamShareHooks.GetUI();
    }
}
