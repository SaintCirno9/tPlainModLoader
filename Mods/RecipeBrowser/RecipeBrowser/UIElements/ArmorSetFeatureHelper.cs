using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    /// <summary>
    /// 护甲套装聚合器（对齐原版 ArmorSetFeatureHelper 的结构与 UI 交互）
    /// 注：原版基于 tML 的 Player.setBonus（UpdateArmorSets 验证套装加成）；
    /// TPML 原版 Player 无 setBonus 字段，故按护甲名称前缀匹配 + 两件套组合近似（记录于 WALKTHROUGH）
    /// </summary>
    public static class ArmorSetFeatureHelper
    {
        public static string ArmorSetsInternalName = "Armor Sets";
        public static string ArmorSetsHoverTest => RBLanguage.GetText("RecipeCatalogueFilters", "ArmorSets");

        internal static List<Tuple<Item, Item, Item, string, int>> sets;
        internal static List<UIArmorSetCatalogueItemSlot> armorSetSlots;

        public static void Unload()
        {
            sets = null;
            armorSetSlots = null;
            UIArmorSetCatalogueItemSlot.drawPlayer = null;
        }

        internal static string RBText(string key)
        {
            return RBLanguage.GetText("UIArmorSetCatalogue", key);
        }

        /// <summary>
        /// 套装控制面板（显示物品/染色/动画/饰品 4 复选框，对齐原版 AppendSpecialUI）
        /// </summary>
        internal static void AppendSpecialUI(UIGrid itemGrid)
        {
            if (itemGrid == null) return;
            UIPanel panel = new UIPanel();
            panel.Width.Set(162f, 0f);
            panel.Height.Set(100f, 0f);
            panel.SetPadding(12f);

            UICheckbox showItemsCheckbox = new UICheckbox(RBText("ShowItems"), RBText("ShowItemsTooltip"));
            showItemsCheckbox.Selected = UIArmorSetCatalogueItemSlot.showItems;
            showItemsCheckbox.OnSelectedChanged += (s, e) =>
            {
                UIArmorSetCatalogueItemSlot.showItems = showItemsCheckbox.Selected;
                if (armorSetSlots != null)
                {
                    foreach (var slot in armorSetSlots) slot.needsUpdate = true;
                }
            };
            showItemsCheckbox.Left.Set(0f, 0f);
            panel.Append(showItemsCheckbox);

            UICheckbox useDyeCheckbox = new UICheckbox(RBText("UseDye"), RBText("UseDyeTooltip"));
            useDyeCheckbox.Selected = UIArmorSetCatalogueItemSlot.useDye;
            useDyeCheckbox.OnSelectedChanged += (s, e) => UIArmorSetCatalogueItemSlot.useDye = useDyeCheckbox.Selected;
            useDyeCheckbox.Top.Set(20f, 0f);
            useDyeCheckbox.Left.Set(0f, 0f);
            panel.Append(useDyeCheckbox);

            UICheckbox animateCheckbox = new UICheckbox(RBText("Animate"), RBText("AnimateTooltip"));
            animateCheckbox.Selected = UIArmorSetCatalogueItemSlot.animate;
            animateCheckbox.OnSelectedChanged += (s, e) => UIArmorSetCatalogueItemSlot.animate = animateCheckbox.Selected;
            animateCheckbox.Top.Set(40f, 0f);
            animateCheckbox.Left.Set(0f, 0f);
            panel.Append(animateCheckbox);

            UICheckbox accessoriesCheckbox = new UICheckbox(RBText("Accessories"), RBText("AccessoriesTooltip"));
            accessoriesCheckbox.Selected = UIArmorSetCatalogueItemSlot.accessories;
            accessoriesCheckbox.OnSelectedChanged += (s, e) => UIArmorSetCatalogueItemSlot.accessories = accessoriesCheckbox.Selected;
            accessoriesCheckbox.Top.Set(60f, 0f);
            accessoriesCheckbox.Left.Set(0f, 0f);
            panel.Append(accessoriesCheckbox);

            itemGrid._items.Add(panel);
            itemGrid._innerList.Append(panel);
        }

        /// <summary>
        /// 计算护甲套装（三件套 + 两件套），返回套件元组列表并构建 armorSetSlots
        /// </summary>
        public static List<Tuple<Item, Item, Item, string, int>> GetArmorSets()
        {
            if (sets != null) return sets;

            sets = new List<Tuple<Item, Item, Item, string, int>>();
            List<Item> headList = new List<Item>();
            List<Item> bodyList = new List<Item>();
            List<Item> legList = new List<Item>();

            int maxId = Math.Max((int)ItemID.Count, TPML.Content.ItemLoader.NextItemID);
            for (int i = 1; i < maxId; i++)
            {
                if (!ContentSamples.ItemsByType.TryGetValue(i, out Item item) || item == null || item.vanity) continue;
                if (item.headSlot >= 0) headList.Add(item);
                if (item.bodySlot >= 0) bodyList.Add(item);
                if (item.legSlot >= 0) legList.Add(item);
            }

            HashSet<string> addedKeys = new HashSet<string>();

            // 三件套：头+身+腿 同前缀
            foreach (Item head in headList)
            {
                string prefix = GetArmorPrefix(head.Name);
                if (string.IsNullOrEmpty(prefix)) continue;

                Item body = bodyList.FirstOrDefault(b => GetArmorPrefix(b.Name) == prefix);
                Item legs = legList.FirstOrDefault(l => GetArmorPrefix(l.Name) == prefix);
                if (body != null && legs != null)
                {
                    int totalDef = head.defense + body.defense + legs.defense;
                    AddSet(head, body, legs, $"{prefix} Set", totalDef, addedKeys);
                }
            }

            // 两件套：身+腿（无头）
            foreach (Item body in bodyList)
            {
                string prefix = GetArmorPrefix(body.Name);
                if (string.IsNullOrEmpty(prefix)) continue;
                Item legs = legList.FirstOrDefault(l => GetArmorPrefix(l.Name) == prefix);
                if (legs != null && !headList.Any(h => GetArmorPrefix(h.Name) == prefix))
                {
                    AddSet(null, body, legs, $"{prefix} Set", body.defense + legs.defense, addedKeys);
                }
            }

            // 两件套：头+身（无腿）
            foreach (Item head in headList)
            {
                string prefix = GetArmorPrefix(head.Name);
                if (string.IsNullOrEmpty(prefix)) continue;
                Item body = bodyList.FirstOrDefault(b => GetArmorPrefix(b.Name) == prefix);
                if (body != null && !legList.Any(l => GetArmorPrefix(l.Name) == prefix))
                {
                    AddSet(head, body, null, $"{prefix} Set", head.defense + body.defense, addedKeys);
                }
            }

            // 两件套：头+腿（无身）
            foreach (Item head in headList)
            {
                string prefix = GetArmorPrefix(head.Name);
                if (string.IsNullOrEmpty(prefix)) continue;
                Item legs = legList.FirstOrDefault(l => GetArmorPrefix(l.Name) == prefix);
                if (legs != null && !bodyList.Any(b => GetArmorPrefix(b.Name) == prefix))
                {
                    AddSet(head, null, legs, $"{prefix} Set", head.defense + legs.defense, addedKeys);
                }
            }

            armorSetSlots = new List<UIArmorSetCatalogueItemSlot>();
            foreach (var set in sets)
            {
                armorSetSlots.Add(new UIArmorSetCatalogueItemSlot(set));
            }

            return sets;
        }

        private static void AddSet(Item head, Item body, Item legs, string bonus, int totalDef, HashSet<string> addedKeys)
        {
            string key = $"{head?.type ?? -1}|{body?.type ?? -1}|{legs?.type ?? -1}";
            if (addedKeys.Contains(key)) return;
            addedKeys.Add(key);
            sets.Add(Tuple.Create(head, body, legs, bonus, totalDef));
        }

        private static string GetArmorPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string[] words = name.Split(' ');
            if (words.Length > 1)
            {
                return string.Join(" ", words.Take(words.Length - 1));
            }
            return words[0];
        }
    }
}
