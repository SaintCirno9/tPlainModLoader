using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Assets;
using Terraria.ModLoader.Engine;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader / TPML 物品加载与生命周期运行时中心
    /// </summary>
    public static class ItemLoader
    {
        private static readonly List<ModItem> _items = new List<ModItem>();
        private static readonly Dictionary<int, ModItem> _itemsByType = new Dictionary<int, ModItem>();
        private static readonly Dictionary<Type, int> _typesByClass = new Dictionary<Type, int>();
        private static readonly Dictionary<string, int> _typesByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, string> _displayNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> _tooltips = new Dictionary<int, string>();

        public static IReadOnlyList<ModItem> Items => _items;
        public static int ItemCount => _items.Count;
        public static int NextItemID = 6200; // 远大于原版 ItemID.Count (6196)

        public static int Register(ModItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_typesByClass.TryGetValue(item.GetType(), out int existingType))
            {
                return existingType;
            }

            int type = NextItemID++;
            item.Item = new Item();
            item.Item.type = type;

            _items.Add(item);
            _itemsByType[type] = item;
            _typesByClass[item.GetType()] = type;

            string modPrefix = item.Mod?.Name ?? "Terraria";
            _typesByName[$"{modPrefix}/{item.Name}"] = type;
            _typesByName[item.Name] = type;

            ModContent.RegisterItemType(item.GetType(), type);

            EnsureArraySizes(type);
            LoadItemTexture(item);

            TModShimEngine.Log($"[ItemLoader] 成功注册物品: [{item.Mod?.Name ?? "Mod"}/{item.Name}] -> ItemID={type}");
            return type;
        }

        public static ModItem GetItem(int type)
        {
            return _itemsByType.TryGetValue(type, out var item) ? item : null;
        }

        public static int GetItemType<T>() where T : ModItem
        {
            return _typesByClass.TryGetValue(typeof(T), out int type) ? type : 0;
        }

        public static int GetItemType(Type type)
        {
            if (type == null) return 0;
            return _typesByClass.TryGetValue(type, out int id) ? id : 0;
        }

        public static int GetItemType(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            return _typesByName.TryGetValue(name, out int type) ? type : 0;
        }

        public static void SetDisplayName(int type, string name)
        {
            if (type > 0 && !string.IsNullOrEmpty(name))
            {
                _displayNames[type] = name;
            }
        }

        public static string GetDisplayName(int type)
        {
            if (_displayNames.TryGetValue(type, out string name)) return name;
            if (_itemsByType.TryGetValue(type, out var item)) return item.Name;
            return $"ModItem_{type}";
        }

        public static void SetTooltip(int type, string tooltip)
        {
            if (type > 0 && !string.IsNullOrEmpty(tooltip))
            {
                _tooltips[type] = tooltip;
            }
        }

        public static string GetTooltip(int type)
        {
            return _tooltips.TryGetValue(type, out string tip) ? tip : null;
        }

        public static void EnsureArraySizes(int maxType = -1)
        {
            int targetSize = Math.Max(NextItemID + 128, maxType + 128);

            // 1. 扩容核心资产与本地化数组
            if (TextureAssets.Item != null && TextureAssets.Item.Length < targetSize)
            {
                Array.Resize(ref TextureAssets.Item, targetSize);
            }

            if (Lang._itemNameCache != null && Lang._itemNameCache.Length < targetSize)
            {
                Array.Resize(ref Lang._itemNameCache, targetSize);
            }

            if (Main.itemAnimations != null && Main.itemAnimations.Length < targetSize)
            {
                Array.Resize(ref Main.itemAnimations, targetSize);
            }

            if (Terraria.DataStructures.ArmorSetBonuses.SetsContaining != null && Terraria.DataStructures.ArmorSetBonuses.SetsContaining.Length < targetSize)
            {
                int oldLen = Terraria.DataStructures.ArmorSetBonuses.SetsContaining.Length;
                Array.Resize(ref Terraria.DataStructures.ArmorSetBonuses.SetsContaining, targetSize);
                var emptyArray = new Terraria.DataStructures.ArmorSetBonus[0];
                for (int i = oldLen; i < targetSize; i++)
                {
                    Terraria.DataStructures.ArmorSetBonuses.SetsContaining[i] = emptyArray;
                }
            }

            if (Item.cachedItemSpawnsByType != null && Item.cachedItemSpawnsByType.Length < targetSize)
            {
                int oldLen = Item.cachedItemSpawnsByType.Length;
                Array.Resize(ref Item.cachedItemSpawnsByType, targetSize);
                for (int i = oldLen; i < targetSize; i++)
                {
                    Item.cachedItemSpawnsByType[i] = -1;
                }
            }

            // Item.staff 与 Item.claw 位于 Item 类本身，不属于 ItemID.Sets；玩家手持模组物品时原版绘制/使用逻辑会按物品 ID 直接索引它们。
            if (Item.staff != null && Item.staff.Length < targetSize) Array.Resize(ref Item.staff, targetSize);
            if (Item.claw != null && Item.claw.Length < targetSize) Array.Resize(ref Item.claw, targetSize);

            // 2. 显式直连扩容常用关键 Sets 与 Prefix 数组
            try
            {
                if (ItemID.Sets.ItemNoGravity != null && ItemID.Sets.ItemNoGravity.Length < targetSize) Array.Resize(ref ItemID.Sets.ItemNoGravity, targetSize);
                if (ItemID.Sets.OverflowProtectionTimeOffset != null && ItemID.Sets.OverflowProtectionTimeOffset.Length < targetSize) Array.Resize(ref ItemID.Sets.OverflowProtectionTimeOffset, targetSize);
                if (ItemID.Sets.ItemIconPulse != null && ItemID.Sets.ItemIconPulse.Length < targetSize) Array.Resize(ref ItemID.Sets.ItemIconPulse, targetSize);
                if (ItemID.Sets.IsAMaterial != null && ItemID.Sets.IsAMaterial.Length < targetSize) Array.Resize(ref ItemID.Sets.IsAMaterial, targetSize);
                if (ItemID.Sets.IsFood != null && ItemID.Sets.IsFood.Length < targetSize) Array.Resize(ref ItemID.Sets.IsFood, targetSize);
                if (ItemID.Sets.AnimatesAsSoul != null && ItemID.Sets.AnimatesAsSoul.Length < targetSize) Array.Resize(ref ItemID.Sets.AnimatesAsSoul, targetSize);
                if (ItemID.Sets.NebulaPickup != null && ItemID.Sets.NebulaPickup.Length < targetSize) Array.Resize(ref ItemID.Sets.NebulaPickup, targetSize);

                if (Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.BoomerangsChakrams != null && Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.BoomerangsChakrams.Length < targetSize) Array.Resize(ref Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.BoomerangsChakrams, targetSize);
                if (Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.ItemsThatCanHaveLegendary2 != null && Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.ItemsThatCanHaveLegendary2.Length < targetSize) Array.Resize(ref Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.ItemsThatCanHaveLegendary2, targetSize);
                if (Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.Magic != null && Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.Magic.Length < targetSize) Array.Resize(ref Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.Magic, targetSize);
                if (Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.Summon != null && Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.Summon.Length < targetSize) Array.Resize(ref Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.Summon, targetSize);
                if (Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.GunsBows != null && Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.GunsBows.Length < targetSize) Array.Resize(ref Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.GunsBows, targetSize);
                if (Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.SpearsMacesChainsawsDrillsPunchCannon != null && Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.SpearsMacesChainsawsDrillsPunchCannon.Length < targetSize) Array.Resize(ref Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.SpearsMacesChainsawsDrillsPunchCannon, targetSize);
                if (Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.SwordsHammersAxesPicks != null && Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.SwordsHammersAxesPicks.Length < targetSize) Array.Resize(ref Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets.SwordsHammersAxesPicks, targetSize);
            }
            catch { }

            // 3. 自动反射扩容 ItemID.Sets 中所有其余静态数组字段
            foreach (FieldInfo field in typeof(ItemID.Sets).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    if (field.FieldType.IsArray)
                    {
                        Array arr = (Array)field.GetValue(null);
                        if (arr != null && arr.Length < targetSize)
                        {
                            Type elementType = field.FieldType.GetElementType();
                            Array newArr = Array.CreateInstance(elementType, targetSize);
                            Array.Copy(arr, newArr, arr.Length);
                            field.SetValue(null, newArr);
                        }
                    }
                }
                catch { }
            }
        }

        public static void LoadItemTexture(ModItem item)
        {
            try
            {
                EnsureArraySizes(item.Type);
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;

                if (device == null)
                {
                    return;
                }

                Texture2D texture = null;
                string texPath = item.Texture;

                // 1. 优先从模组程序集内嵌资源加载。Mod 资产仓库在找不到文件时会返回占位贴图，不能让占位贴图遮蔽真实 PNG。
                Assembly asm = item.GetType().Assembly;
                string[] resNames = asm.GetManifestResourceNames();
                string targetRes = null;
                foreach (var res in resNames)
                {
                    if (res.Equals($"{item.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{item.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.Equals($"{item.Name}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{item.Name}.rawimg", StringComparison.OrdinalIgnoreCase))
                    {
                        targetRes = res;
                        break;
                    }
                }

                if (targetRes != null)
                {
                    using (Stream stream = asm.GetManifestResourceStream(targetRes))
                    {
                        if (stream != null)
                        {
                            if (targetRes.EndsWith(".rawimg", StringComparison.OrdinalIgnoreCase))
                            {
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    byte[] bytes = ms.ToArray();
                                    if (bytes.Length >= 12)
                                    {
                                        int version = BitConverter.ToInt32(bytes, 0);
                                        int width = BitConverter.ToInt32(bytes, 4);
                                        int height = BitConverter.ToInt32(bytes, 8);
                                        int expectedPixelBytes = width * height * 4;
                                        if (version == 1 && width > 0 && height > 0 && bytes.Length >= 12 + expectedPixelBytes)
                                        {
                                            texture = new Texture2D(device, width, height);
                                            byte[] pixelData = new byte[expectedPixelBytes];
                                            Buffer.BlockCopy(bytes, 12, pixelData, 0, expectedPixelBytes);
                                            texture.SetData(pixelData);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                texture = Texture2D.FromStream(device, stream);
                            }
                        }
                    }
                }

                // 2. 再尝试从 Mod 资产仓库加载 tModLoader/.tmod 文件资产。
                if (texture == null && item.Mod != null)
                {
                    var asset = item.Mod.Assets.Request<Texture2D>(texPath);
                    if (asset != null && asset.IsLoaded)
                    {
                        texture = asset.Value;
                    }
                }

                // 3. 最后使用彩色占位材质，避免物品图标为空导致原版绘制链崩溃。
                if (texture == null)
                {
                    texture = new Texture2D(device, 32, 32);
                    Color[] colors = new Color[32 * 32];
                    for (int i = 0; i < colors.Length; i++) colors[i] = new Color(255, 140, 40, 255);
                    texture.SetData(colors);
                }

                var assetInstance = CreateAsset(texture, texPath);
                TextureAssets.Item[item.Type] = assetInstance;
                TModShimEngine.Log($"[ItemLoader] ★ 物品 [{item.Name}] 贴图已绑定: {texture.Width}x{texture.Height}");
            }
            catch (Exception ex)
            {
                TModShimEngine.Log($"[ItemLoader] 载入物品贴图异常 [{item.Name}]: {ex.Message}");
            }
        }

        public static void EnsureTextureLoaded(int type)
        {
            EnsureArraySizes(type);
            if (_itemsByType.TryGetValue(type, out ModItem item))
            {
                if (TextureAssets.Item == null || type >= TextureAssets.Item.Length || TextureAssets.Item[type] == null || TextureAssets.Item[type].Value == null)
                {
                    LoadItemTexture(item);
                }
            }
        }

        public static void ReloadTextures()
        {
            foreach (var item in _items)
            {
                if (item.Type > 0)
                {
                    EnsureTextureLoaded(item.Type);
                }
            }
        }

        private static readonly FieldInfo _assetFieldName = typeof(Asset<Texture2D>).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo _assetFieldValue = typeof(Asset<Texture2D>).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo _assetFieldState = typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo _assetFieldDisposed = typeof(Asset<Texture2D>).GetField("<IsDisposed>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static Asset<Texture2D> CreateAsset(Texture2D tex, string name)
        {
            if (tex == null) return null;
            try
            {
                var asset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                if (_assetFieldName != null) _assetFieldName.SetValue(asset, name ?? "ModItem");
                if (_assetFieldValue != null) _assetFieldValue.SetValue(asset, tex);
                if (_assetFieldState != null) _assetFieldState.SetValue(asset, AssetState.Loaded);
                if (_assetFieldDisposed != null) _assetFieldDisposed.SetValue(asset, false);

                TModShimEngine.Log($"[ItemLoader] ★ 成功为 [{name}] 构建有效 Asset<Texture2D> (尺寸: {tex.Width}x{tex.Height}, 状态: Loaded)");
                return asset;
            }
            catch (Exception ex)
            {
                TModShimEngine.Log($"[ItemLoader] CreateAsset 异常: {ex.Message}");
                return null;
            }
        }

        public static void SetDefaults(Item item, bool createModItem = true)
        {
            if (item == null) return;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                ModItem modItem = (ModItem)Activator.CreateInstance(template.GetType());
                modItem.Item = item;
                modItem.Mod = template.Mod;
                modItem.SetDefaults();

                item.type = template.Type;
                if (item.stack <= 0) item.stack = 1;
                item.alpha = 0;

                string displayName = template.DisplayName;
                if (string.IsNullOrEmpty(displayName)) displayName = template.Name;
                item.SetNameOverride(displayName);

                EnsureTextureLoaded(template.Type);

                foreach (var gItem in TModHookDispatcher.ActiveGlobalItems)
                {
                    try { gItem.SetDefaults(item); } catch (Exception ex) { TModShimEngine.Log($"[ItemLoader] GlobalItem.SetDefaults 异常: {ex.Message}"); }
                }
            }
        }

        public static bool? CanUseItem(Item item, Player player)
        {
            if (item == null || item.IsAir) return null;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                bool? canUse = template.CanUseItem(player);
                if (canUse == false) return false;
            }

            foreach (var gItem in TModHookDispatcher.ActiveGlobalItems)
            {
                bool? gCanUse = gItem.CanUseItem(item, player);
                if (gCanUse == false) return false;
            }

            return null;
        }

        public static bool? UseItem(Item item, Player player)
        {
            if (item == null || item.IsAir) return null;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                template.UseItem(player);
            }

            foreach (var gItem in TModHookDispatcher.ActiveGlobalItems)
            {
                gItem.UseItem(item, player);
            }

            return null;
        }

        public static void HoldItem(Item item, Player player)
        {
            if (item == null || item.IsAir) return;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                template.HoldItem(player);
            }

            foreach (var gItem in TModHookDispatcher.ActiveGlobalItems)
            {
                gItem.HoldItem(item, player);
            }
        }

        public static void UpdateInventory(Item item, Player player)
        {
            if (item == null || item.IsAir) return;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                template.UpdateInventory(player);
            }

            foreach (var gItem in TModHookDispatcher.ActiveGlobalItems)
            {
                gItem.UpdateInventory(item, player);
            }
        }

        public static void UpdateEquip(Item item, Player player)
        {
            if (item == null || item.IsAir) return;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                template.UpdateEquip(player);
            }

            foreach (var gItem in TModHookDispatcher.ActiveGlobalItems)
            {
                gItem.UpdateEquip(item, player);
            }
        }

        public static void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item == null || item.IsAir || tooltips == null) return;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                template.ModifyTooltips(tooltips);
            }

            foreach (var gItem in TModHookDispatcher.ActiveGlobalItems)
            {
                gItem.ModifyTooltips(item, tooltips);
            }
        }

        public static bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (item == null || item.IsAir) return true;
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                return template.Shoot(player, source, position, velocity, type, damage, knockback);
            }
            return true;
        }

        private static bool _recipesAdded = false;

        public static void AddRecipes()
        {
            TModShimEngine.Log($"[ItemLoader] 开始执行模组配方注册 (已注册物品数={_items.Count})...");
            int count = 0;
            foreach (var item in _items)
            {
                try
                {
                    item.AddRecipes();
                    count++;
                }
                catch (Exception ex)
                {
                    TModShimEngine.Log($"[ItemLoader] 执行 {item.Name}.AddRecipes() 异常: {ex}");
                }
            }
            _recipesAdded = true;
            TModShimEngine.Log($"[ItemLoader] 模组配方注册完成，共处理 {count} 个物品。当前全局配方数: {Recipe.numRecipes}");
        }

        public static void Clear()
        {
            _items.Clear();
            _itemsByType.Clear();
            _typesByClass.Clear();
            _typesByName.Clear();
            _displayNames.Clear();
            _tooltips.Clear();
            NextItemID = 6200;
        }
    }
}
