using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace TPML.Content
{
    /// <summary>
    /// 原版 Recipe 实例便捷扩展方法（对齐 tML 常用配方操作）
    /// 作者: SaintCirno9
    /// </summary>
    public static class RecipeExtensions
    {
        public static bool HasIngredient(this Recipe recipe, int itemID)
        {
            if (recipe?.requiredItem == null) return false;
            return recipe.requiredItem.Any(i => i != null && i.type == itemID && i.stack > 0);
        }

        public static bool HasIngredient(this Recipe recipe, ModItem item)
        {
            return item != null && HasIngredient(recipe, item.Type);
        }

        public static bool HasResult(this Recipe recipe, int itemID)
        {
            return recipe?.createItem != null && recipe.createItem.type == itemID;
        }

        public static bool HasResult(this Recipe recipe, ModItem item)
        {
            return item != null && HasResult(recipe, item.Type);
        }

        public static bool TryGetIngredient(this Recipe recipe, int itemID, out Item item)
        {
            item = null;
            if (recipe?.requiredItem == null) return false;
            foreach (var req in recipe.requiredItem)
            {
                if (req != null && req.type == itemID && req.stack > 0)
                {
                    item = req;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetIngredient(this Recipe recipe, ModItem modItem, out Item item)
        {
            item = null;
            return modItem != null && TryGetIngredient(recipe, modItem.Type, out item);
        }

        public static void RemoveIngredient(this Recipe recipe, int itemID)
        {
            if (recipe?.requiredItem == null) return;
            for (int i = 0; i < recipe.requiredItem.Length; i++)
            {
                if (recipe.requiredItem[i] != null && recipe.requiredItem[i].type == itemID)
                {
                    recipe.requiredItem[i] = new Item();
                }
            }
        }

        public static void RemoveIngredient(this Recipe recipe, Item item)
        {
            if (recipe?.requiredItem == null || item == null) return;
            for (int i = 0; i < recipe.requiredItem.Length; i++)
            {
                if (recipe.requiredItem[i] == item || (recipe.requiredItem[i] != null && recipe.requiredItem[i].type == item.type))
                {
                    recipe.requiredItem[i] = new Item();
                }
            }
        }

        public static void RemoveIngredient(this Recipe recipe, ModItem modItem)
        {
            if (modItem != null)
            {
                RemoveIngredient(recipe, modItem.Type);
            }
        }

        public static void SetResult(this Recipe recipe, int itemID, int stack = 1)
        {
            if (recipe == null) return;
            recipe.createItem.SetDefaults(itemID);
            recipe.createItem.stack = stack;
        }

        public static void SetResult(this Recipe recipe, ModItem item, int stack = 1)
        {
            if (item != null)
            {
                SetResult(recipe, item.Type, stack);
            }
        }

        public static Recipe AddIngredient(this Recipe recipe, int itemID, int stack = 1)
        {
            if (recipe == null) return null;
            if (recipe.requiredItem != null)
            {
                for (int i = 0; i < recipe.requiredItem.Length; i++)
                {
                    if (recipe.requiredItem[i] == null || recipe.requiredItem[i].IsAir || recipe.requiredItem[i].type == 0)
                    {
                        recipe.requiredItem[i] = new Item();
                        recipe.requiredItem[i].SetDefaults(itemID);
                        recipe.requiredItem[i].stack = stack;
                        return recipe;
                    }
                }
                int oldLen = recipe.requiredItem.Length;
                Array.Resize(ref recipe.requiredItem, oldLen + 5);
                for (int i = oldLen; i < recipe.requiredItem.Length; i++)
                {
                    recipe.requiredItem[i] = new Item();
                }
                recipe.requiredItem[oldLen].SetDefaults(itemID);
                recipe.requiredItem[oldLen].stack = stack;
            }
            return recipe;
        }

        public static Recipe AddIngredient(this Recipe recipe, ModItem item, int stack = 1)
        {
            return item != null ? AddIngredient(recipe, item.Type, stack) : recipe;
        }

        public static Recipe AddIngredient<T>(this Recipe recipe, int stack = 1) where T : ModItem
        {
            return AddIngredient(recipe, ModContent.ItemType<T>(), stack);
        }

        public static Recipe AddIngredient(this Recipe recipe, Mod mod, string itemName, int stack = 1)
        {
            string modName = mod?.Name ?? "Fargowiltas";
            if (ModContent.TryFind<ModItem>(modName, itemName, out var item))
            {
                return AddIngredient(recipe, item.Type, stack);
            }
            return recipe;
        }

        public static Recipe AddIngredient(this Recipe recipe, string modName, string itemName, int stack = 1)
        {
            if (ModContent.TryFind<ModItem>(modName, itemName, out var item))
            {
                return AddIngredient(recipe, item.Type, stack);
            }
            return recipe;
        }

        public static Recipe AddTile(this Recipe recipe, int tileID)
        {
            if (recipe != null)
            {
                recipe.requiredTile = tileID;
                if (tileID >= 0 && tileID < Recipe.TileUsedInRecipes.Length)
                {
                    Recipe.TileUsedInRecipes[tileID] = true;
                }
            }
            return recipe;
        }

        public static Recipe AddTile(this Recipe recipe, ModTile tile)
        {
            return tile != null ? AddTile(recipe, tile.Type) : recipe;
        }

        public static Recipe AddTile<T>(this Recipe recipe) where T : ModTile
        {
            return AddTile(recipe, ModContent.TileType<T>());
        }

        public static Recipe AddTile(this Recipe recipe, Mod mod, string tileName)
        {
            string modName = mod?.Name ?? "Fargowiltas";
            if (ModContent.TryFind<ModTile>(modName, tileName, out var tile))
            {
                return AddTile(recipe, tile.Type);
            }
            return recipe;
        }

        public static Recipe AddRecipeGroup(this Recipe recipe, int groupId, int stack = 1)
        {
            if (recipe == null) return null;
            if (RecipeGroup.recipeGroups.TryGetValue(groupId, out RecipeGroup group) && group != null)
            {
                recipe.RequireGroup(group);
                int displayItemId = group.Items.Count > 0 ? group.Items[0] : 0;
                AddIngredient(recipe, displayItemId, stack);
            }
            return recipe;
        }

        public static Recipe AddRecipeGroup(this Recipe recipe, string name, int stack = 1)
        {
            if (RecipeLoader.TryGetRecipeGroup(name, out var group) && group != null)
            {
                return AddRecipeGroup(recipe, group.RegisteredId, stack);
            }
            return recipe;
        }

        public static Recipe AddRecipeGroup(this Recipe recipe, RecipeGroup group, int stack = 1)
        {
            if (group != null)
            {
                if (group.RegisteredId < 0)
                {
                    try { group.Register(); } catch { }
                }
                return AddRecipeGroup(recipe, group.RegisteredId, stack);
            }
            return recipe;
        }

        public static Recipe AddCondition(this Recipe recipe, Condition condition)
        {
            if (recipe != null && condition != null)
            {
                if (RecipeLoader.TryGetModRecipe(recipe, out var modRecipe))
                {
                    modRecipe.AddCondition(condition);
                }
            }
            return recipe;
        }

        public static Recipe AddCondition(this Recipe recipe, Func<bool> predicate)
        {
            return AddCondition(recipe, new Condition(string.Empty, predicate));
        }

        public static Recipe AddCondition(this Recipe recipe, string description, Func<bool> predicate)
        {
            return AddCondition(recipe, new Condition(description, predicate));
        }

        public static Recipe AddCondition(this Recipe recipe, LocalizedText description, Func<bool> predicate)
        {
            return AddCondition(recipe, new Condition(description, predicate));
        }

        public static Recipe DisableDecraft(this Recipe recipe)
        {
            if (recipe != null)
            {
                recipe.notDecraftable = true;
            }
            return recipe;
        }

        public static void AddRecipe(this Recipe recipe)
        {
            if (recipe != null)
            {
                RecipeLoader.Register(ModRecipe.Create(recipe.createItem.type, recipe.createItem.stack));
            }
        }
    }
}
