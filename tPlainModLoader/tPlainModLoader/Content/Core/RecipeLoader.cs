using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 配方判定条件
    /// 作者: SaintCirno9
    /// </summary>
    public class Condition
    {
        public string Description { get; }
        public Func<bool> Predicate { get; }

        public Condition(string description, Func<bool> predicate)
        {
            Description = description;
            Predicate = predicate;
        }

        public Condition(LocalizedText description, Func<bool> predicate)
        {
            Description = description?.Value ?? string.Empty;
            Predicate = predicate;
        }

        public bool IsMet() => Predicate == null || Predicate();

        public static readonly Condition NearWater = new Condition("Near Water", () => Main.LocalPlayer.adjWaterSource || (Main.LocalPlayer.adjTile != null && 172 < Main.LocalPlayer.adjTile.Length && Main.LocalPlayer.adjTile[172]));
        public static readonly Condition NearLava = new Condition("Near Lava", () => Main.LocalPlayer.adjLava);
        public static readonly Condition NearHoney = new Condition("Near Honey", () => Main.LocalPlayer.adjHoney);
        public static readonly Condition NearShimmer = new Condition("Near Shimmer", () => Main.LocalPlayer.ZoneShimmer);
        public static readonly Condition TimeDay = new Condition("During Day", () => Main.dayTime);
        public static readonly Condition TimeNight = new Condition("During Night", () => !Main.dayTime);
        public static readonly Condition InGraveyard = new Condition("In Graveyard", () => Main.LocalPlayer.ZoneGraveyard);
        public static readonly Condition InDungeon = new Condition("In Dungeon", () => Main.LocalPlayer.ZoneDungeon);
        public static readonly Condition InCorrupt = new Condition("In Corruption", () => Main.LocalPlayer.ZoneCorrupt);
        public static readonly Condition InCrimson = new Condition("In Crimson", () => Main.LocalPlayer.ZoneCrimson);
        public static readonly Condition InHallow = new Condition("In Hallow", () => Main.LocalPlayer.ZoneHallow);
        public static readonly Condition InJungle = new Condition("In Jungle", () => Main.LocalPlayer.ZoneJungle);
        public static readonly Condition InSnow = new Condition("In Snow", () => Main.LocalPlayer.ZoneSnow);
        public static readonly Condition InDesert = new Condition("In Desert", () => Main.LocalPlayer.ZoneDesert);
        public static readonly Condition InGlowshroom = new Condition("In Glowing Mushroom", () => Main.LocalPlayer.ZoneGlowshroom);
        public static readonly Condition InUnderworld = new Condition("In Underworld", () => Main.LocalPlayer.ZoneUnderworldHeight);
        public static readonly Condition InAether = new Condition("In Aether", () => Main.LocalPlayer.ZoneShimmer);
        public static readonly Condition Hardmode = new Condition("Hardmode", () => Main.hardMode);
        public static readonly Condition PreHardmode = new Condition("Pre-Hardmode", () => !Main.hardMode);
        public static readonly Condition SmashedShadowOrb = new Condition("Smashed Shadow Orb", () => WorldGen.shadowOrbSmashed);
        public static readonly Condition NotRemixWorld = new Condition("Not Remix World", () => !Main.remixWorld);
        public static readonly Condition RemixWorld = new Condition("Remix World", () => Main.remixWorld);
        public static readonly Condition BloodMoon = new Condition("Blood Moon", () => Main.bloodMoon);
        public static readonly Condition Eclipse = new Condition("Solar Eclipse", () => Main.eclipse);

        public static readonly Condition DownedKingSlime = new Condition("Downed King Slime", () => NPC.downedSlimeKing);
        public static readonly Condition DownedEyeOfCthulhu = new Condition("Downed Eye of Cthulhu", () => NPC.downedBoss1);
        public static readonly Condition DownedEowOrBoc = new Condition("Downed Eater of Worlds or Brain of Cthulhu", () => NPC.downedBoss2);
        public static readonly Condition DownedEaterOfWorlds = new Condition("Downed Eater of Worlds", () => NPC.downedBoss2 && !WorldGen.crimson);
        public static readonly Condition DownedBrainOfCthulhu = new Condition("Downed Brain of Cthulhu", () => NPC.downedBoss2 && WorldGen.crimson);
        public static readonly Condition DownedQueenBee = new Condition("Downed Queen Bee", () => NPC.downedQueenBee);
        public static readonly Condition DownedSkeletron = new Condition("Downed Skeletron", () => NPC.downedBoss3);
        public static readonly Condition DownedDeerclops = new Condition("Downed Deerclops", () => NPC.downedDeerclops);
        public static readonly Condition DownedQueenSlime = new Condition("Downed Queen Slime", () => NPC.downedQueenSlime);
        public static readonly Condition DownedMechBossAny = new Condition("Downed Any Mechanical Boss", () => NPC.downedMechBossAny);
        public static readonly Condition DownedTwins = new Condition("Downed The Twins", () => NPC.downedMechBoss2);
        public static readonly Condition DownedDestroyer = new Condition("Downed The Destroyer", () => NPC.downedMechBoss1);
        public static readonly Condition DownedSkeletronPrime = new Condition("Downed Skeletron Prime", () => NPC.downedMechBoss3);
        public static readonly Condition DownedMechBossAll = new Condition("Downed All Mechanical Bosses", () => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3);
        public static readonly Condition DownedPlantera = new Condition("Downed Plantera", () => NPC.downedPlantBoss);
        public static readonly Condition DownedGolem = new Condition("Downed Golem", () => NPC.downedGolemBoss);
        public static readonly Condition DownedDukeFishron = new Condition("Downed Duke Fishron", () => NPC.downedFishron);
        public static readonly Condition DownedEmpressOfLight = new Condition("Downed Empress of Light", () => NPC.downedEmpressOfLight);
        public static readonly Condition DownedCultist = new Condition("Downed Lunatic Cultist", () => NPC.downedAncientCultist);
        public static readonly Condition DownedMoonLord = new Condition("Downed Moon Lord", () => NPC.downedMoonlord);
        public static readonly Condition DownedPirates = new Condition("Downed Pirates", () => NPC.downedPirates);
        public static readonly Condition DownedGoblinArmy = new Condition("Downed Goblin Army", () => NPC.downedGoblins);
        public static readonly Condition DownedFrostLegion = new Condition("Downed Frost Legion", () => NPC.downedFrost);
        public static readonly Condition DownedPumpking = new Condition("Downed Pumpking", () => NPC.downedHalloweenTree);
        public static readonly Condition DownedIceQueen = new Condition("Downed Ice Queen", () => NPC.downedChristmasIceQueen);
        public static readonly Condition DownedMourningWood = new Condition("Downed Mourning Wood", () => NPC.downedHalloweenTree);
        public static readonly Condition DownedSantaNK1 = new Condition("Downed Santa-NK1", () => NPC.downedChristmasSantank);
        public static readonly Condition DownedEverscream = new Condition("Downed Everscream", () => NPC.downedChristmasTree);
        public static readonly Condition DownedOldOnesArmyAny = new Condition("Downed Old One's Army", () => Terraria.GameContent.Events.DD2Event.DownedInvasionT1 || Terraria.GameContent.Events.DD2Event.DownedInvasionT2 || Terraria.GameContent.Events.DD2Event.DownedInvasionT3);
        public static readonly Condition DownedOldOnesArmyT1 = new Condition("Downed Old One's Army Tier 1", () => Terraria.GameContent.Events.DD2Event.DownedInvasionT1);
        public static readonly Condition DownedOldOnesArmyT2 = new Condition("Downed Old One's Army Tier 2", () => Terraria.GameContent.Events.DD2Event.DownedInvasionT2);
        public static readonly Condition DownedOldOnesArmyT3 = new Condition("Downed Old One's Army Tier 3", () => Terraria.GameContent.Events.DD2Event.DownedInvasionT3);
        public static readonly Condition DownedDarkMage = new Condition("Downed Dark Mage", () => Terraria.GameContent.Events.DD2Event.DownedInvasionT1);
        public static readonly Condition DownedOgre = new Condition("Downed Ogre", () => Terraria.GameContent.Events.DD2Event.DownedInvasionT2);
        public static readonly Condition DownedBetsy = new Condition("Downed Betsy", () => Terraria.GameContent.Events.DD2Event.DownedInvasionT3);
        public static readonly Condition DownedClown = new Condition("Downed Clown", () => NPC.downedClown);
        public static readonly Condition DownedDreadnautilus = new Condition("Downed Dreadnautilus", () => NPC.downedMechBossAny);
        public static readonly Condition DownedSolarPillar = new Condition("Downed Solar Pillar", () => NPC.downedTowerSolar);
        public static readonly Condition DownedVortexPillar = new Condition("Downed Vortex Pillar", () => NPC.downedTowerVortex);
        public static readonly Condition DownedNebulaPillar = new Condition("Downed Nebula Pillar", () => NPC.downedTowerNebula);
        public static readonly Condition DownedStardustPillar = new Condition("Downed Stardust Pillar", () => NPC.downedTowerStardust);
        public static readonly Condition HappyEnoughToSellPylons = new Condition("Happy Enough To Sell Pylons", () => true);
        public static Condition NpcIsPresent(int npcType) => new Condition("NPC Is Present", () => NPC.AnyNPCs(npcType));
        public static readonly Condition CorruptWorld = new Condition("Corruption World", () => !WorldGen.crimson);
        public static readonly Condition CrimsonWorld = new Condition("Crimson World", () => WorldGen.crimson);
        public static readonly Condition DownedMartians = new Condition("Downed Martian Madness", () => NPC.downedMartians);
        public static readonly Condition InDirtLayerHeight = new Condition("In Dirt Layer Height", () => Main.LocalPlayer.ZoneDirtLayerHeight);
        public static readonly Condition InRockLayerHeight = new Condition("In Rock Layer Height", () => Main.LocalPlayer.ZoneRockLayerHeight);
        public static readonly Condition InUnderworldHeight = new Condition("In Underworld Height", () => Main.LocalPlayer.ZoneUnderworldHeight);
        public static readonly Condition InSkyHeight = new Condition("In Sky Height", () => Main.LocalPlayer.ZoneSkyHeight);
        public static readonly Condition InBeach = new Condition("In Beach", () => Main.LocalPlayer.ZoneBeach);
        public static readonly Condition InExpertMode = new Condition("In Expert Mode", () => Main.expertMode);
        public static readonly Condition InMasterMode = new Condition("In Master Mode", () => Main.masterMode);
    }

    /// <summary>
    /// TPML 原生配方构建与向导检索中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class RecipeLoader
    {
        internal static readonly ILogger Logger = LogManager.GetLogger("RecipeLoader");

        private static readonly List<ModRecipe> _registeredModRecipes = new List<ModRecipe>();
        private static readonly Dictionary<Recipe, ModRecipe> _recipeMap = new Dictionary<Recipe, ModRecipe>();
        private static readonly Dictionary<string, RecipeGroup> _namedGroups = new Dictionary<string, RecipeGroup>(StringComparer.OrdinalIgnoreCase);

        static RecipeLoader()
        {
            try
            {
                On_Recipe.PlayerMeetsEnvironmentConditions += (orig, self, player, missingObjects) =>
                {
                    bool flag = orig(self, player, missingObjects);
                    if (!flag) return false;

                    if (TryGetModRecipe(self, out var modRecipe))
                    {
                        foreach (var cond in modRecipe.Conditions)
                        {
                            if (cond != null && !cond.IsMet())
                            {
                                if (missingObjects != null && !string.IsNullOrEmpty(cond.Description))
                                {
                                    missingObjects.Add(cond.Description);
                                }
                                return false;
                            }
                        }
                    }

                    return true;
                };
            }
            catch (Exception ex)
            {
                Logger.Warn($"PlayerMeetsEnvironmentConditions 挂钩异常: {ex.Message}");
            }
        }

        public static ModRecipe CreateRecipe(int resultType, int amount = 1)
        {
            var r = new ModRecipe();
            r.SetResult(resultType, amount);
            return r;
        }

        public static ModRecipe CreateRecipe(ModItem item, int amount = 1)
        {
            return CreateRecipe(item.Type, amount);
        }

        public static void AddRecipeGroup(string name, RecipeGroup group)
        {
            RegisterGroup(name, group);
        }

        public static int RegisterGroup(string name, RecipeGroup group)
        {
            if (string.IsNullOrEmpty(name) || group == null) return -1;

            if (group.RegisteredId < 0)
            {
                try
                {
                    group.Register();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"注册配方组 [{name}] 异常: {ex.Message}");
                }
            }

            _namedGroups[name] = group;
            return group.RegisteredId;
        }

        public static bool TryGetRecipeGroup(string name, out RecipeGroup group)
        {
            group = null;
            if (string.IsNullOrEmpty(name)) return false;

            if (_namedGroups.TryGetValue(name, out group))
            {
                return true;
            }

            EnsureVanillaRecipeGroupsPopulated();
            return _namedGroups.TryGetValue(name, out group);
        }

        public static int GetRecipeGroupId(string name)
        {
            if (TryGetRecipeGroup(name, out var group) && group != null)
            {
                return group.RegisteredId;
            }
            return -1;
        }

        public static RecipeGroup GetRecipeGroup(int id)
        {
            if (RecipeGroup.recipeGroups.TryGetValue(id, out var group))
            {
                return group;
            }
            return null;
        }

        public static bool TryGetModRecipe(Recipe recipe, out ModRecipe modRecipe)
        {
            modRecipe = null;
            if (recipe == null) return false;
            return _recipeMap.TryGetValue(recipe, out modRecipe);
        }

        internal static void RegisterModRecipeMapping(Recipe recipe, ModRecipe modRecipe)
        {
            if (recipe != null && modRecipe != null)
            {
                _recipeMap[recipe] = modRecipe;
            }
        }

        private static void EnsureVanillaRecipeGroupsPopulated()
        {
            RegisterGroupIfNotNull("Wood", RecipeGroups.Wood);
            RegisterGroupIfNotNull("IronBar", RecipeGroups.IronBar);
            RegisterGroupIfNotNull("Sand", RecipeGroups.Sand);
            RegisterGroupIfNotNull("Fragment", RecipeGroups.Fragment);
            RegisterGroupIfNotNull("PressurePlate", RecipeGroups.PressurePlate);
            RegisterGroupIfNotNull("Birds", RecipeGroups.Birds);
            RegisterGroupIfNotNull("Bugs", RecipeGroups.Bugs);
            RegisterGroupIfNotNull("Ducks", RecipeGroups.Ducks);
            RegisterGroupIfNotNull("Squirrels", RecipeGroups.Squirrels);
            RegisterGroupIfNotNull("Butterflies", RecipeGroups.Butterflies);
            RegisterGroupIfNotNull("Fireflies", RecipeGroups.Fireflies);
            RegisterGroupIfNotNull("Snails", RecipeGroups.Snails);
            RegisterGroupIfNotNull("FishForDinner", RecipeGroups.FishForDinner);
            RegisterGroupIfNotNull("GoldenCritter", RecipeGroups.GoldenCritter);
            RegisterGroupIfNotNull("Dragonflies", RecipeGroups.Dragonflies);
            RegisterGroupIfNotNull("Turtles", RecipeGroups.Turtles);
            RegisterGroupIfNotNull("Fruit", RecipeGroups.Fruit);
            RegisterGroupIfNotNull("Balloons", RecipeGroups.Balloons);
            RegisterGroupIfNotNull("Scorpions", RecipeGroups.Scorpions);
            RegisterGroupIfNotNull("Macaws", RecipeGroups.Macaws);
            RegisterGroupIfNotNull("Cockatiels", RecipeGroups.Cockatiels);
            RegisterGroupIfNotNull("CloudBalloons", RecipeGroups.CloudBalloons);
            RegisterGroupIfNotNull("BlizzardBalloons", RecipeGroups.BlizzardBalloons);
            RegisterGroupIfNotNull("SandstormBalloons", RecipeGroups.SandstormBalloons);
            RegisterGroupIfNotNull("CritterGuides", RecipeGroups.CritterGuides);
            RegisterGroupIfNotNull("NatureGuides", RecipeGroups.NatureGuides);
            RegisterGroupIfNotNull("Seashells", RecipeGroups.Seashells);
            RegisterGroupIfNotNull("Stone", RecipeGroups.Stone);
            RegisterGroupIfNotNull("CobaltBar", RecipeGroups.CobaltBar);
            RegisterGroupIfNotNull("MythrilBar", RecipeGroups.MythrilBar);
            RegisterGroupIfNotNull("AdamantiteBar", RecipeGroups.AdamantiteBar);
            RegisterGroupIfNotNull("GemCritter", RecipeGroups.GemCritter);
            RegisterGroupIfNotNull("MagicMirror", RecipeGroups.MagicMirror);
            RegisterGroupIfNotNull("Jellyfish", RecipeGroups.Jellyfish);
            RegisterGroupIfNotNull("SilverBar", RecipeGroups.SilverBar);
            RegisterGroupIfNotNull("GoldBar", RecipeGroups.GoldBar);
        }

        private static void RegisterGroupIfNotNull(string name, RecipeGroup group)
        {
            if (group != null && !_namedGroups.ContainsKey(name))
            {
                _namedGroups[name] = group;
            }
        }

        public static void SetupRecipes()
        {
            var log = TPML.Core.Logging.LogManager.GetLogger("RecipeLoader");
            try
            {
                EnsureVanillaRecipeGroupsPopulated();

                foreach (var system in ModContent.GetContent<ModSystem>())
                {
                    try
                    {
                        if (system.IsLoadingEnabled(system.Mod))
                        {
                            system.AddRecipeGroups();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"ModSystem {system.FullName} AddRecipeGroups 异常", ex);
                    }
                }

                foreach (var mod in ModContent.Mods)
                {
                    try
                    {
                        mod.AddRecipes();
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Mod {mod.Name} AddRecipes 异常", ex);
                    }
                }

                foreach (var item in ModContent.GetContent<ModItem>())
                {
                    try
                    {
                        if (item.IsLoadingEnabled(item.Mod))
                        {
                            item.AddRecipes();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"ModItem {item.FullName} AddRecipes 异常", ex);
                    }
                }

                foreach (var system in ModContent.GetContent<ModSystem>())
                {
                    try
                    {
                        if (system.IsLoadingEnabled(system.Mod))
                        {
                            system.AddRecipes();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"ModSystem {system.FullName} AddRecipes 异常", ex);
                    }
                }

                // 兜底确保所有注册配方已成功注入原版
                PostSetupRecipes();

                foreach (var system in ModContent.GetContent<ModSystem>())
                {
                    try
                    {
                        if (system.IsLoadingEnabled(system.Mod))
                        {
                            system.PostAddRecipes();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"ModSystem {system.FullName} PostAddRecipes 异常", ex);
                    }
                }

                // 确保原版配方数组及相关查找数组容量足够且填满非空 Recipe
                EnsureRecipeCapacity(Recipe.numRecipes + 10);

                var modItems = ModContent.GetContent<ModItem>().ToList();
                log.Info($"SetupRecipes 完成: Mods={ModContent.Mods.Count}, ModItems={modItems.Count}, 注入配方={_registeredModRecipes.Count}, 容量上限={Recipe.maxRecipes}");
            }
            catch (Exception ex)
            {
                log.Error("SetupRecipes 异常", ex);
                throw;
            }
        }

        public static void EnsureRecipeCapacity(int minCapacity)
        {
            if (Main.recipe == null) return;

            if (Main.recipe.Length < minCapacity)
            {
                int oldLen = Main.recipe.Length;
                int newLen = Math.Max(minCapacity, oldLen * 2);
                Array.Resize(ref Main.recipe, newLen);
                for (int k = oldLen; k < newLen; k++)
                {
                    Main.recipe[k] = new Recipe();
                }
            }
            else
            {
                // 确保已分配槽位中不存在 null
                for (int k = 0; k < Main.recipe.Length; k++)
                {
                    if (Main.recipe[k] == null)
                    {
                        Main.recipe[k] = new Recipe();
                    }
                }
            }

            if (Recipe.maxRecipes < Main.recipe.Length)
            {
                Recipe.maxRecipes = Main.recipe.Length;
            }

            if (Main.availableRecipe == null || Main.availableRecipe.Length < Recipe.maxRecipes)
            {
                Array.Resize(ref Main.availableRecipe, Recipe.maxRecipes);
            }

            if (CraftingUI.availableRecipeY == null || CraftingUI.availableRecipeY.Length < Recipe.maxRecipes)
            {
                int curLen = CraftingUI.availableRecipeY?.Length ?? 0;
                Array.Resize(ref CraftingUI.availableRecipeY, Recipe.maxRecipes);
                for (int i = curLen; i < CraftingUI.availableRecipeY.Length; i++)
                {
                    CraftingUI.availableRecipeY[i] = 65f * i;
                }
            }

            int tileReq = Math.Max(TileLoader.TileCount + 64, 800);
            if (Recipe.TileUsedInRecipes == null || Recipe.TileUsedInRecipes.Length < tileReq)
            {
                Array.Resize(ref Recipe.TileUsedInRecipes, tileReq);
            }
            if (Recipe.TileCountsAs == null || Recipe.TileCountsAs.Length < tileReq)
            {
                Array.Resize(ref Recipe.TileCountsAs, tileReq);
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
            _recipeMap.Clear();
            _namedGroups.Clear();
        }
    }

    /// <summary>
    /// TPML 模组配方链式构建器
    /// </summary>
    public class ModRecipe
    {
        public delegate void OnCraftCallback(Recipe recipe, Item item, List<Item> consumedItems, Item destinationStack);
        public delegate void ConsumeItemCallback(Recipe recipe, int type, ref int amount);
        public delegate void IngredientQuantityCallback(Recipe recipe, int type, ref int amount, bool isDecrafting);

        public int ResultType { get; private set; }
        public int ResultStack { get; private set; }
        public List<(int itemId, int stack)> RequiredItems { get; } = new List<(int, int)>();
        public List<(int groupId, int stack)> RequiredGroups { get; } = new List<(int, int)>();
        public List<int> RequiredTiles { get; } = new List<int>();
        public List<Condition> Conditions { get; } = new List<Condition>();
        public bool NotDecraftable { get; private set; }
        public List<int> DecraftFilters { get; } = new List<int>();
        public OnCraftCallback OnCraftHooks { get; private set; }
        public IngredientQuantityCallback ConsumeIngredientHooks { get; private set; }

        public static ModRecipe Create(int resultType, int amount = 1) => RecipeLoader.CreateRecipe(resultType, amount);

        public ModRecipe SetResult(int resultType, int amount = 1)
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
            return item != null ? AddIngredient(item.Type, stack) : this;
        }

        public ModRecipe AddIngredient<T>(int stack = 1) where T : ModItem
        {
            return AddIngredient(ModContent.ItemType<T>(), stack);
        }

        public ModRecipe AddIngredient(string modName, string itemName, int stack = 1)
        {
            string targetMod = string.IsNullOrEmpty(modName) ? "Fargowiltas" : modName;
            if (ModContent.TryFind<ModItem>(targetMod, itemName, out var item))
            {
                return AddIngredient(item.Type, stack);
            }
            return this;
        }

        public ModRecipe AddTile(int tileID)
        {
            RequiredTiles.Add(tileID);
            return this;
        }

        public ModRecipe AddTile(ModTile tile)
        {
            return tile != null ? AddTile(tile.Type) : this;
        }

        public ModRecipe AddTile<T>() where T : ModTile
        {
            return AddTile(ModContent.TileType<T>());
        }

        public ModRecipe AddTile(Mod mod, string tileName)
        {
            string modName = mod?.Name ?? "Fargowiltas";
            if (ModContent.TryFind<ModTile>(modName, tileName, out var tile))
            {
                return AddTile(tile.Type);
            }
            return this;
        }

        public ModRecipe AddCondition(Condition condition)
        {
            if (condition != null)
            {
                Conditions.Add(condition);
            }
            return this;
        }

        public ModRecipe AddCondition(string description, Func<bool> predicate)
        {
            return AddCondition(new Condition(description, predicate));
        }

        public ModRecipe AddCondition(LocalizedText description, Func<bool> predicate)
        {
            return AddCondition(new Condition(description, predicate));
        }

        public ModRecipe AddCondition(Func<bool> predicate)
        {
            return AddCondition(new Condition(string.Empty, predicate));
        }

        public ModRecipe AddRecipeGroup(string name, int stack = 1)
        {
            if (RecipeLoader.TryGetRecipeGroup(name, out var group) && group != null)
            {
                return AddRecipeGroup(group.RegisteredId, stack);
            }
            return this;
        }

        public ModRecipe AddRecipeGroup(int groupId, int stack = 1)
        {
            RequiredGroups.Add((groupId, stack));
            return this;
        }

        public ModRecipe AddRecipeGroup(RecipeGroup group, int stack = 1)
        {
            if (group != null)
            {
                if (group.RegisteredId < 0)
                {
                    try { group.Register(); }
                    catch (Exception ex) { RecipeLoader.Logger.Warn($"注册配方组 [{group.RegisteredId}] 异常: {ex.Message}"); }
                }
                return AddRecipeGroup(group.RegisteredId, stack);
            }
            return this;
        }

        public ModRecipe DisableDecraft()
        {
            NotDecraftable = true;
            return this;
        }

        public ModRecipe AddDecraftFilter(int itemType)
        {
            if (itemType > 0 && !DecraftFilters.Contains(itemType))
            {
                DecraftFilters.Add(itemType);
            }
            return this;
        }

        public ModRecipe AddOnCraftCallback(OnCraftCallback callback)
        {
            OnCraftHooks += callback;
            return this;
        }

        public ModRecipe AddConsumeItemCallback(ConsumeItemCallback callback)
        {
            ConsumeIngredientHooks += (Recipe recipe, int type, ref int num, bool decraft) => callback(recipe, type, ref num);
            return this;
        }

        public ModRecipe AddConsumeIngredientCallback(IngredientQuantityCallback callback)
        {
            ConsumeIngredientHooks += callback;
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

            RecipeLoader.EnsureRecipeCapacity(Recipe.numRecipes + 10);

            Recipe recipe = new Recipe();
            recipe.createItem = new Item();
            recipe.createItem.SetDefaults(ResultType);
            if (recipe.createItem.type != ResultType || recipe.createItem.IsAir)
            {
                recipe.createItem.type = ResultType;
                recipe.createItem.stack = ResultStack > 0 ? ResultStack : 1;
                ItemLoader.SetDefaults(recipe.createItem);
            }
            recipe.createItem.stack = ResultStack > 0 ? ResultStack : 1;

            int reqIndex = 0;

            // 注入普通物品材料
            for (int i = 0; i < RequiredItems.Count && reqIndex < recipe.requiredItem.Length; i++, reqIndex++)
            {
                var (itemId, stack) = RequiredItems[i];
                recipe.requiredItem[reqIndex] = new Item();
                recipe.requiredItem[reqIndex].SetDefaults(itemId);
                recipe.requiredItem[reqIndex].stack = stack;
                recipe.requiredItemQuickLookup[reqIndex] = new Recipe.RequiredItemEntry(itemId, stack);
            }

            // 注入配方组材料
            for (int i = 0; i < RequiredGroups.Count && reqIndex < recipe.requiredItem.Length; i++, reqIndex++)
            {
                var (groupId, stack) = RequiredGroups[i];
                if (RecipeGroup.recipeGroups.TryGetValue(groupId, out RecipeGroup group) && group != null)
                {
                    recipe.RequireGroup(group);
                    int displayItemId = group.Items.Count > 0 ? group.Items[0] : 0;
                    recipe.requiredItem[reqIndex] = new Item();
                    if (displayItemId > 0)
                    {
                        recipe.requiredItem[reqIndex].SetDefaults(displayItemId);
                    }
                    recipe.requiredItem[reqIndex].stack = stack;
                    recipe.requiredItemQuickLookup[reqIndex] = new Recipe.RequiredItemEntry(group, stack);
                }
            }

            // 确保剩余未使用的槽位填充非空 Item 与 default RequiredItemEntry
            for (int i = reqIndex; i < recipe.requiredItem.Length; i++)
            {
                if (recipe.requiredItem[i] == null)
                {
                    recipe.requiredItem[i] = new Item();
                }
                recipe.requiredItemQuickLookup[i] = default;
            }

            if (RequiredTiles.Count > 0)
            {
                recipe.requiredTile = RequiredTiles[0];
                if (recipe.requiredTile >= 0 && recipe.requiredTile < Recipe.TileUsedInRecipes.Length)
                {
                    Recipe.TileUsedInRecipes[recipe.requiredTile] = true;
                }
            }

            if (NotDecraftable)
            {
                recipe.notDecraftable = true;
            }

            if (DecraftFilters.Count > 0)
            {
                foreach (var filter in DecraftFilters)
                {
                    recipe.AddCustomShimmerResult(filter);
                }
            }

            RecipeLoader.RegisterModRecipeMapping(recipe, this);
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
            if (count != (RequiredItems.Count + RequiredGroups.Count)) return false;

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
