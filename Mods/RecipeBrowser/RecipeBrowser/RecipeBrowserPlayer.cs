using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using TPML.Content.IO;

namespace RecipeBrowser
{
    /// <summary>
    /// RecipeBrowser 玩家生命周期与数据绑定
    /// 自动接入 TPML Sidecar 持久化系统 (Player_*.tpml_data)
    /// 作者: SaintCirno9
    /// </summary>
    public class RecipeBrowserPlayer : ModPlayer
    {
        public List<int> favoritedRecipes = new List<int>();
        public static bool[] seenTiles;
        private bool[] _seenTiles;

        public static List<int> GetLocalFavoritedRecipes()
        {
            if (Main.LocalPlayer != null && Main.LocalPlayer.active)
            {
                var mp = Main.LocalPlayer.GetModPlayer<RecipeBrowserPlayer>();
                if (mp?.favoritedRecipes != null) return mp.favoritedRecipes;
            }
            return new List<int>();
        }

        public override void Initialize()
        {
            favoritedRecipes = new List<int>();
            _seenTiles = new bool[TileID.Count + 1000];
        }

        public override void SaveData(TagCompound tag)
        {
            if (favoritedRecipes != null && favoritedRecipes.Count > 0)
            {
                var list = new List<TagCompound>();
                foreach (int idx in favoritedRecipes)
                {
                    if (idx >= 0 && idx < Recipe.numRecipes && Main.recipe[idx] != null)
                    {
                        list.Add(RecipeIO.Save(Main.recipe[idx]));
                    }
                }
                tag["StarredRecipes"] = list;
            }

            if (_seenTiles != null)
            {
                var seenList = new List<int>();
                for (int i = 0; i < _seenTiles.Length; i++)
                {
                    if (_seenTiles[i]) seenList.Add(i);
                }
                tag["SeenTiles"] = seenList;
            }
        }

        public override void LoadData(TagCompound tag)
        {
            favoritedRecipes.Clear();
            if (tag.ContainsKey("StarredRecipes"))
            {
                var list = tag.GetList<TagCompound>("StarredRecipes");
                if (list != null)
                {
                    foreach (var t in list)
                    {
                        int r = RecipeIO.Load(t);
                        if (r >= 0 && !favoritedRecipes.Contains(r))
                        {
                            favoritedRecipes.Add(r);
                        }
                    }
                }
            }

            if (tag.ContainsKey("SeenTiles"))
            {
                var seenList = tag.GetList<int>("SeenTiles");
                if (seenList != null)
                {
                    foreach (int tile in seenList)
                    {
                        if (tile >= 0 && tile < _seenTiles.Length)
                        {
                            _seenTiles[tile] = true;
                        }
                    }
                }
            }
        }

        public override void OnEnterWorld(Player player)
        {
            seenTiles = _seenTiles;
            RecipePath.Refresh();
            RecipePath.PrepareGetCraftPaths();
            // Load 时 ItemDropsDB 可能尚未填充，进世界后重建掉落缓存
            LootCacheManager.Setup();
            LootCacheManager.itemDrops = null;

            // 背包扫描：登记已有物品对应的工作台为已见（对齐原版 OnEnterWorld 逐个 ItemReceived）
            if (RecipeBrowserUI.instance != null)
            {
                for (int i = 0; i < 58; i++)
                {
                    Item invItem = Player.inventory[i];
                    if (invItem != null && !invItem.IsAir)
                    {
                        RecipeBrowserUI.instance.ItemReceived(invItem);
                    }
                }
            }

            int centerTileX = (int)(Player.Center.X / 16f);
            int centerTileY = (int)(Player.Center.Y / 16f);

            for (int i = centerTileX - 100; i < centerTileX + 100; i++)
            {
                for (int j = centerTileY - 100; j < centerTileY + 100; j++)
                {
                    if (!WorldGen.InWorld(i, j, 0)) continue;
                    Tile tile = Main.tile[i, j];
                    if (tile != null && tile.active() && tile.type < seenTiles.Length)
                    {
                        if (seenTiles[tile.type]) continue;
                        foreach (int adj in Utilities.PopulateAdjTilesForTile(tile.type))
                        {
                            if (adj < seenTiles.Length) seenTiles[adj] = true;
                        }
                    }
                }
            }

            if (RecipeBrowserUI.instance != null)
            {
                RecipeBrowserUI.instance.favoritePanelUpdateNeeded = true;
                RecipeBrowserUI.instance.ShowFavoritePanel = favoritedRecipes.Count > 0 && RecipeBrowserUI.instance.HideUnlessInventoryToggle?.CurrentState == 0;
                if (RecipeCatalogueUI.instance != null)
                {
                    RecipeCatalogueUI.instance.updateNeeded = true;
                    if (RecipeCatalogueUI.instance.recipeSlots.Count > 0)
                    {
                        RecipeBrowserUI.instance.UpdateFavoritedPanel();
                    }
                }
                if (SharedUI.instance != null)
                {
                    SharedUI.instance.updateNeeded = true;
                }
            }
        }

