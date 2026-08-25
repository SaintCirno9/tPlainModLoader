using System;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader.Engine;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader / TPML 配方加载与生命周期运行时中心
    /// </summary>
    public static class RecipeLoader
    {
        private static readonly HashSet<string> _registeredRecipeKeys = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<Type> _processedItemTypes = new HashSet<Type>();
        private static bool _recipesAdded = false;

        public static bool RecipesAdded => _recipesAdded;

        /// <summary>
        /// 全局生命周期入口：统筹执行配方注册与后处理
        /// </summary>
        public static void PostSetupRecipes()
        {
            AddRecipes();
            SetupRecipeLookups();
            _recipesAdded = true;
        }

        /// <summary>
        /// 遍历所有已注册物品，调用各物品的 AddRecipes()
        /// </summary>
        public static void AddRecipes()
        {
            int count = 0;
            foreach (var item in ItemLoader.Items)
            {
                if (item == null) continue;
                Type t = item.GetType();
                if (_processedItemTypes.Contains(t)) continue;

                try
                {
                    item.AddRecipes();
                    _processedItemTypes.Add(t);
                    count++;
                }
                catch (Exception ex)
                {
                    TModShimEngine.Log($"[RecipeLoader] 执行 {item.Name}.AddRecipes() 异常: {ex}");
                }
            }

            if (count > 0)
            {
                TModShimEngine.Log($"[RecipeLoader] 模组配方注册完成，本次处理 {count} 个物品。当前全局配方数: {Recipe.numRecipes}");
            }
        }

        /// <summary>
        /// 确保所有配方的材料与工作台标志更新完毕并刷新全局材料索引
        /// </summary>
        public static void SetupRecipeLookups()
        {
            try
            {
                // 确保 maxRecipes 与当前有效配方数对齐
                Recipe.maxRecipes = Recipe.numRecipes;

                // 遍历全局配方设置材料标记与工作台标记
                for (int rIdx = 0; rIdx < Recipe.numRecipes; rIdx++)
                {
                    Recipe r = Main.recipe[rIdx];
                    if (r == null || r.createItem == null || r.createItem.IsAir) continue;

                    for (int i = 0; i < Recipe.maxRequirements; i++)
                    {
                        Item req = r.requiredItem[i];
                        if (req != null && !req.IsAir && req.type > 0)
                        {
                            r.requiredItemQuickLookup[i] = new Recipe.RequiredItemEntry(req.type, req.stack);
                            if (ItemID.Sets.IsAMaterial != null && req.type < ItemID.Sets.IsAMaterial.Length)
                            {
                                ItemID.Sets.IsAMaterial[req.type] = true;
                            }
                        }
                    }

                    if (r.requiredTile >= 0 && Recipe.TileUsedInRecipes != null && r.requiredTile < Recipe.TileUsedInRecipes.Length)
                    {
                        Recipe.TileUsedInRecipes[r.requiredTile] = true;
                    }
                }

                // 刷新原版材料映射表，确保向导、合成表等能准确查询
                Recipe.UpdateWhichItemsAreMaterials();
                TModShimEngine.Log($"[RecipeLoader] ★ 已完成配方依赖关系与材料标记刷新 (numRecipes={Recipe.numRecipes})");
            }
            catch (Exception ex)
            {
                TModShimEngine.Log($"[RecipeLoader] SetupRecipeLookups 异常: {ex}");
            }
        }

        /// <summary>
        /// 确保 Main.recipe 与 Main.availableRecipe 数组容量
        /// </summary>
        public static void EnsureRecipeArraySizes(int minCapacity = -1)
        {
            int requiredCapacity = Math.Max(Recipe.numRecipes + 100, minCapacity);
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
        }

        /// <summary>
        /// 注册单个 Recipe 实例到原版配方系统
        /// </summary>
        public static void RegisterRecipe(Recipe recipe)
        {
            if (recipe == null || recipe.createItem == null || recipe.createItem.IsAir)
                return;

            try
            {
                string key = BuildRecipeKey(recipe);
                if (_registeredRecipeKeys.Contains(key))
                {
                    TModShimEngine.Log($"[RecipeLoader] 跳过重复配方: 产物 ID={recipe.createItem.type}");
                    return;
                }

                EnsureRecipeArraySizes(Recipe.numRecipes + 1);
                ItemLoader.EnsureArraySizes(recipe.createItem.type);

                for (int i = 0; i < Recipe.maxRequirements; i++)
                {
                    Item req = recipe.requiredItem[i];
                    if (req != null && !req.IsAir && req.type > 0)
                    {
                        recipe.requiredItemQuickLookup[i] = new Recipe.RequiredItemEntry(req.type, req.stack);
                        if (ItemID.Sets.IsAMaterial != null && req.type < ItemID.Sets.IsAMaterial.Length)
                        {
                            ItemID.Sets.IsAMaterial[req.type] = true;
                        }
                    }
                }

                if (recipe.requiredTile >= 0 && Recipe.TileUsedInRecipes != null && recipe.requiredTile < Recipe.TileUsedInRecipes.Length)
                {
                    Recipe.TileUsedInRecipes[recipe.requiredTile] = true;
                }

                int assignedIndex = Recipe.numRecipes++;
                Main.recipe[assignedIndex] = recipe;
                Recipe.maxRecipes = Recipe.numRecipes;

                _registeredRecipeKeys.Add(key);

                try
                {
                    Recipe.UpdateWhichItemsAreMaterials();
                }
                catch { }

                string itemName = ItemLoader.GetDisplayName(recipe.createItem.type);
                if (!string.IsNullOrEmpty(itemName) && !itemName.StartsWith("ModItem_"))
                {
                    recipe.createItem.SetNameOverride(itemName);
                }
                else
                {
                    itemName = Lang.GetItemNameValue(recipe.createItem.type);
                    if (string.IsNullOrEmpty(itemName)) itemName = $"ModItem_{recipe.createItem.type}";
                }

                TModShimEngine.Log($"[RecipeLoader] ★ 成功注入配方 #{assignedIndex}: 产物 [{itemName}] (ID={recipe.createItem.type}) x{recipe.createItem.stack}, 当前配方总数={Recipe.numRecipes}");
            }
            catch (Exception ex)
            {
                TModShimEngine.Log($"[RecipeLoader] RegisterRecipe 异常: {ex}");
            }
        }

        internal static bool IsRecipeRegistered(Recipe recipe)
        {
            return recipe != null && _registeredRecipeKeys.Contains(BuildRecipeKey(recipe));
        }

        internal static void MarkRecipeRegistered(Recipe recipe)
        {
            if (recipe != null)
                _registeredRecipeKeys.Add(BuildRecipeKey(recipe));
        }

        private static string BuildRecipeKey(Recipe recipe)
        {
            var parts = new List<string>
            {
                recipe.createItem?.type.ToString() ?? "0",
                recipe.createItem?.stack.ToString() ?? "0",
                recipe.requiredTile.ToString()
            };

            for (int i = 0; i < Recipe.maxRequirements; i++)
            {
                Item ingredient = recipe.requiredItem[i];
                if (ingredient != null && !ingredient.IsAir && ingredient.type > 0)
                    parts.Add($"{ingredient.type}:{ingredient.stack}");
            }

            return string.Join("|", parts);
        }

        public static void Clear()
        {
            _registeredRecipeKeys.Clear();
            _processedItemTypes.Clear();
            _recipesAdded = false;
        }
    }

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
            RecipeLoader.RegisterRecipe(Recipe);
        }
    }
}
