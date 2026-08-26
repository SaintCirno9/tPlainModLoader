using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch.Content.UI.ModSet;
using Terraria.UI;

namespace OptimizeAndTool.Content.EnhancedTooltips
{
    /// <summary>
    /// 增强信息提示系统配置中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class EnhancedTooltipConfig
    {
        public static GetSetReset<bool> ShowShimmerInfo = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> ShowAmmoInfo = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> ShowMoreDataInfo = new GetSetReset<bool>(false, false);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("showShimmerInfo", ShowShimmerInfo),
                CommandBuild.get2("showAmmoInfo", ShowAmmoInfo),
                CommandBuild.get2("showMoreDataInfo", ShowMoreDataInfo)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(ShowShimmerInfo, "微光转化提示：在物品提示中显示微光直接蜕变、配方逆合成、钱币幸运与解锁条件", "Images/Item_5364", "微光转化提示"),
                UIBuild.get2(ShowAmmoInfo, "弹药信息提示：在武器和弹药提示中显示消耗弹药或弹药分类", "Images/Item_40", "弹药信息提示"),
                UIBuild.get2(ShowMoreDataInfo, "物品底层数据提示：显示物品内部 ID、名称、使用时间、射速与放置物块等调试数据", "Images/Item_509", "物品底层数据")
            };
        }
    }
}
