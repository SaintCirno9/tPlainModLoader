using OptimizeAndTool.Content.Storage.Core;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 容器物品槽（继承自 UniversalBagSlot 保持向后兼容）
    /// 作者: SaintCirno9
    /// </summary>
    public class ItemContainerSlot : UniversalBagSlot
    {
        public ItemContainerSlot(IItemContainer container, int slotIndex) : base(container, slotIndex)
        {
        }
    }
}
