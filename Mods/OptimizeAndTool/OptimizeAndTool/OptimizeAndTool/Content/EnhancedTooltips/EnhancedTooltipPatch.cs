using Microsoft.Xna.Framework;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace OptimizeAndTool.Content.EnhancedTooltips
{
    /// <summary>
    /// 增强提示与随身饰品袋套装激活点亮渲染管道
    /// 作者: SaintCirno9
    /// </summary>
    public class EnhancedTooltipPatch : PatchMain
    {
        public static readonly Color AmmoColor = new Color(60, 160, 90);
        public static readonly Color DataColor = Color.Gray;

        public override void MouseText_DrawItemTooltip_GetLinesInfoPostfix(Item item, ref int yoyoLogo, ref float oldKB, ref int numLines, ref string[] toolTipLine, ref Color[] lineColors)
        {
            if (item == null || item.IsAir || item.type <= ItemID.None) return;

            // 1. 随身饰品袋：防具套装满足时提示点亮为激活状态
            if (AccessoryBagConfig.EnablePassive.val &&
                AccessoryBagConfig.EnableArmorSetBonuses.val &&
                AccessoryBagConfig.HighlightActiveSetBonusTooltips.val)
            {
                ApplySetBonusHighlight(item, ref numLines, toolTipLine, lineColors);
            }

            // 2. 微光转化提示
            if (EnhancedTooltipConfig.ShowShimmerInfo.val)
            {
                ApplyShimmerTooltip(item, ref numLines, toolTipLine, lineColors);
            }

            // 3. 弹药类型与消耗提示
            if (EnhancedTooltipConfig.ShowAmmoInfo.val)
            {
                ApplyAmmoTooltip(item, ref numLines, toolTipLine, lineColors);
            }

            // 4. 物品底层数据提示
            if (EnhancedTooltipConfig.ShowMoreDataInfo.val)
            {
                ApplyMoreDataTooltip(item, ref numLines, toolTipLine, lineColors);
            }
        }

        private static void ApplySetBonusHighlight(Item item, ref int numLines, string[] toolTipLine, Color[] lineColors)
        {
            var sets = ArmorSetBonuses.SetsContaining != null && item.type < ArmorSetBonuses.SetsContaining.Length
                ? ArmorSetBonuses.SetsContaining[item.type]
                : null;

            if (sets == null || sets.Length == 0) return;

            Player player = Main.LocalPlayer;
            if (player == null) return;

            // 收集全身装备与饰品袋中的所有可用防具类型
            var availableArmorTypes = new HashSet<int>();
            if (player.armor != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (player.armor[i] != null && !player.armor[i].IsAir)
                    {
                        availableArmorTypes.Add(player.armor[i].type);
                    }
                }
            }

            var bags = AccessoryBagCacheManager.GetAllBags();
            if (bags != null)
            {
                for (int b = 0; b < bags.Count; b++)
                {
                    var bag = bags[b];
                    if (bag?.personalInventory == null) continue;

                    int limit = bag.personalInventory.Length;
                    if (AccessoryBagConfig.EnableEffectiveSlotsLimit.val)
                    {
                        limit = Math.Min(limit, AccessoryBagConfig.EffectiveSlots.val);
                    }

                    for (int i = 0; i < limit; i++)
                    {
                        Item it = bag.personalInventory[i];
                        if (it != null && !it.IsAir && it.type > ItemID.None)
                        {
                            availableArmorTypes.Add(it.type);
                        }
                    }
                }
            }

            ArmorSetBonus satisfiedSet = null;
            for (int s = 0; s < sets.Length; s++)
            {
                var set = sets[s];
                if (set == null) continue;

                bool headOk = set.Head == 0 || availableArmorTypes.Contains(set.Head);
                bool bodyOk = set.Body == 0 || availableArmorTypes.Contains(set.Body);
                bool legsOk = set.Legs == 0 || availableArmorTypes.Contains(set.Legs);

                if (headOk && bodyOk && legsOk)
                {
                    satisfiedSet = set;
                    break;
                }
            }

            if (satisfiedSet == null) return;

            // 构造激活态的 QueryResult 与 QueryContext
            int needed = (satisfiedSet.Head != 0 ? 1 : 0) + (satisfiedSet.Body != 0 ? 1 : 0) + (satisfiedSet.Legs != 0 ? 1 : 0);
            var queryResult = new ArmorSetBonus.QueryResult
            {
                ItemsNeeded = needed,
                ItemsFound = needed
            };

            var context = new ArmorSetBonus.QueryContext(player)
            {
                HeadItem = satisfiedSet.Head != 0 ? satisfiedSet.Head : 0,
                BodyItem = satisfiedSet.Body != 0 ? satisfiedSet.Body : 0,
                LegItem = satisfiedSet.Legs != 0 ? satisfiedSet.Legs : 0
            };

            string activeTooltip = satisfiedSet.GetTooltipForWornArmor(context, queryResult);
            string singleTooltip = satisfiedSet.GetTooltipForSinglePiece(item.type);

            // 查找原版生成的单件套装行并替换为已激活样式
            bool replaced = false;
            for (int i = 0; i < numLines; i++)
            {
                if (toolTipLine[i] == singleTooltip ||
                    (!string.IsNullOrEmpty(toolTipLine[i]) &&
                     (toolTipLine[i].StartsWith("套装奖励") || toolTipLine[i].StartsWith("已装备套装奖励") ||
                      toolTipLine[i].StartsWith("Set Bonus") || toolTipLine[i].StartsWith("Equipped Set Bonus") ||
                      (!string.IsNullOrEmpty(satisfiedSet.Description?.Value) && toolTipLine[i].Contains(satisfiedSet.Description.Value)))))
                {
                    toolTipLine[i] = activeTooltip;
                    lineColors[i] = Color.LimeGreen;
                    replaced = true;
                    break;
                }
            }

            // 若未找到对应行，追加激活提示行
            if (!replaced && numLines < toolTipLine.Length)
            {
                toolTipLine[numLines] = activeTooltip;
                lineColors[numLines] = Color.LimeGreen;
                numLines++;
            }
        }

        private static void ApplyShimmerTooltip(Item item, ref int numLines, string[] toolTipLine, Color[] lineColors)
        {
            if (numLines >= toolTipLine.Length) return;

            if (ShimmerHelper.TryGetShimmerTooltip(item, out string shimmerText))
            {
                toolTipLine[numLines] = shimmerText;
                lineColors[numLines] = ShimmerHelper.ShimmerColor;
                numLines++;
            }
        }

        private static void ApplyAmmoTooltip(Item item, ref int numLines, string[] toolTipLine, Color[] lineColors)
        {
            if (item.useAmmo > 0 && numLines < toolTipLine.Length)
            {
                toolTipLine[numLines] = $"使用弹药: [i:{item.useAmmo}] {Lang.GetItemNameValue(item.useAmmo)}";
                lineColors[numLines] = AmmoColor;
                numLines++;
            }

            if (item.ammo > 0 && !item.notAmmo && numLines < toolTipLine.Length)
            {
                toolTipLine[numLines] = $"弹药类型: [i:{item.ammo}] {Lang.GetItemNameValue(item.ammo)}";
                lineColors[numLines] = AmmoColor;
                numLines++;
            }
        }

        private static void ApplyMoreDataTooltip(Item item, ref int numLines, string[] toolTipLine, Color[] lineColors)
        {
            if (numLines < toolTipLine.Length)
            {
                toolTipLine[numLines] = $"[物品数据] ID: {item.type} | 内部名: {ItemID.Search.GetName(item.type)}";
                lineColors[numLines] = DataColor;
                numLines++;
            }

            if (item.damage > 0 && numLines < toolTipLine.Length)
            {
                toolTipLine[numLines] = $"[攻击参数] 基础伤害: {item.damage} | 击退: {item.knockBack} | 使用时间: {item.useTime} | 动画: {item.useAnimation}";
                lineColors[numLines] = DataColor;
                numLines++;
            }

            if (item.shoot > 0 && numLines < toolTipLine.Length)
            {
                toolTipLine[numLines] = $"[弹幕射击] 弹幕ID: {item.shoot} | 弹幕速度: {item.shootSpeed}";
                lineColors[numLines] = DataColor;
                numLines++;
            }

            if ((item.createTile > -1 || item.createWall > -1) && numLines < toolTipLine.Length)
            {
                string tileInfo = item.createTile > -1 ? $"图格: {item.createTile}" : "";
                string wallInfo = item.createWall > -1 ? $"墙壁: {item.createWall}" : "";
                toolTipLine[numLines] = $"[放置属性] {tileInfo} {wallInfo}".Trim();
                lineColors[numLines] = DataColor;
                numLines++;
            }
        }
    }
}
