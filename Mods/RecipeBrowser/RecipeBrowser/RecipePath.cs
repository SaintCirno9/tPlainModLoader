using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RecipeBrowser.Common;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using TPML.Content.Fusion;

namespace RecipeBrowser
{
    public struct TileTypeMinPickPair
    {
        public int Tile;
        public int MinPick;

        public TileTypeMinPickPair(int tile, int minPick)
        {
            Tile = tile;
            MinPick = minPick;
        }

        public void Deconstruct(out int tile, out int minPick)
        {
            tile = Tile;
            minPick = MinPick;
        }
    }

    public struct ShopEntry
    {
        public int npcType;
        public int itemType;
        public int price;

        public ShopEntry(int npcType, int itemType, int price)
        {
            this.npcType = npcType;
            this.itemType = itemType;
            this.price = price;
        }
    }

    public static class RecipePath
    {
        internal static bool sourceInventory = true;
        internal static bool sourceBanks = false;
        internal static bool sourceChests = false;
        internal static bool sourceMagicStorage = false;
        internal static bool extendedCraft = false;
        internal static bool allowLoots = false;
        internal static bool allowMineables = false;
        internal static Dictionary<int, List<TileTypeMinPickPair>> mineables;
        internal static bool allowBugNetables = false;
        internal static Dictionary<int, int> bugNetables;
        internal static bool allowMissingStations = false;
        internal static bool allowPurchasable = true;
        internal static Dictionary<int, List<ShopEntry>> purchasable;
        internal static Dictionary<int, List<Recipe>> recipeDictionary;

        internal static void Refresh(bool complete = false)
        {
            purchasable = null;
            if (complete)
            {
                recipeDictionary = null;
                allowLoots = false;
                allowMissingStations = false;
                allowPurchasable = false;
            }
        }

        internal static void InitializeRecipeDictionary()
        {
            recipeDictionary = new Dictionary<int, List<Recipe>>();
            for (int i = 0; i < Recipe.numRecipes; i++)
            {
                Recipe r = Main.recipe[i];
                if (r == null || r.createItem == null) continue;
                if (!recipeDictionary.TryGetValue(r.createItem.type, out var list))
                {
                    recipeDictionary.Add(r.createItem.type, list = new List<Recipe>());
                }
                list.Add(r);
            }
            List<int> toRemove = new List<int>();
            foreach (var kvp in recipeDictionary)
            {
                if (kvp.Value.Count > 15) toRemove.Add(kvp.Key);
            }
            foreach (int k in toRemove) recipeDictionary.Remove(k);
        }

        internal static void Adjust(this Dictionary<int, int> d, int key, int adjustment)
        {
            d.TryGetValue(key, out var val);
            int newVal = val + adjustment;
            if (newVal <= 0) d.Remove(key);
            else d[key] = newVal;
        }

        internal static void PrepareGetCraftPaths()
        {
            if (recipeDictionary == null) InitializeRecipeDictionary();
            if (purchasable == null && allowPurchasable) InitializePurchasable();
            if (bugNetables == null && allowBugNetables)
            {
                bugNetables = new Dictionary<int, int>();
                for (int i = 1; i < ItemID.Count; i++)
                {
                    Item item = ContentSamples.ItemsByType[i];
                    if (item != null && item.makeNPC > 0) bugNetables[i] = item.makeNPC;
                }
            }
            if (mineables != null || !allowMineables) return;

            mineables = new Dictionary<int, List<TileTypeMinPickPair>>();
            for (int j = 1; j < ItemID.Count; j++)
            {
                Item item = ContentSamples.ItemsByType[j];
                if (item == null || item.createTile <= -1 || (item.createTile < Main.tileFrameImportant.Length && Main.tileFrameImportant[item.createTile]))
                {
                    continue;
                }
                if (item.createTile < Main.tileCut.Length && Main.tileCut[item.createTile])
                {
                    if (!mineables.TryGetValue(j, out var list))
                    {
                        mineables.Add(j, list = new List<TileTypeMinPickPair>());
                    }
                    list.Add(new TileTypeMinPickPair(item.createTile, -1));
                    continue;
                }

                Tile t = Main.tile[0, 0];
                t.active(true);
                t.type = (ushort)item.createTile;
                for (int pick = 5; pick < 300; pick += 5)
                {
                    int hitBuffer = Main.LocalPlayer.hitTile.HitObject(0, 0, 1);
                    if (Main.LocalPlayer.GetPickaxeDamage(0, 0, pick, hitBuffer, t) > 0)
                    {
                        if (!mineables.TryGetValue(j, out var list2))
                        {
                            mineables.Add(j, list2 = new List<TileTypeMinPickPair>());
                        }
                        list2.Add(new TileTypeMinPickPair(item.createTile, pick));
                        break;
                    }
                }
            }
        }

