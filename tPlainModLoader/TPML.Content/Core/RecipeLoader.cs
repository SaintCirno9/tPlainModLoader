using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生配方构建与向导检索中心
    /// </summary>
    public static class RecipeLoader
    {
        private static readonly List<ModRecipe> _registeredModRecipes = new List<ModRecipe>();

        public static ModRecipe CreateRecipe(int resultType, int amount = 1)
        {
            var r = new ModRecipe();
            r.Create(resultType, amount);
            return r;
        }

        public static ModRecipe CreateRecipe(ModItem item, int amount = 1)
        {
            return CreateRecipe(item.Type, amount);
        }

        public static void SetupRecipes()
        {
            var log = TPML.Core.Logging.LogManager.GetLogger("RecipeLoader");
            try
            {
                foreach (var mod in ModContent.Mods)
                {
                    mod.AddRecipes();
                }

                foreach (var item in ModContent.GetContent<ModItem>())
                {
                    item.AddRecipes();
                }

                // 兜底确保所有注册配方已成功注入原版
                PostSetupRecipes();
                var modItems = ModContent.GetContent<ModItem>().ToList();
                log.Info($"SetupRecipes 完成: Mods={ModContent.Mods.Count}, ModItems={modItems.Count}, 注入配方={_registeredModRecipes.Count}");
            }
            catch (Exception ex)
            {
                log.Error("SetupRecipes 异常", ex);
                throw;
            }
        }

        public static void PostSetupRecipes()
        {
            foreach (var mr in _registeredModRecipes)
            {
                mr.InjectIntoVanilla();
            }
        }

        public static void Register(ModRecipe recipe)
        {
            if (!_registeredModRecipes.Contains(recipe))
            {
                _registeredModRecipes.Add(recipe);
                recipe.InjectIntoVanilla();
            }
        }

        public static void Clear()
        {
            _registeredModRecipes.Clear();
        }
    }

    public class ModRecipe
    {
        public int ResultType { get; private set; }
        public int ResultStack { get; private set; }
        public List<(int itemId, int stack)> RequiredItems { get; } = new List<(int, int)>();
        public List<int> RequiredTiles { get; } = new List<int>();

        public ModRecipe Create(int resultType, int amount = 1)
        {
            ResultType = resultType;
            ResultStack = amount;
            return this;
        }

        public ModRecipe AddIngredient(int itemID, int stack = 1)
        {
            RequiredItems.Add((itemID, stack));
            return this;
        }

        public ModRecipe AddIngredient(ModItem item, int stack = 1)
        {
            return AddIngredient(item.Type, stack);
        }

        public ModRecipe AddTile(int tileID)
        {
            RequiredTiles.Add(tileID);
            return this;
        }

        public void Register()
        {
            RecipeLoader.Register(this);
        }

        internal void InjectIntoVanilla()
        {
            if (ResultType <= 0) return;

            // 检查是否已经注入
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                var vRecipe = Main.recipe[i];
                if (vRecipe?.createItem?.type == ResultType && MatchIngredients(vRecipe))
                {
                    return; // 已存在相同配方
                }
            }

            if (Recipe.numRecipes >= Main.recipe.Length - 10)
            {
                Array.Resize(ref Main.recipe, Main.recipe.Length * 2);
            }

            Recipe recipe = new Recipe();
            recipe.createItem = new Item();
            recipe.createItem.SetDefaults(ResultType);
            // M2 兜底：SetDefaults 钩子若未生效，原版会把 mod ID(>=6200) 当越界清成 type=0，
            // 此时强制恢复 type 并走引擎 ItemLoader 派发（否则配方产物无效、向导/浏览器搜不到）
            if (recipe.createItem.type != ResultType || recipe.createItem.IsAir)
            {
                recipe.createItem.type = ResultType;
                recipe.createItem.stack = ResultStack > 0 ? ResultStack : 1;
                ItemLoader.SetDefaults(recipe.createItem);
            }
            recipe.createItem.stack = ResultStack > 0 ? ResultStack : 1;

            for (int i = 0; i < RequiredItems.Count && i < recipe.requiredItem.Length; i++)
            {
                var (itemId, stack) = RequiredItems[i];
                recipe.requiredItem[i] = new Item();
                recipe.requiredItem[i].SetDefaults(itemId);
                recipe.requiredItem[i].stack = stack;
                recipe.requiredItemQuickLookup[i] = new Recipe.RequiredItemEntry(itemId, stack);
            }

            if (RequiredTiles.Count > 0)
            {
                recipe.requiredTile = RequiredTiles[0];
                if (recipe.requiredTile >= 0 && recipe.requiredTile < Recipe.TileUsedInRecipes.Length)
                {
                    Recipe.TileUsedInRecipes[recipe.requiredTile] = true;
                }
            }

            Main.recipe[Recipe.numRecipes] = recipe;
            Recipe.numRecipes++;
        }

        private bool MatchIngredients(Recipe vRecipe)
        {
            if (vRecipe.requiredItem == null) return false;
            int count = 0;
            for (int i = 0; i < vRecipe.requiredItem.Length; i++)
            {
                if (vRecipe.requiredItem[i] != null && !vRecipe.requiredItem[i].IsAir)
                {
                    count++;
                }
            }
            if (count != RequiredItems.Count) return false;

            foreach (var (itemId, stack) in RequiredItems)
            {
                bool found = false;
                for (int i = 0; i < vRecipe.requiredItem.Length; i++)
                {
                    var req = vRecipe.requiredItem[i];
                    if (req != null && req.type == itemId && req.stack == stack)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }
    }
}
