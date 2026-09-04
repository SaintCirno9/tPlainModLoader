using OptimizeAndTool.Content.Storage.Core;
using Terraria;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 巨大背包物品格（继承自 UniversalBagSlot 保持向后兼容）
    /// 作者: SaintCirno9
    /// </summary>
    public class BigBagItem : UniversalBagSlot
    {
        public BigBagItem(Item[] inv, int slot) : base(BigBag.Inventory, slot)
        {
        }
    }
}
