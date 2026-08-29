using CommandHelp;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.GuaranteedDrop
{
    /// <summary>
    /// 全场景全物品首见保底掉落系统配置与 UI 注册
    /// 作者: SaintCirno9
    /// </summary>
    internal static class GuaranteedDropSystem
    {
        /// <summary>首见掉落全保底总开关</summary>
        public static readonly GetSetReset<bool> EnableGuaranteedDrop = new GetSetReset<bool>(true, true);

        /// <summary>多选一掉落池首次全量大爆开关</summary>
        public static readonly GetSetReset<bool> EnableMultiOptionBurst = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("dropGuaranteed", EnableGuaranteedDrop),
                CommandBuild.get2("dropMultiBurst", EnableMultiOptionBurst),
                new CommandResetDiscovered("dropReset")
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableGuaranteedDrop, "怪物与Boss本体掉落享受首见保底（首次掉齐未拥有战利品）；17种原版Boss宝藏袋与钓鱼宝匣/锁盒开启时永久全量大爆（肉后Boss袋必出1套完整开发者套装）", "Images/Item_5010", "掉落优化与全量开箱"),
                UIBuild.get2(EnableMultiOptionBurst, "怪物与Boss本体的多选一/多选多掉落池（如肉山徽章与武器），首次击杀全量大爆特爆（一次性掉齐所有未拥有的专属战利品）", "Images/Item_3324", "多选一全量大爆")
            };
        }

        /// <summary>
        /// 控制台重置指令：清空当前角色的历史已发现记忆并重新深度扫描当前背包
        /// </summary>
        private class CommandResetDiscovered : CommandObject
        {
            public CommandResetDiscovered(string name) : base(name)
            {
                TipText = "重置当前角色的历史掉落发现记忆，重新建档。";
            }

            public override object Run(ref int index, List<CommandObject> commandList)
            {
                Player player = Main.LocalPlayer;
                if (player != null)
                {
                    DiscoveredItemTracker.ResetDiscovered(player);
                    Main.NewText($"[保底掉落] 已重置角色 [{player.name}] 的历史掉落记忆，当前已重新扫描收录随身物品: {DiscoveredItemTracker.DiscoveredCount} 件。");
                }
                return this;
            }
        }
    }
}
