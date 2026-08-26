using OptimizeAndTool.Content.Storage.Core;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋物品槽位 UI（继承自 UniversalBagSlot 保持向后兼容）
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagSlot : UniversalBagSlot
    {
        public AccessoryBagSlot(AccessoryBagItem bag, int slotIndex) : base(bag, slotIndex)
        {
        }
    }
}
