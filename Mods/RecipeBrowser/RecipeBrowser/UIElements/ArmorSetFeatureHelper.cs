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
    public static class ArmorSetFeatureHelper
    {
        public static string ArmorSetsInternalName = "ArmorSets";
        public static string ArmorSetsHoverTest => RBLanguage.GetText("RecipeCatalogueFilters", "ArmorSets");

        private static List<Tuple<Item, Item, Item, string, int>> sets;

        public static void Unload()
        {
            sets = null;
        }

        public static List<Tuple<Item, Item, Item, string, int>> GetArmorSets()
        {
            if (sets != null) return sets;

            sets = new List<Tuple<Item, Item, Item, string, int>>();
            List<Item> headList = new List<Item>();
            List<Item> bodyList = new List<Item>();
            List<Item> legList = new List<Item>();

            for (int i = 1; i < ItemID.Count; i++)
            {
                Item item = ContentSamples.ItemsByType[i];
                if (item == null || item.vanity) continue;
                if (item.headSlot >= 0) headList.Add(item);
                if (item.bodySlot >= 0) bodyList.Add(item);
                if (item.legSlot >= 0) legList.Add(item);
            }

            // 按护甲槽位前缀自动匹配套装
            foreach (Item head in headList)
            {
                string headPrefix = GetArmorPrefix(head.Name);
                if (string.IsNullOrEmpty(headPrefix)) continue;

                Item matchedBody = bodyList.FirstOrDefault(b => GetArmorPrefix(b.Name) == headPrefix);
                Item matchedLeg = legList.FirstOrDefault(l => GetArmorPrefix(l.Name) == headPrefix);

                if (matchedBody != null && matchedLeg != null)
                {
                    int totalDef = head.defense + matchedBody.defense + matchedLeg.defense;
                    string bonus = $"{headPrefix} Set";
                    var tuple = Tuple.Create(head, matchedBody, matchedLeg, bonus, totalDef);
                    if (!sets.Contains(tuple))
                    {
                        sets.Add(tuple);
                    }
                }
            }

            return sets;
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
