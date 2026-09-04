using System.Collections.Generic;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using TPML.Content.Fusion;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 通用随身实体容器（随身饰品袋、随身垃圾桶、药水袋、旗帜盒等）统一融合数据源提供者：<br/>
    /// 1. 基于权威缓存 <see cref="CarriedBagCacheManager"/> 统一获取随身携带的全部实体容器；<br/>
    /// 2. 严格保证消耗优先级：在插槽列表中将【随身饰品袋】排在最前列，确保合成进阶饰品时优先扣除饰品袋内材料，收纳容器作为后备兜底；<br/>
    /// 3. 合成消耗后统一执行空槽位净化规范化，并触发各自的写盘与 UI 刷新事件。
    /// 作者: SaintCirno9
    /// </summary>
    public class ItemContainerFusionSource : IFusionItemSource
    {
        public string Id => "OptimizeAndTool.CarriedContainers";

        public int Priority => 100;

        public bool AllowCrafting => true;

        public bool IsActive(Player player)
        {
            if (player == null || !player.active || player.whoAmI != Main.myPlayer) return false;

            var accBags = CarriedBagCacheManager.GetAllAccessoryBags(player);
            var containers = CarriedBagCacheManager.GetAllItemContainers(player);
            return (accBags != null && accBags.Count > 0) || (containers != null && containers.Count > 0);
        }

        public Item[] GetSlots(Player player)
        {
            var accBags = CarriedBagCacheManager.GetAllAccessoryBags(player);
            var containers = CarriedBagCacheManager.GetAllItemContainers(player);

            if ((accBags == null || accBags.Count == 0) && (containers == null || containers.Count == 0))
            {
                return null;
            }

            var result = new List<Item>();

            // 1. 优先放入随身饰品袋中的物品，确保配方合成优先扣除饰品袋内的材料
            if (accBags != null)
            {
                for (int b = 0; b < accBags.Count; b++)
                {
                    var bag = accBags[b];
                    if (bag?.personalInventory != null)
                    {
                        for (int i = 0; i < bag.personalInventory.Length; i++)
                        {
                            Item it = bag.personalInventory[i];
                            if (it != null && !it.IsAir && it.stack > 0)
                            {
                                result.Add(it);
                            }
                        }
                    }
                }
            }

            // 2. 随后放入其他收纳容器（垃圾桶、药水袋、旗帜盒）中的物品作为兜底补充
            if (containers != null)
            {
                for (int c = 0; c < containers.Count; c++)
                {
                    var container = containers[c];
                    if (container?.Slots != null)
                    {
                        for (int i = 0; i < container.Slots.Length; i++)
                        {
                            Item it = container.Slots[i];
                            if (it != null && !it.IsAir && it.stack > 0)
                            {
                                result.Add(it);
                            }
                        }
                    }
                }
            }

            return result.Count > 0 ? result.ToArray() : null;
        }

        public void OnModified(Player player)
        {
            var accBags = CarriedBagCacheManager.GetAllAccessoryBags(player);
            if (accBags != null)
            {
                for (int b = 0; b < accBags.Count; b++)
                {
                    var bag = accBags[b];
                    if (bag?.personalInventory != null)
                    {
                        bool anyCleaned = false;
                        for (int i = 0; i < bag.personalInventory.Length; i++)
                        {
                            Item it = bag.personalInventory[i];
                            if (it != null && (it.IsAir || it.stack <= 0))
                            {
                                bag.personalInventory[i] = new Item();
                                anyCleaned = true;
                            }
                        }

                        if (anyCleaned)
                        {
                            bag.TriggerSlotsChanged();
                        }
                    }
                }
            }

            var containers = CarriedBagCacheManager.GetAllItemContainers(player);
            if (containers != null)
            {
                for (int c = 0; c < containers.Count; c++)
                {
                    var container = containers[c];
                    if (container?.Slots != null)
                    {
                        bool anyCleaned = false;
                        for (int i = 0; i < container.Slots.Length; i++)
                        {
                            Item it = container.Slots[i];
                            if (it != null && (it.IsAir || it.stack <= 0))
                            {
                                container.Slots[i] = new Item();
                                anyCleaned = true;
                            }
                        }

                        if (anyCleaned)
                        {
                            container.TriggerSlotsChanged();
                        }
                    }
                }
            }
        }
    }
}
