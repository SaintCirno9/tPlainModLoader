using Terraria;
using TPML.Content.Fusion;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 大背包在 TPML.Content 框架通用背包融合系统中的数据源提供者
    /// 作者: SaintCirno9
    /// </summary>
    public class BigBagFusionSource : IFusionItemSource
    {
        public string Id => "OptimizeAndTool.BigBag";

        public int Priority => 100;

        public bool AllowCrafting => BigBag.EnableBigBagCraft.val;

        public bool IsActive(Player player)
        {
            if (!BigBag.EnableBigBag.val) return false;
            if (player == null || !player.active || player.whoAmI != Main.myPlayer) return false;
            return BigBag.Slots != null && BigBag.Slots.Length > 0;
        }

        public Item[] GetSlots(Player player)
        {
            return BigBag.Slots;
        }

        public void OnModified(Player player)
        {
            BigBag.NotifySlotsChanged();
        }
    }
}
