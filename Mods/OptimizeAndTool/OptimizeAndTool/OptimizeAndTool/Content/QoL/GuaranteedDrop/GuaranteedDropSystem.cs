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
    /// 全场景全物品必定全量大爆系统配置与 UI 注册
    /// 作者: SaintCirno9
    /// </summary>
    internal static class GuaranteedDropSystem
    {
        /// <summary>怪物必定掉落所有可能物品与全量大爆总开关</summary>
        public static readonly GetSetReset<bool> EnableGuaranteedDrop = new GetSetReset<bool>(true, true);

        /// <summary>多选一掉落池全量大爆开关</summary>
        public static readonly GetSetReset<bool> EnableMultiOptionBurst = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("dropGuaranteed", EnableGuaranteedDrop),
                CommandBuild.get2("dropMultiBurst", EnableMultiOptionBurst)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableGuaranteedDrop, "所有怪物击杀时 100% 必定掉落其全部可能的物品与战利品；17 种原版 Boss 宝藏袋与钓鱼宝匣/锁盒开启时永久全量大爆（肉后 Boss 袋必出 1 套完整开发者套装）", "Images/Item_5010", "怪物必定全量大爆"),
                UIBuild.get2(EnableMultiOptionBurst, "怪物与 Boss 本体的多选一/多选多掉落池（如肉山徽章与武器），击杀时全量大爆特爆（一次性掉落所有专属战利品）", "Images/Item_3324", "多选一全量大爆")
            };
        }
    }
}
