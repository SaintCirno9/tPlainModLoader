using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TPML.Content;
using TPML.Content.IO;

namespace RecipeBrowser
{
    /// <summary>
    /// 配方数据序列化与匹配还原器
    /// 作者: SaintCirno9
    /// </summary>
    internal static class RecipeIO
    {
        public static TagCompound SaveItem(Item item)
        {
            var tag = new TagCompound();
            if (item == null || item.IsAir)
            {
                tag["type"] = 0;
                return tag;
            }

            tag["type"] = item.type;
            var modItem = ItemLoader.GetModItem(item);
            if (modItem != null)
            {
                tag["mod"] = modItem.Mod?.Name ?? "TPML";
                tag["name"] = modItem.Name;
            }
            return tag;
        }

        public static int LoadItemType(TagCompound tag)
        {
            if (tag == null) return 0;
            if (tag.ContainsKey("mod") && tag.ContainsKey("name"))
            {
                string mod = tag.GetString("mod");
                string name = tag.GetString("name");
                int modType = ItemLoader.ItemType(mod, name);
                if (modType > 0) return modType;
            }

            if (tag.ContainsKey("type"))
            {
                return tag.GetInt("type");
            }

            return 0;
        }

        public static TagCompound Save(Recipe recipe)
        {
            if (recipe == null) return new TagCompound();

            var reqList = new List<TagCompound>();
            if (recipe.requiredItem != null)
            {
                foreach (var item in recipe.requiredItem)
                {
                    if (item != null && !item.IsAir)
                    {
                        reqList.Add(SaveItem(item));
                    }
                }
            }

            return new TagCompound
            {
                ["createItem"] = SaveItem(recipe.createItem),
                ["requiredItem"] = reqList
            };
        }

        public static int Load(TagCompound tag)
        {
            if (tag == null || !tag.ContainsKey("createItem")) return -1;

            TagCompound createTag = tag.Get<TagCompound>("createItem");
            int createType = LoadItemType(createTag);
            if (createType <= 0) return -1;

            var reqTags = tag.GetList<TagCompound>("requiredItem");
            var reqTypes = new HashSet<int>();
            if (reqTags != null)
            {
                foreach (var reqTag in reqTags)
                {
                    int t = LoadItemType(reqTag);
                    if (t > 0) reqTypes.Add(t);
                }
            }

            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe rec = Main.recipe[i];
                if (rec == null || rec.createItem == null) continue;

                if (rec.createItem.type == createType)
                {
                    var actualReqTypes = new HashSet<int>();
                    if (rec.requiredItem != null)
                    {
                        foreach (var it in rec.requiredItem)
                        {
                            if (it != null && !it.IsAir) actualReqTypes.Add(it.type);
                        }
                    }

                    if (reqTypes.SetEquals(actualReqTypes))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
