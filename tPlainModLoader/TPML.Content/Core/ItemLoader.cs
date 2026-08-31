using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using TPML.Content.Assets;
using TPML.Content.Engine;
using TPML.Content.UI;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义物品注册与生命周期分发中心
    /// </summary>
    public static class ItemLoader
    {
        public const int ModItemOffset = 6200;
        private static int _nextItemID = ModItemOffset;
        private static readonly Dictionary<int, ModItem> _itemsByType = new Dictionary<int, ModItem>();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Item, ModItem> _modItemInstances = new System.Runtime.CompilerServices.ConditionalWeakTable<Item, ModItem>();
        private static readonly Dictionary<int, string> _displayNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> _tooltips = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> _itemsByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly FieldInfo _assetValueField = typeof(Asset<Texture2D>).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetStateField = typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetNameField = typeof(Asset<Texture2D>).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int ItemCount => _nextItemID;
        public static int NextItemID => _nextItemID;
        public static IReadOnlyCollection<ModItem> Items => _itemsByType.Values;

        public static int Register(ModItem item)
        {
            if (item == null) return 0;

            int type = _nextItemID++;
            item.SetType(type);
            _itemsByType[type] = item;
            _itemsByName[item.FullName] = type;
            _itemsByName[item.Name] = type;

            ModContent.RegisterItemType(item.GetType(), type);

            EnsureArraySizes(type);
            LoadItemTexture(item);

            item.SetStaticDefaults();
            ContentHookDispatcher.RegisterHookInstances(new[] { item });

            try
            {
                Item sample = new Item();
                sample.type = type;
                SetDefaults(sample);
                ContentSamples.ItemsByType[type] = sample;

                if (!string.IsNullOrEmpty(item.FullName))
                {
                    ContentSamples.ItemPersistentIdsByNetIds[type] = item.FullName;
                    ContentSamples.ItemNetIdsByPersistentIds[item.FullName] = type;
                    if (!string.IsNullOrEmpty(item.Name) && !ContentSamples.ItemNetIdsByPersistentIds.ContainsKey(item.Name))
                    {
                        ContentSamples.ItemNetIdsByPersistentIds[item.Name] = type;
                    }
                }
            }
            catch { }

            ModLoader.Log($"[ItemLoader] 成功注册物品: [{item.FullName}] -> ItemID={type}");
            return type;
        }

        public static void EnsureArraySizes(int maxType)
        {
            int required = maxType + 64;

            if (TextureAssets.Item != null && TextureAssets.Item.Length <= required)
            {
                int newLen = Math.Max(required, TextureAssets.Item.Length * 2);
                Array.Resize(ref TextureAssets.Item, newLen);
                Texture2D fallback = TextureAssets.Item[0]?.Value ?? TileLoader.GetFallbackTexture();
                for (int i = 0; i < TextureAssets.Item.Length; i++)
                {
                    if (TextureAssets.Item[i] == null)
                    {
                        var emptyAsset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                        _assetNameField?.SetValue(emptyAsset, string.Empty);
                        _assetValueField?.SetValue(emptyAsset, fallback);
                        _assetStateField?.SetValue(emptyAsset, AssetState.Loaded);
                        TextureAssets.Item[i] = emptyAsset;
                    }
                }
            }

            // 自动扩容 ItemID.Sets 中的所有数组字段
            foreach (FieldInfo field in typeof(ItemID.Sets).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType.IsArray)
                {
                    Array arr = field.GetValue(null) as Array;
                    if (arr != null && arr.Length <= required)
                    {
                        int newLen = Math.Max(required, arr.Length * 2);
                        Array newArr = Array.CreateInstance(field.FieldType.GetElementType(), newLen);
                        Array.Copy(arr, newArr, arr.Length);
                        field.SetValue(null, newArr);
                    }
                }
            }

            // 扩容 ArmorSetBonuses.SetsContaining
            if (ArmorSetBonuses.SetsContaining != null && ArmorSetBonuses.SetsContaining.Length <= required)
            {
                int newLen = Math.Max(required, ArmorSetBonuses.SetsContaining.Length * 2);
                Array.Resize(ref ArmorSetBonuses.SetsContaining, newLen);
                for (int i = 0; i < ArmorSetBonuses.SetsContaining.Length; i++)
                {
                    if (ArmorSetBonuses.SetsContaining[i] == null)
                    {
                        ArmorSetBonuses.SetsContaining[i] = Array.Empty<ArmorSetBonus>();
                    }
                }
            }

            // 自动扩容 PrefixLegacy.ItemSets 中的所有数组字段
            foreach (FieldInfo field in typeof(Terraria.GameContent.Prefixes.PrefixLegacy.ItemSets).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType.IsArray)
                {
                    Array arr = field.GetValue(null) as Array;
                    if (arr != null && arr.Length <= required)
                    {
                        int newLen = Math.Max(required, arr.Length * 2);
                        Array newArr = Array.CreateInstance(field.FieldType.GetElementType(), newLen);
                        Array.Copy(arr, newArr, arr.Length);
                        field.SetValue(null, newArr);
                    }
                }
            }

            // 扩容 Item.staff 与 Item.claw
            if (Item.staff != null && Item.staff.Length <= required)
            {
                int newLen = Math.Max(required, Item.staff.Length * 2);
                Array.Resize(ref Item.staff, newLen);
            }
            if (Item.claw != null && Item.claw.Length <= required)
            {
                int newLen = Math.Max(required, Item.claw.Length * 2);
                Array.Resize(ref Item.claw, newLen);
            }
            if (Item.cachedItemSpawnsByType != null && Item.cachedItemSpawnsByType.Length <= required)
            {
                int newLen = Math.Max(required, Item.cachedItemSpawnsByType.Length * 2);
                int oldLen = Item.cachedItemSpawnsByType.Length;
                Array.Resize(ref Item.cachedItemSpawnsByType, newLen);
                for (int i = oldLen; i < newLen; i++)
                {
                    Item.cachedItemSpawnsByType[i] = -1;
                }
            }

            // 扩容 Main 中的物品数组
            if (Main.itemAnimations != null && Main.itemAnimations.Length <= required)
            {
                int newLen = Math.Max(required, Main.itemAnimations.Length * 2);
                Array.Resize(ref Main.itemAnimations, newLen);
            }
            if (Main.itemFrame != null && Main.itemFrame.Length <= required)
            {
                int newLen = Math.Max(required, Main.itemFrame.Length * 2);
                Array.Resize(ref Main.itemFrame, newLen);
            }
        }

        public static void ReloadTextures()
        {
            foreach (var kvp in _itemsByType)
            {
                LoadItemTexture(kvp.Value);
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

                Assembly asm = item.GetType().Assembly;
                string[] resNames = asm.GetManifestResourceNames();
                string targetRes = null;
                string normalizedTex = texPath?.Replace('/', '.')?.Replace('\\', '.');

                foreach (var res in resNames)
                {
                    if ((!string.IsNullOrEmpty(normalizedTex) && (res.Equals(normalizedTex, StringComparison.OrdinalIgnoreCase) || res.Equals(normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex, StringComparison.OrdinalIgnoreCase))) ||
                        res.Equals($"{item.Name}.png", StringComparison.OrdinalIgnoreCase) ||
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

                if (texture == null && item.Mod != null && !string.IsNullOrEmpty(texPath))
                {
                    string cleanPath = texPath.Replace('\\', '/');
                    if (item.Mod.HasAsset(cleanPath + ".png"))
                    {
                        using (Stream s = item.Mod.GetFileStream(cleanPath + ".png"))
                        {
                            if (s != null) texture = Texture2D.FromStream(device, s);
                        }
                    }
                    else if (item.Mod.HasAsset(cleanPath + ".rawimg"))
                    {
                        byte[] rawBytes = item.Mod.GetFileBytes(cleanPath + ".rawimg");
                        if (rawBytes != null && rawBytes.Length >= 12)
                        {
                            int width = BitConverter.ToInt32(rawBytes, 4);
                            int height = BitConverter.ToInt32(rawBytes, 8);
                            int expectedPixelBytes = width * height * 4;
                            if (width > 0 && height > 0 && rawBytes.Length >= 12 + expectedPixelBytes)
                            {
                                texture = new Texture2D(device, width, height);
                                byte[] pixelData = new byte[expectedPixelBytes];
                                Buffer.BlockCopy(rawBytes, 12, pixelData, 0, expectedPixelBytes);
                                texture.SetData(pixelData);
                            }
                        }
                    }
                }

                if (texture == null)
                {
                    texture = TextureAssets.Item[0]?.Value ?? new Texture2D(device, 16, 16);
                }

                var asset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                _assetNameField?.SetValue(asset, item.FullName);
                _assetValueField?.SetValue(asset, texture);
                _assetStateField?.SetValue(asset, AssetState.Loaded);
                TextureAssets.Item[item.Type] = asset;
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[ItemLoader] 为物品 [{item.FullName}] 加载材质异常: {ex.Message}");
            }
        }

        public static ModItem GetItem(int type)
        {
            _itemsByType.TryGetValue(type, out ModItem item);
            return item;
        }

        public static void SetDisplayName(int type, string name) => _displayNames[type] = name;
        public static string GetDisplayName(int type) => _displayNames.TryGetValue(type, out string name) ? name : string.Empty;

        public static void SetTooltip(int type, string tooltip) => _tooltips[type] = tooltip;
        public static string GetTooltip(int type) => _tooltips.TryGetValue(type, out string tip) ? tip : string.Empty;

        public static void EnsureTextureLoaded(int type)
        {
            if (type <= 0 || type >= TextureAssets.Item.Length)
                return;

            var asset = TextureAssets.Item[type];
            if (asset == null || !asset.IsLoaded || asset.Value == null || asset.Value.Width <= 1 || asset.Value.Height <= 1)
            {
                if (_itemsByType.TryGetValue(type, out ModItem modItem))
                {
                    LoadItemTexture(modItem);
                }
            }
        }

        public static ModItem GetModItem(Item item)
        {
            if (item == null || item.IsAir || item.type < ItemID.Count) return null;
            if (_modItemInstances.TryGetValue(item, out ModItem instance))
            {
                return instance;
            }
            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                ModItem newInst = template.Clone(item);
                newInst.Item = item;
                newInst.SetType(item.type);
                _modItemInstances.Add(item, newInst);
                return newInst;
            }
            return null;
        }

        public static T GetModItem<T>(Item item) where T : ModItem => GetModItem(item) as T;

        public static void OnItemCloned(Item source, Item destination)
        {
            if (source == null || destination == null || source.type < ItemID.Count) return;
            if (_modItemInstances.TryGetValue(source, out ModItem sourceModItem))
            {
                ModItem clonedModItem = sourceModItem.Clone(destination);
                clonedModItem.Item = destination;
                clonedModItem.SetType(destination.type);
                _modItemInstances.Remove(destination);
                _modItemInstances.Add(destination, clonedModItem);
            }
        }

        public static void SetDefaults(Item item)
        {
            if (item == null) return;

            if (_itemsByType.TryGetValue(item.type, out ModItem template))
            {
                item.SetNameOverride(string.Empty);
                ModItem instance = template.Clone(item);
                instance.Item = item;
                instance.SetType(item.type);
                _modItemInstances.Remove(item);
                _modItemInstances.Add(item, instance);
                instance.SetDefaults();

                string name = GetDisplayName(template.Type);
                if (!string.IsNullOrEmpty(name))
                {
                    item.SetNameOverride(name);
                }

                string tooltip = GetTooltip(template.Type);
                if (!string.IsNullOrEmpty(tooltip))
                {
                    string[] lines = tooltip.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    item.ToolTip = Terraria.UI.ItemTooltip.FromHardcodedText(lines);
                }
                else
                {
                    item.ToolTip = Terraria.UI.ItemTooltip.None;
                }

                if (item.stack <= 0)
                {
                    item.stack = 1;
                }

                EnsureTextureLoaded(template.Type);

                foreach (var gItem in ContentHookDispatcher.ActiveGlobalItems)
                {
                    try { gItem.SetDefaults(item); } catch (Exception ex) { ModLoader.Log($"[ItemLoader] GlobalItem.SetDefaults 异常: {ex.Message}"); }
                }
            }
        }

        /// <summary>
        /// 原生 SetDefaults IL 拦截入口：由 Prepatcher 织入 Item.SetDefaults 与 Item.netDefaults 头部
        /// </summary>
        public static bool OnSetDefaultsPrefix(Item item, int type)
        {
            if (item == null) return false;
            ModItem modItem = GetItem(type);
            if (modItem == null) return false;

            item.type = type;
            item.stack = 1;
            item.prefix = 0;
            SetDefaults(item);
            return true;
        }

        public static bool? CanUseItem(Item item, Player player)
        {
            if (item == null || item.IsAir) return null;
            ModItem modItem = GetModItem(item) ?? GetModItem(item.type);
            if (modItem != null)
            {
                bool? canUse = modItem.CanUseItem(player);
                if (canUse == false) return false;
            }

            foreach (var gItem in ContentHookDispatcher.ActiveGlobalItems)
            {
                bool? gCanUse = gItem.CanUseItem(item, player);
                if (gCanUse == false) return false;
            }

            return null;
        }

        public static bool? UseItem(Item item, Player player)
        {
            if (item == null || item.IsAir) return null;
            ModItem modItem = GetModItem(item) ?? GetModItem(item.type);
            if (modItem != null)
            {
                modItem.UseItem(player);
            }

            foreach (var gItem in ContentHookDispatcher.ActiveGlobalItems)
            {
                gItem.UseItem(item, player);
            }

            return null;
        }

        public static void HoldItem(Item item, Player player)
        {
            if (item == null || item.IsAir) return;
            ModItem modItem = GetModItem(item) ?? GetModItem(item.type);
            if (modItem != null)
            {
                modItem.HoldItem(player);
            }

            foreach (var gItem in ContentHookDispatcher.ActiveGlobalItems)
            {
                gItem.HoldItem(item, player);
            }
        }

        public static void UpdateInventory(Item item, Player player)
        {
            if (item == null || item.IsAir) return;
            ModItem modItem = GetModItem(item) ?? GetModItem(item.type);
            if (modItem != null)
            {
                modItem.UpdateInventory(player);
            }

            foreach (var gItem in ContentHookDispatcher.ActiveGlobalItems)
            {
                gItem.UpdateInventory(item, player);
            }
        }

        public static void UpdateEquip(Item item, Player player)
        {
            if (item == null || item.IsAir) return;
            ModItem modItem = GetModItem(item) ?? GetModItem(item.type);
            if (modItem != null)
            {
                modItem.UpdateEquip(player);
            }

            foreach (var gItem in ContentHookDispatcher.ActiveGlobalItems)
            {
                gItem.UpdateEquip(item, player);
            }
        }

        public static void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item == null || item.IsAir || tooltips == null) return;
            ModItem modItem = GetModItem(item) ?? GetModItem(item.type);
            if (modItem != null)
            {
                modItem.ModifyTooltips(tooltips);
            }

            foreach (var gItem in ContentHookDispatcher.ActiveGlobalItems)
            {
                gItem.ModifyTooltips(item, tooltips);
            }
        }

        public static bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (item == null || item.IsAir) return true;
            ModItem modItem = GetModItem(item) ?? GetModItem(item.type);
            if (modItem != null)
            {
                return modItem.Shoot(player, source, position, velocity, type, damage, knockback);
            }
            return true;
        }

        public static ModItem GetModItem(int type)
        {
            _itemsByType.TryGetValue(type, out ModItem item);
            return item;
        }

        public static int ItemType(string modName, string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return 0;
            if (!string.IsNullOrEmpty(modName) && _itemsByName.TryGetValue($"{modName}/{itemName}", out int type))
            {
                return type;
            }
            if (_itemsByName.TryGetValue(itemName, out int fallbackType))
            {
                return fallbackType;
            }
            return 0;
        }

        public static int ItemType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            if (_itemsByName.TryGetValue(fullName, out int type)) return type;
            int idx = fullName.IndexOf('/');
            if (idx >= 0 && idx < fullName.Length - 1)
            {
                string shortName = fullName.Substring(idx + 1);
                if (_itemsByName.TryGetValue(shortName, out int shortType)) return shortType;
            }
            return 0;
        }

        public static void Clear()
        {
            _itemsByType.Clear();
            _displayNames.Clear();
            _tooltips.Clear();
            _itemsByName.Clear();
            _nextItemID = ModItemOffset;
        }
    }
}
