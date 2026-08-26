using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RecipeBrowser;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.ID;
using TPML.Content;
using TPML.Content.Fusion;
using TPMLBridge.GABP;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// RecipeBrowser (合成表与物品图鉴) 自动化测试与诊断 GABP 工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class RecipeBrowserTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_get_state",
                    Description = "诊断 RecipeBrowser 整体运行状态（主面板开关、当前 Tab、收藏夹面板、查询槽物品、过滤配方数、已见制作站数量与窗口位置）。",
                    Tags = new List<string> { "read-only", "ui", "diagnostic" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_toggle",
                    Description = "打开或关闭 RecipeBrowser 主面板或收藏夹面板（可传入显式布尔值，或不传参数进行切换）。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            open = new { type = "boolean", description = "是否打开主面板（不传则切换）" },
                            openFavoritePanel = new { type = "boolean", description = "是否打开收藏夹悬浮面板（可选）" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_set_tab",
                    Description = "切换 RecipeBrowser 主面板的当前激活标签页 (Tab)。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "tabIndex" },
                        properties = new
                        {
                            tabIndex = new { type = "integer", description = "标签页索引: 0=Recipes, 1=Craft, 2=Items, 3=Bestiary, 4=Help" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_set_filter",
                    Description = "向 RecipeBrowser 配方图鉴设置搜索与过滤条件（名称关键词、Tooltip 关键词、制作站 Tile 过滤、附近材料过滤等）。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            itemName = new { type = "string", description = "物品名称搜索关键词" },
                            itemTooltip = new { type = "string", description = "物品 Tooltip 搜索关键词" },
                            tile = new { type = "integer", description = "制作站 TileID 过滤 (-1 为全部制作站)" },
                            nearbyChestsOnly = new { type = "boolean", description = "是否只显示附近箱子/背包材料可合成的配方" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_query_item",
                    Description = "向 RecipeBrowser 查询槽位放置指定物品（模拟放入物品或清空），触发全面板过滤匹配。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "itemId" },
                        properties = new
                        {
                            itemId = new { type = "integer", description = "物品 ID (0 为清空查询槽)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_get_craft_path",
                    Description = "针对指定配方索引或物品 ID 执行深度多级合成树递归分析，返回完整材料分解与获取途径。",
                    Tags = new List<string> { "read-only", "logic" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            recipeIndex = new { type = "integer", description = "配方索引" },
                            itemId = new { type = "integer", description = "成品物品 ID (如果未传 recipeIndex 时使用)" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_favorite_recipe",
                    Description = "收藏或取消收藏指定配方索引，并触发 Sidecar 持久化快照更新。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "recipeIndex" },
                        properties = new
                        {
                            recipeIndex = new { type = "integer", description = "要收藏/取消收藏的配方索引" },
                            favorite = new { type = "boolean", description = "true 为收藏，false 为取消；不传则切换" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/recipe_browser_select_catalogue_item",
                    Description = "在物品目录 (Item Catalogue) 中选中指定物品，并自动同步至制作树 (CraftUI) 与掉落查看器。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "itemId" },
                        properties = new
                        {
                            itemId = new { type = "integer", description = "物品 ID" }
                        }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/recipe_browser_get_state":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        var ui = RecipeBrowserUI.instance;
                        var cat = RecipeCatalogueUI.instance;
                        var rbPlayer = (Main.LocalPlayer != null && Main.LocalPlayer.active) ? Main.LocalPlayer.GetModPlayer<RecipeBrowserPlayer>() : null;

                        var queryItem = cat?.queryItem?.item;
                        int seenTilesCount = 0;
                        if (RecipeBrowserPlayer.seenTiles != null)
                        {
                            seenTilesCount = RecipeBrowserPlayer.seenTiles.Count(x => x);
                        }

                        int filteredRecipes = cat?.recipeGrid?._items?.Count ?? 0;

                        return new
                        {
                            initialized = ui != null,
                            showRecipeBrowser = ui?.ShowRecipeBrowser ?? false,
                            currentPanel = ui?.CurrentPanel ?? -1,
                            showFavoritePanel = ui?.ShowFavoritePanel ?? false,
                            totalRecipes = Recipe.numRecipes,
                            filteredRecipesCount = filteredRecipes,
                            favoritedRecipes = RecipeBrowserPlayer.GetLocalFavoritedRecipes(),
                            seenTilesCount = seenTilesCount,
                            queryItem = queryItem != null && !queryItem.IsAir ? new
                            {
                                type = queryItem.type,
                                name = queryItem.Name,
                                stack = queryItem.stack
                            } : null,
                            itemNameFilter = cat?.itemNameFilter?.currentString ?? "",
                            tileFilter = cat?.Tile ?? -1,
                            nearbyChestsSelected = cat?.NearbyIngredientsRadioButton?.Selected ?? false,
                            config = new
                            {
                                position = RecipeBrowserClientConfig.Instance.RecipeBrowserPosition,
                                size = RecipeBrowserClientConfig.Instance.RecipeBrowserSize,
                                favoritedPosition = RecipeBrowserClientConfig.Instance.FavoritedRecipePanelPosition
                            }
                        };
                    });

                case "tpml/recipe_browser_toggle":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        var ui = RecipeBrowserUI.instance;
                        if (ui == null) return new { success = false, message = "RecipeBrowserUI 尚未初始化" };

                        if (args.TryGetValue("open", out var openToken))
                        {
                            ui.ShowRecipeBrowser = openToken.Value<bool>();
                        }
                        else
                        {
                            ui.ShowRecipeBrowser = !ui.ShowRecipeBrowser;
                        }

                        if (args.TryGetValue("openFavoritePanel", out var favToken))
                        {
                            ui.ShowFavoritePanel = favToken.Value<bool>();
                        }

                        return new
                        {
                            success = true,
                            showRecipeBrowser = ui.ShowRecipeBrowser,
                            showFavoritePanel = ui.ShowFavoritePanel
                        };
                    });

                case "tpml/recipe_browser_set_tab":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        var ui = RecipeBrowserUI.instance;
                        if (ui == null || ui.tabController == null)
                        {
                            return new { success = false, message = "RecipeBrowserUI 未初始化" };
                        }

                        int tabIndex = args.Value<int>("tabIndex");
                        ui.tabController.SetPanel(tabIndex);

                        if (tabIndex == 0 && RecipeCatalogueUI.instance != null)
                        {
                            RecipeCatalogueUI.instance.updateNeeded = true;
                        }
                        else if (tabIndex == 2 && ItemCatalogueUI.instance != null)
                        {
                            ItemCatalogueUI.instance.updateNeeded = true;
                        }
                        else if (tabIndex == 3 && BestiaryUI.instance != null)
                        {
                            BestiaryUI.instance.updateNeeded = true;
                        }

                        return new
                        {
                            success = true,
                            currentPanel = ui.CurrentPanel
                        };
                    });

                case "tpml/recipe_browser_set_filter":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        var cat = RecipeCatalogueUI.instance;
                        if (cat == null) return new { success = false, message = "RecipeCatalogueUI 未初始化" };

                        if (args.TryGetValue("itemName", out var nameToken))
                        {
                            cat.itemNameFilter?.SetText(nameToken.Value<string>() ?? "");
                        }

                        if (args.TryGetValue("itemTooltip", out var tipToken))
                        {
                            cat.itemDescriptionFilter?.SetText(tipToken.Value<string>() ?? "");
                        }

                        if (args.TryGetValue("tile", out var tileToken))
                        {
                            cat.Tile = tileToken.Value<int>();
                        }

                        if (args.TryGetValue("nearbyChestsOnly", out var nearbyToken))
                        {
                            bool nearby = nearbyToken.Value<bool>();
                            if (cat.NearbyIngredientsRadioButton != null)
                            {
                                cat.NearbyIngredientsRadioButton.Selected = nearby;
                            }
                        }

                        cat.updateNeeded = true;
                        cat.Update();

                        return new
                        {
                            success = true,
                            filteredRecipesCount = cat.recipeGrid?._items?.Count ?? 0,
                            itemName = cat.itemNameFilter?.currentString ?? "",
                            tile = cat.Tile,
                            nearbyChestsSelected = cat.NearbyIngredientsRadioButton?.Selected ?? false
                        };
                    });

                case "tpml/recipe_browser_query_item":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        var cat = RecipeCatalogueUI.instance;
                        if (cat == null || cat.queryItem == null)
                        {
                            return new { success = false, message = "RecipeCatalogueUI 未初始化" };
                        }

                        int itemId = args.Value<int>("itemId");
                        cat.queryItem.ReplaceWithFake(itemId);
                        cat.updateNeeded = true;
                        cat.Update();

                        return new
                        {
                            success = true,
                            queryItemId = itemId,
                            filteredRecipesCount = cat.recipeGrid?._items?.Count ?? 0
                        };
                    });

                case "tpml/recipe_browser_get_craft_path":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        int recipeIndex = -1;
                        if (args.TryGetValue("recipeIndex", out var rIdxToken))
                        {
                            recipeIndex = rIdxToken.Value<int>();
                        }
                        else if (args.TryGetValue("itemId", out var itemToken))
                        {
                            int itemId = itemToken.Value<int>();
                            for (int i = 0; i < Recipe.numRecipes; i++)
                            {
                                if (Main.recipe[i]?.createItem?.type == itemId)
                                {
                                    recipeIndex = i;
                                    break;
                                }
                            }
                        }

                        if (recipeIndex < 0 || recipeIndex >= Recipe.numRecipes)
                        {
                            return new { success = false, message = $"未找到有效配方 (recipeIndex: {recipeIndex})" };
                        }

                        Recipe targetRecipe = Main.recipe[recipeIndex];
                        bool prevMissingStations = RecipePath.allowMissingStations;
                        RecipePath.allowMissingStations = true;
                        List<CraftPath> paths = RecipePath.GetCraftPaths(targetRecipe, CancellationToken.None, single: false);
                        RecipePath.allowMissingStations = prevMissingStations;

                        var resultNodes = new List<object>();
                        if (paths != null && paths.Count > 0)
                        {
                            foreach (var p in paths)
                            {
                                var nodes = p.root.GetAllChildrenPreOrder().Select(n => new
                                {
                                    type = n.GetType().Name,
                                    text = n.ToUITextString(),
                                    multiplier = n.multiplier
                                }).ToList();
                                resultNodes.Add(nodes);
                            }
                        }

                        return new
                        {
                            success = true,
                            recipeIndex = recipeIndex,
                            createItem = targetRecipe.createItem.Name,
                            createItemStack = targetRecipe.createItem.stack,
                            pathCount = paths?.Count ?? 0,
                            paths = resultNodes
                        };
                    });

                case "tpml/recipe_browser_favorite_recipe":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        var ui = RecipeBrowserUI.instance;
                        var rbPlayer = (Main.LocalPlayer != null && Main.LocalPlayer.active) ? Main.LocalPlayer.GetModPlayer<RecipeBrowserPlayer>() : null;
                        if (ui == null || rbPlayer == null)
                        {
                            return new { success = false, message = "RecipeBrowser 或 LocalPlayer 未就绪" };
                        }

                        int recipeIndex = args.Value<int>("recipeIndex");
                        if (recipeIndex < 0 || recipeIndex >= Recipe.numRecipes)
                        {
                            return new { success = false, message = $"无效配方索引: {recipeIndex}" };
                        }

                        bool fav = !rbPlayer.favoritedRecipes.Contains(recipeIndex);
                        if (args.TryGetValue("favorite", out var fToken))
                        {
                            fav = fToken.Value<bool>();
                        }

                        ui.FavoriteChange(recipeIndex, fav);

                        return new
                        {
                            success = true,
                            recipeIndex = recipeIndex,
                            favorited = fav,
                            favoritedRecipes = rbPlayer.favoritedRecipes
                        };
                    });

                case "tpml/recipe_browser_select_catalogue_item":
                    return await MainThreadQueue.EnqueueAsync<object>(() =>
                    {
                        var itemCat = ItemCatalogueUI.instance;
                        var craft = CraftUI.instance;
                        if (itemCat == null || craft == null)
                        {
                            return new { success = false, message = "ItemCatalogueUI 或 CraftUI 未就绪" };
                        }

                        int itemId = args.Value<int>("itemId");
                        var slot = itemCat.itemSlots?.FirstOrDefault(s => s.itemType == itemId);
                        if (slot != null)
                        {
                            itemCat.SetItem(slot);
                        }
                        craft.SetItem(itemId);
                        itemCat.PopulateItemDropViewerPanel(itemId);

                        return new
                        {
                            success = true,
                            itemId = itemId,
                            selectedIndexesCount = craft.selectedIndexes.Count
                        };
                    });

                default:
                    throw new ArgumentException($"未知的 GABP 工具: {name}");
            }
        }
    }
}
