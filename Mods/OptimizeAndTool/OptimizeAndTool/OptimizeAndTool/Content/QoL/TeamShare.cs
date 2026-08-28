using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 队伍共享便携制作站（对齐 ImproveGame 语义）：同队玩家互相共享便携制作站（adjTile），
    /// 即队友携带工作台等制作站时本方无需携带即可合成。
    /// 原版 Recipe.PlayerMeetsEnvironmentConditions 仅检查玩家自身 adjTile（Recipe.cs:325），
    /// 原版无任何 team 维度支持，此处 Postfix Player.AdjTiles 合并同队玩家 adjTile。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class TeamShare
    {
        public static GetSetReset<bool> EnableShareCraftingStation = new GetSetReset<bool>(false, false);

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
    }

    /// <summary>
    /// 队伍共享：Player.AdjTiles()（Player.cs:35940）结束合并同队活跃玩家的 adjTile。
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AdjTiles))]
    internal static class Patch_TeamShare
    {
        [HarmonyPostfix]
        internal static void Postfix(Player __instance)
        {
            if (!TeamShare.EnableShareCraftingStation.val) return;
            if (__instance.team <= 0) return;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player teammate = Main.player[i];
                if (teammate == null || teammate == __instance || !teammate.active || teammate.dead) continue;
                if (teammate.team != __instance.team) continue;
                for (int t = 0; t < __instance.adjTile.Length; t++)
                {
                    if (teammate.adjTile[t])
                    {
                        __instance.adjTile[t] = true;
                    }
                }
            }
        }
    }
}