        internal static void InitializePurchasable()
        {
            if (purchasable != null) return;
            purchasable = new Dictionary<int, List<ShopEntry>>();

            // 针对原版常见城镇 NPC 商店进行映射
            Chest testChest = new Chest();
            for (int npcId = 1; npcId < NPCID.Count; npcId++)
            {
                try
                {
                    testChest.SetupShop(npcId);
                    for (int s = 0; s < 40; s++)
                    {
                        Item it = testChest.item[s];
                        if (it != null && !it.IsAir && it.type > 0)
                        {
                            if (!purchasable.TryGetValue(it.type, out var list))
                            {
                                purchasable.Add(it.type, list = new List<ShopEntry>());
                            }
                            list.Add(new ShopEntry(npcId, it.type, it.value));
                        }
                    }
                }
                catch { }
            }
        }

        public static List<CraftPath> GetCraftPaths(Recipe recipe, CancellationToken token, bool single)
        {
            Dictionary<int, int> haveItems = CalculateHaveItems();
            List<CraftPath> list = new List<CraftPath>();
            CraftPath craftPath = new CraftPath(recipe, haveItems);
            FindCraftPaths(list, craftPath, token, single);

            if (list.Count == 0)
            {
                list.Add(craftPath);
            }

            if (!allowMissingStations)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].root.GetAllChildrenPreOrder().OfType<CraftPath.RecipeNode>().Any(rn => rn.recipe.requiredTile > -1 && RecipeBrowserPlayer.seenTiles != null && rn.recipe.requiredTile < RecipeBrowserPlayer.seenTiles.Length && !RecipeBrowserPlayer.seenTiles[rn.recipe.requiredTile]))
                    {
                        list.RemoveAt(i);
                    }
                }
            }
            return list;
        }

        private static Dictionary<int, int> CalculateHaveItems()
        {
            Dictionary<int, int> dict = new Dictionary<int, int>();
            if (sourceInventory)
            {
                for (int i = 0; i < 59; i++)
                {
                    Item item = (i == 58) ? Main.mouseItem : Main.LocalPlayer.inventory[i];
                    if (item != null && !item.IsAir && item.type > 0)
                    {
                        dict.Adjust(item.type, item.stack);
                    }
                }

                // 背包融合 (Fusion) 穿透检测
                try
                {
                    var fusionItems = InventoryFusionManager.GetAllFusionItems(Main.LocalPlayer);
                    if (fusionItems != null)
                    {
                        foreach (var fit in fusionItems)
                        {
                            if (fit != null && !fit.IsAir && fit.type > 0)
                            {
                                dict.Adjust(fit.type, fit.stack);
                            }
                        }
                    }
                }
                catch { }
            }
            return dict;
        }

        private static void FindCraftPaths(List<CraftPath> paths, CraftPath inProgress, CancellationToken token, bool single)
        {
            if (single && paths.Count > 0) return;
            if (token.IsCancellationRequested) return;
            if (inProgress.root.GetAllChildrenPreOrder().Count() > 20) return;

            CraftPath.UnfulfilledNode current = inProgress.GetCurrent();
            if (current == null)
            {
                paths.Add(inProgress.Clone());
                return;
            }

            HashSet<int> viable = current.item != null ? new HashSet<int>(current.item) : new HashSet<int>();
            int stack = current.stack;
            current.CheckParentsForRecipeLoopViaIngredients(viable);
            if (viable.Count == 0) return;

            if (recipeDictionary != null)
            {
                foreach (Recipe item in recipeDictionary.Where(x => viable.Contains(x.Key)).SelectMany(x => x.Value).ToList())
                {
                    if (inProgress.root is CraftPath.RecipeNode rn)
                    {
                        Recipe rootRecipe = rn.recipe;
                        if (item.requiredItem.Any(x => x != null && x.type == rootRecipe.createItem.type && x.stack >= rootRecipe.createItem.stack))
                        {
                            continue;
                        }
                    }
                    int needed = (stack - 1) / Math.Max(1, item.createItem.stack) + 1;
                    CraftPath.RecipeNode recipeNode = inProgress.Push(current, item, needed);
                    FindCraftPaths(paths, inProgress, token, single);
                    inProgress.Pop(current, recipeNode);
                }
            }

            if (allowPurchasable && purchasable != null)
            {
                var intersect = viable.Intersect(purchasable.Keys);
                foreach (int it in intersect)
                {
                    foreach (ShopEntry entry in purchasable[it])
                    {
                        if (NPCUnlocked(entry.npcType))
                        {
                            CraftPath.BuyItemNode buyNode = new CraftPath.BuyItemNode(it, stack, entry.price, entry.npcType, current.ChildNumber, current.parent, current.craftPath);
                            inProgress.Push(current, buyNode);
                            FindCraftPaths(paths, inProgress, token, single);
                            inProgress.Pop(current, buyNode);
                        }
                    }
                }
            }

            if (allowLoots && LootCache.instance?.lootInfos != null)
            {
                var lootIntersect = viable.Intersect(LootCache.instance.lootInfos.Keys);
                if (lootIntersect.Any())
                {
                    int first = lootIntersect.First();
                    bool unlocked = LootCache.instance.lootInfos[first].Any(NPCUnlocked);
                    if (unlocked)
                    {
                        CraftPath.LootItemNode lootNode = new CraftPath.LootItemNode(first, stack, current.ChildNumber, current.parent, current.craftPath);
                        inProgress.Push(current, lootNode);
                        FindCraftPaths(paths, inProgress, token, single);
                        inProgress.Pop(current, lootNode);
                    }
                }
            }

            if (allowMineables && mineables != null)
            {
                int bestPick = Main.LocalPlayer.GetBestPickaxe()?.pick ?? -1;
                var mineIntersect = viable.Intersect(mineables.Keys);
                foreach (int it in mineIntersect)
                {
                    foreach (var (tType, minP) in mineables[it])
                    {
                        if (RecipeBrowserPlayer.seenTiles != null && tType < RecipeBrowserPlayer.seenTiles.Length && RecipeBrowserPlayer.seenTiles[tType] && bestPick >= minP)
                        {
                            CraftPath.MineItemNode mineNode = new CraftPath.MineItemNode(it, stack, current.ChildNumber, current.parent, current.craftPath);
                            inProgress.Push(current, mineNode);
                            FindCraftPaths(paths, inProgress, token, single);
                            inProgress.Pop(current, mineNode);
                        }
                    }
                }
            }

            if (allowBugNetables && bugNetables != null)
            {
                foreach (int it in viable.Intersect(bugNetables.Keys))
                {
                    if (NPCUnlocked(bugNetables[it]))
                    {
                        CraftPath.BugNetItemNode bugNode = new CraftPath.BugNetItemNode(it, stack, bugNetables[it], current.ChildNumber, current.parent, current.craftPath);
                        inProgress.Push(current, bugNode);
                        FindCraftPaths(paths, inProgress, token, single);
                        inProgress.Pop(current, bugNode);
                        break;
                    }
                }
            }
        }

        internal static bool NPCUnlocked(int npcid)
        {
            var entry = Main.BestiaryDB?.FindEntryByNPCID(npcid);
            if (entry == null || entry.UIInfoProvider == null) return true;
            return entry.UIInfoProvider.GetEntryUICollectionInfo().UnlockState > BestiaryEntryUnlockState.NotKnownAtAll_0;
        }

        internal static bool ItemFullyResearched(int itemID)
        {
            int current = 0;
            int max = 0;
            if (Main.LocalPlayerCreativeTracker?.ItemSacrifices != null && Main.LocalPlayerCreativeTracker.ItemSacrifices.TryGetSacrificeNumbers(itemID, out current, out max) && current >= max)
            {
                return true;
            }
            return false;
        }
    }

    public static class RecipePathTester
    {
        public static bool print = false;
        public static bool printResults = false;
        public static bool thousandExtra = false;
    }
}
