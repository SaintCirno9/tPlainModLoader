using System;
using Terraria.ID;
using Terraria.ModLoader.Engine;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 风格配方流式构建器
    /// </summary>
    public class ModRecipe
    {
        public Recipe Recipe { get; }
        public Mod Mod { get; }

        public ModRecipe(Mod mod = null)
        {
            Mod = mod;
            Recipe = new Recipe();
        }
        public ModRecipe Create(int itemType, int amount = 1)
        {
            Recipe.createItem.SetDefaults(itemType);
            Recipe.createItem.type = itemType;
            Recipe.createItem.stack = amount;
            return this;
        }

        public ModRecipe AddIngredient(int itemID, int stack = 1)
        {
            for (int i = 0; i < Recipe.maxRequirements; i++)
            {
                if (Recipe.requiredItem[i] == null) Recipe.requiredItem[i] = new Item();
                if (Recipe.requiredItem[i].IsAir || Recipe.requiredItem[i].type == 0)
                {
                    Recipe.requiredItem[i].SetDefaults(itemID);
                    Recipe.requiredItem[i].type = itemID;
                    Recipe.requiredItem[i].stack = stack;
                    break;
                }
            }
            return this;
        }

        public ModRecipe AddTile(int tileID)
        {
            Recipe.requiredTile = tileID;
            return this;
        }

        public ModRecipe AddRecipeGroup(RecipeGroup group)
        {
            if (group != null)
            {
                Recipe.RequireGroup(group);
            }
            return this;
        }

        public ModRecipe AddRecipeGroup(string groupName)
        {
            if (!string.IsNullOrEmpty(groupName))
            {
                foreach (var kvp in RecipeGroup.recipeGroups)
                {
                    if (kvp.Value?.GetText != null && kvp.Value.GetText() == groupName)
                    {
                        Recipe.RequireGroup(kvp.Value);
                        break;
                    }
                }
            }
            return this;
        }

        public void Register()
        {
            try
            {
                // 1. 确保 Main.recipe 与 Main.availableRecipe 数组容量充裕
                int requiredCapacity = Recipe.numRecipes + 100;
                if (Main.recipe == null || Main.recipe.Length < requiredCapacity)
                {
                    int newSize = Math.Max(5000, requiredCapacity + 500);
                    int oldLen = Main.recipe?.Length ?? 0;
                    Array.Resize(ref Main.recipe, newSize);
                    for (int i = oldLen; i < newSize; i++)
                    {
                        Main.recipe[i] = new Recipe();
                    }
                }

                if (Main.availableRecipe == null || Main.availableRecipe.Length < requiredCapacity)
                {
                    int newSize = Math.Max(5000, requiredCapacity + 500);
                    Array.Resize(ref Main.availableRecipe, newSize);
                }

                // 2. 确保材料数组与相关 ID 扩容
                ItemLoader.EnsureArraySizes(Recipe.createItem.type);

                // 3. 构建快速查询字典并标记材料属性
                for (int i = 0; i < Recipe.maxRequirements; i++)
                {
                    Item req = Recipe.requiredItem[i];
                    if (req != null && !req.IsAir && req.type > 0)
                    {
                        Recipe.requiredItemQuickLookup[i] = new Recipe.RequiredItemEntry(req.type, req.stack);
                        if (ItemID.Sets.IsAMaterial != null && req.type < ItemID.Sets.IsAMaterial.Length)
                        {
                            ItemID.Sets.IsAMaterial[req.type] = true;
                        }
                    }
                }

                if (Recipe.requiredTile >= 0 && Recipe.TileUsedInRecipes != null && Recipe.requiredTile < Recipe.TileUsedInRecipes.Length)
                {
                    Recipe.TileUsedInRecipes[Recipe.requiredTile] = true;
                }

                int assignedIndex = Recipe.numRecipes++;
                Main.recipe[assignedIndex] = Recipe;

                // 4. 确保 maxRecipes 始终与有效配方数量对齐
                Recipe.maxRecipes = Recipe.numRecipes;

                // 5. 刷新全局材料标记，保证向导与合成面板能准确索引
                try
                {
                    Recipe.UpdateWhichItemsAreMaterials();
                }
                catch { }

                string itemName = Lang.GetItemNameValue(Recipe.createItem.type);
                if (string.IsNullOrEmpty(itemName) || itemName.StartsWith("ModItem_"))
                {
                    itemName = ItemLoader.GetDisplayName(Recipe.createItem.type);
                }

                TModShimEngine.Log($"[RecipeLoader] ★ 成功注入配方 #{assignedIndex}: 产物 [{itemName}] (ID={Recipe.createItem.type}) x{Recipe.createItem.stack}, 当前配方总数={Recipe.numRecipes}");
            }
            catch (Exception ex)
            {
                TModShimEngine.Log($"[RecipeLoader] 注册配方异常: {ex}");
            }
        }
    }
}
