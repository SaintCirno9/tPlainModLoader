using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;

namespace OptimizeAndTool.Content.EnhancedTooltips
{
    /// <summary>
    /// 微光转化计算与格式化工具类
    /// 作者: SaintCirno9
    /// </summary>
    public static class ShimmerHelper
    {
        public static readonly Microsoft.Xna.Framework.Color ShimmerColor = new Microsoft.Xna.Framework.Color(241, 175, 233);

        /// <summary>
        /// 查询物品的微光转化结果与解锁状态
        /// </summary>
        public static bool TryGetShimmerTooltip(Item item, out string tooltipText)
        {
            tooltipText = null;
            if (item == null || item.IsAir || item.type <= ItemID.None) return false;

            // 1. 钱币幸运检查
            if (item.type >= ItemID.CopperCoin && item.type <= ItemID.PlatinumCoin && ItemID.Sets.CommonCoin[item.type])
            {
                int luckVal = 0;
                switch (item.type)
                {
                    case ItemID.CopperCoin: luckVal = 1; break;
                    case ItemID.SilverCoin: luckVal = 100; break;
                    case ItemID.GoldCoin: luckVal = 10000; break;
                    case ItemID.PlatinumCoin: luckVal = 1000000; break;
                }
                if (luckVal > 0)
                {
                    tooltipText = $"丢入微光增加金币幸运: {luckVal}";
                    return true;
                }
            }

            int shimmerType = item.GetShimmerEquivalentType(false);
            int decraftShimmerType = item.GetShimmerEquivalentType(true);

            int stackRequired = 1;
            bool isDecrafting = false;
            bool isLocked = false;
            string lockReason = null;
            List<Item> results = null;

            // 2. 特殊直接蜕变与月总后蜕变检查
            switch (shimmerType)
            {
                case ItemID.RodofDiscord: // 1326
                    results = new List<Item> { CreateItem(ItemID.RodOfHarmony) };
                    if (!NPC.downedMoonlord) { isLocked = true; lockReason = "需击败月球领主解锁"; }
                    break;
                case ItemID.Clentaminator: // 779
                    results = new List<Item> { CreateItem(ItemID.Clentaminator2) };
                    if (!NPC.downedMoonlord) { isLocked = true; lockReason = "需击败月球领主解锁"; }
                    break;
                case ItemID.BottomlessBucket: // 3031
                    results = new List<Item> { CreateItem(ItemID.BottomlessShimmerBucket) };
                    if (!NPC.downedMoonlord) { isLocked = true; lockReason = "需击败月球领主解锁"; }
                    break;
                case ItemID.BottomlessShimmerBucket: // 5364
                    results = new List<Item> { CreateItem(ItemID.BottomlessBucket) };
                    if (!NPC.downedMoonlord) { isLocked = true; lockReason = "需击败月球领主解锁"; }
                    break;
                case 3461: // 日耀砖 (按月相蜕变)
                    MoonPhase moonPhase = Main.GetMoonPhase();
                    int targetBrick;
                    switch (moonPhase)
                    {
                        case MoonPhase.QuarterAtRight: targetBrick = 5407; break;
                        case MoonPhase.HalfAtRight: targetBrick = 5405; break;
                        case MoonPhase.ThreeQuartersAtRight: targetBrick = 5404; break;
                        case MoonPhase.Full: targetBrick = 5408; break;
                        case MoonPhase.ThreeQuartersAtLeft: targetBrick = 5401; break;
                        case MoonPhase.HalfAtLeft: targetBrick = 5403; break;
                        case MoonPhase.QuarterAtLeft: targetBrick = 5402; break;
                        default: targetBrick = 5406; break;
                    }
                    results = new List<Item> { CreateItem(targetBrick) };
                    break;
                default:
                    if (item.createTile == TileID.MusicBoxes)
                    {
                        results = new List<Item> { CreateItem(ItemID.MusicBox) };
                    }
                    break;
            }

            // 3. 通用直接蜕变检查
            if (results == null)
            {
                int directTransform = ShimmerTransforms.GetTransformToItem(shimmerType);
                if (directTransform > 0)
                {
                    results = new List<Item> { CreateItem(directTransform) };
                    if (ShimmerTransforms.IsItemTransformLocked(shimmerType))
                    {
                        isLocked = true;
                        lockReason = "需击败月球领主解锁";
                    }
                }
            }

            // 4. 配方逆合成/材料分解 (Decrafting) 检查
            if (results == null)
            {
                int decraftIndex = ShimmerTransforms.GetDecraftingRecipeIndex(decraftShimmerType);
                if (decraftIndex >= 0 && decraftIndex < Main.recipe.Length)
                {
                    Recipe recipe = Main.recipe[decraftIndex];
                    if (recipe != null)
                    {
                        isDecrafting = true;
                        stackRequired = recipe.createItem.stack;
                        if (recipe.customShimmerResults != null)
                        {
                            results = recipe.customShimmerResults;
                        }
                        else if (recipe.requiredItem != null)
                        {
                            results = new List<Item>(recipe.requiredItem);
                        }

                        if (ShimmerTransforms.IsRecipeIndexDecraftLocked(decraftIndex))
                        {
                            isLocked = true;
                            if (ShimmerTransforms.RecipeSets.PostSkeletron != null &&
                                decraftIndex < ShimmerTransforms.RecipeSets.PostSkeletron.Length &&
                                ShimmerTransforms.RecipeSets.PostSkeletron[decraftIndex] &&
                                !NPC.downedBoss3)
                            {
                                lockReason = "需击败骷髅王解锁";
                            }
                            else if (ShimmerTransforms.RecipeSets.PostGolem != null &&
                                     decraftIndex < ShimmerTransforms.RecipeSets.PostGolem.Length &&
                                     ShimmerTransforms.RecipeSets.PostGolem[decraftIndex] &&
                                     !NPC.downedGolemBoss)
                            {
                                lockReason = "需击败石巨人解锁";
                            }
                            else
                            {
                                lockReason = "未解锁";
                            }
                        }
                    }
                }
            }

            if (results == null || results.Count == 0) return false;

            // 5. 格式化构建 Tooltip 文本
            StringBuilder sb = new StringBuilder();
            if (stackRequired > 1)
            {
                sb.Append($"微光转化 (需 {stackRequired} 个): ");
            }
            else if (isDecrafting)
            {
                sb.Append("微光分解: ");
            }
            else
            {
                sb.Append("微光转化: ");
            }

            int addedCount = 0;
            for (int i = 0; i < results.Count; i++)
            {
                Item res = results[i];
                if (res == null || res.IsAir || res.type <= ItemID.None) continue;

                sb.Append("[i");
                if (res.stack > 1)
                {
                    sb.Append($"/s{res.stack}");
                }
                sb.Append($":{res.type}]");
                addedCount++;
            }

            if (addedCount == 0) return false;

            if (isLocked && !string.IsNullOrEmpty(lockReason))
            {
                sb.Append($" [c/E5C158:({lockReason})]");
            }

            tooltipText = sb.ToString();
            return true;
        }

        private static Item CreateItem(int type)
        {
            Item it = new Item();
            it.SetDefaults(type);
            return it;
        }
    }
}
