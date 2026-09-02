using System.Collections.Generic;
using Terraria;
using TPML.Content.Fusion;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋在 TPML 背包融合系统中的材料提供者：
    /// 让饰品袋内的所有饰品直接参与工作台配方合成与自动扣料
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagFusionSource : IFusionItemSource
    {
        public string Id => "OptimizeAndTool.AccessoryBag";

        public int Priority => 90;

        public bool AllowCrafting => AccessoryBagConfig.EnableAccessoryBagCraft.val;

        public bool IsActive(Player player)
        {
            if (player == null || !player.active || player.whoAmI != Main.myPlayer) return false;
            var bags = AccessoryBagCacheManager.GetAllBags();
            return bags != null && bags.Count > 0;
        }

        public Item[] GetSlots(Player player)
        {
            var bags = AccessoryBagCacheManager.GetAllBags();
            if (bags == null || bags.Count == 0) return null;

            var snapshot = bags is List<AccessoryBagItem> list ? list.ToArray() : new List<AccessoryBagItem>(bags).ToArray();
            var result = new List<Item>();
            for (int b = 0; b < snapshot.Length; b++)
            {
                var bag = snapshot[b];
                if (bag?.personalInventory != null)
                {
                    for (int i = 0; i < bag.personalInventory.Length; i++)
                    {
                        Item it = bag.personalInventory[i];
                        if (it != null && !it.IsAir) result.Add(it);
                    }
                }
            }

            return result.ToArray();
        }

        public void OnModified(Player player)
        {
            var bags = AccessoryBagCacheManager.GetAllBags();
            if (bags != null)
            {
                var snapshot = bags is List<AccessoryBagItem> list ? list.ToArray() : new List<AccessoryBagItem>(bags).ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    snapshot[i]?.TriggerSlotsChanged();
                }
            }
        }
    }
}