        public override void PreUpdate()
        {
            if (Player == null || Player.whoAmI != Main.myPlayer || seenTiles == null) return;

            if (!Main.playerInventory && WorldGen.InWorld((int)(Player.position.X / 16f), (int)(Player.position.Y / 16f), 10))
            {
                Main.LocalPlayer.AdjTiles();
            }

            for (int i = 0; i < seenTiles.Length; i++)
            {
                if (i < Player.adjTile.Length && Player.adjTile[i] && !seenTiles[i])
                {
                    seenTiles[i] = true;
                    RecipeCatalogueUI.instance?.InvalidateExtendedCraft();
                }
            }
        }

        public override bool OnPickup(Item item)
        {
            // 拾取物品 → 实时登记已见工作台（对齐原版 RecipeBrowserGlobalItem.OnPickup 语义）
            if (item != null && Player != null && Player.whoAmI == Main.myPlayer)
            {
                RecipeBrowserUI.instance?.ItemReceived(item);
            }
            return true;
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (RecipeBrowserMod.ToggleRecipeBrowserHotKey?.JustPressed == true)
            {
                RecipeBrowserUI.instance.ShowRecipeBrowser = !RecipeBrowserUI.instance.ShowRecipeBrowser;
            }

            if (RecipeBrowserMod.QueryHoveredItemHotKey?.JustPressed == true && Main.HoverItem != null && !Main.HoverItem.IsAir)
            {
                bool showRecipeBrowser = true;
                if (RecipeBrowserUI.instance.CurrentPanel == 0)
                {
                    if (RecipeCatalogueUI.instance?.queryItem?.item?.type == Main.HoverItem.type)
                    {
                        showRecipeBrowser = !RecipeBrowserUI.instance.ShowRecipeBrowser;
                    }
                    else
                    {
                        RecipeCatalogueUI.instance?.queryItem?.ReplaceWithFake(Main.HoverItem.type);
                    }
                }
                else if (RecipeBrowserUI.instance.CurrentPanel == 1)
                {
                    if (CraftUI.instance?.recipeResultItemSlot?.item?.type == Main.HoverItem.type)
                    {
                        showRecipeBrowser = !RecipeBrowserUI.instance.ShowRecipeBrowser;
                    }
                    else
                    {
                        CraftUI.instance?.SetItem(Main.HoverItem.type);
                    }
                }
                else if (RecipeBrowserUI.instance.CurrentPanel == 2)
                {
                    ItemCatalogueUI.instance?.itemGrid?.Goto(element =>
                    {
                        if (element is UIItemCatalogueItemSlot slot && slot.itemType == Main.HoverItem.type)
                        {
                            ItemCatalogueUI.instance.SetItem(slot);
                            return true;
                        }
                        return false;
                    }, center: true);
                }
                else if (RecipeBrowserUI.instance.CurrentPanel == 3)
                {
                    if (BestiaryUI.instance?.queryItem?.item?.type == Main.HoverItem.type)
                    {
                        showRecipeBrowser = !RecipeBrowserUI.instance.ShowRecipeBrowser;
                    }
                    else
                    {
                        BestiaryUI.instance?.queryItem?.ReplaceWithFake(Main.HoverItem.type);
                    }
                }
                RecipeBrowserUI.instance.ShowRecipeBrowser = showRecipeBrowser;
            }

            if (RecipeBrowserMod.ToggleFavoritedPanelHotKey?.JustPressed == true)
            {
                RecipeBrowserUI.instance.ShowFavoritePanel = !RecipeBrowserUI.instance.ShowFavoritePanel;
                if (!RecipeBrowserUI.instance.ShowFavoritePanel)
                {
                    RecipeBrowserUI.instance.ForceHideFavoritePanel = true;
                }
                else
                {
                    RecipeBrowserUI.instance.ForceShowFavoritePanel = true;
                }
                RecipeBrowserUI.instance.favoritePanelUpdateNeeded = true;
            }
        }
    }
}
