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
using Terraria.ObjectData;
using TPML.Content.Engine;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生物块 (ModTile) 注册、数组扩容与生命周期分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class TileLoader
    {
        public static readonly int ModTileOffset = TileID.Count;
        private static int _nextTileID = TileID.Count;
        private static readonly Dictionary<int, ModTile> _tilesByType = new Dictionary<int, ModTile>();
        private static readonly Dictionary<string, int> _tilesByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly FieldInfo _assetValueField = typeof(Asset<Texture2D>).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetStateField = typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetNameField = typeof(Asset<Texture2D>).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool _hooksInitialized = false;

        public static int TileCount => _nextTileID;
        public static IReadOnlyCollection<ModTile> Tiles => _tilesByType.Values;

        public static void InitializeHooks()
        {
            if (_hooksInitialized) return;

            On_Main.DoDraw += Hook_DoDraw;
            On_Main.EndDraw += Hook_EndDraw;
            On_WorldGen.KillTile += Hook_KillTile;
            On_WorldGen.TileFrame += Hook_TileFrame;
            On_Player.TileInteractionsCheck += Hook_TileInteractionsCheck;
            On_Player.TileInteractionsMouseOver += Hook_TileInteractionsMouseOver;
            On_SceneMetrics.Scan += Hook_SceneMetrics_Scan;

            _hooksInitialized = true;
        }

        public static int Register(ModTile tile)
        {
            if (tile == null) return 0;

            InitializeHooks();

            int type = _nextTileID++;
            tile.Type = type;
            _tilesByType[type] = tile;
            _tilesByName[tile.FullName] = type;
            _tilesByName[tile.Name] = type;

            ModContent.RegisterTileType(tile.GetType(), type);

            EnsureArraySizes(type);
            LoadTileTexture(tile);

            bool wasReadOnly = TileObjectData.readOnlyData;
            try
            {
                TileObjectData.readOnlyData = false;
                tile.SetStaticDefaults();
                tile.SetDefaults();
            }
            finally
            {
                TileObjectData.readOnlyData = wasReadOnly;
            }

            ContentHookDispatcher.RegisterHookInstances(new[] { tile });

            ModLoader.Log($"[TileLoader] 成功注册物块: [{tile.FullName}] -> TileID={type}");
            return type;
        }

        public static ModTile GetTile(int type)
        {
            _tilesByType.TryGetValue(type, out ModTile tile);
            return tile;
        }

        public static T GetTile<T>() where T : ModTile
        {
            return ModContent.GetInstance<T>();
        }

        public static int TileType<T>() where T : ModTile
        {
            return ModContent.TileType<T>();
        }

        public static int TileType(string modName, string tileName)
        {
            if (string.IsNullOrEmpty(tileName)) return 0;
            if (!string.IsNullOrEmpty(modName) && _tilesByName.TryGetValue($"{modName}/{tileName}", out int type))
            {
                return type;
            }
            if (_tilesByName.TryGetValue(tileName, out int fallbackType))
            {
                return fallbackType;
            }
            return 0;
        }

        public static int TileType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            if (_tilesByName.TryGetValue(fullName, out int type)) return type;
            int idx = fullName.IndexOf('/');
            if (idx >= 0 && idx < fullName.Length - 1)
            {
                string shortName = fullName.Substring(idx + 1);
                if (_tilesByName.TryGetValue(shortName, out int shortType)) return shortType;
            }
            return 0;
        }

        public static void EnsureArraySizes(int maxType)
        {
            int required = maxType + 64;

            // 1. 强类型扩容核心资产与属性数组
            if (TextureAssets.Tile != null && TextureAssets.Tile.Length <= required)
            {
                int newLen = Math.Max(required, TextureAssets.Tile.Length * 2);
                Array.Resize(ref TextureAssets.Tile, newLen);
                Texture2D fallback = TextureAssets.Tile[0]?.Value ?? GetFallbackTexture();
                for (int i = 0; i < TextureAssets.Tile.Length; i++)
                {
                    if (TextureAssets.Tile[i] == null)
                    {
                        var emptyAsset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                        _assetNameField?.SetValue(emptyAsset, string.Empty);
                        _assetValueField?.SetValue(emptyAsset, fallback);
                        _assetStateField?.SetValue(emptyAsset, AssetState.Loaded);
                        TextureAssets.Tile[i] = emptyAsset;
                    }
                }
            }

            if (TextureAssets.HighlightMask != null && TextureAssets.HighlightMask.Length <= required)
            {
                int newLen = Math.Max(required, TextureAssets.HighlightMask.Length * 2);
                Array.Resize(ref TextureAssets.HighlightMask, newLen);
                Texture2D fallback = GetFallbackTexture();
                for (int i = 0; i < TextureAssets.HighlightMask.Length; i++)
                {
                    if (TextureAssets.HighlightMask[i] == null)
                    {
                        var emptyAsset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                        _assetNameField?.SetValue(emptyAsset, string.Empty);
                        _assetValueField?.SetValue(emptyAsset, fallback);
                        _assetStateField?.SetValue(emptyAsset, AssetState.Loaded);
                        TextureAssets.HighlightMask[i] = emptyAsset;
                    }
                }
            }

            // 扩容 TileObjectData._data
            if (TileObjectData._data != null)
            {
                while (TileObjectData._data.Count <= required)
                {
                    TileObjectData._data.Add(null);
                }
            }

            // 2. 递归扩容 TileID.Sets 及所有嵌套类 (如 Wiring, Conversion) 中的静态数组字段
            ResizeSetsClass(typeof(TileID.Sets), required, 693);

            // 3. 全量反射扫描 Main、WorldGen 与 MapHelper 中与 Tile 关联的静态一维/二维数组并统一扩容
            ScanAndResizeStaticArrays(typeof(Main), required);
            ScanAndResizeStaticArrays(typeof(WorldGen), required);
            ScanAndResizeStaticArrays(typeof(Terraria.Map.MapHelper), required);

            // 4. 特殊处理 MapHelper 与 Main.tileGlowMask
            if (Terraria.Map.MapHelper.tileLookup != null && Terraria.Map.MapHelper.tileLookup.Length <= required)
            {
                int newLen = Math.Max(required, Terraria.Map.MapHelper.tileLookup.Length * 2);
                Array.Resize(ref Terraria.Map.MapHelper.tileLookup, newLen);
            }
            if (Terraria.Map.MapHelper.tileOptionCounts != null && Terraria.Map.MapHelper.tileOptionCounts.Length <= required)
            {
                int newLen = Math.Max(required, Terraria.Map.MapHelper.tileOptionCounts.Length * 2);
                Array.Resize(ref Terraria.Map.MapHelper.tileOptionCounts, newLen);
            }
            if (Terraria.Map.MapHelper.snowTypes != null && Terraria.Map.MapHelper.snowTypes.Length <= required)
            {
                int newLen = Math.Max(required, Terraria.Map.MapHelper.snowTypes.Length * 2);
                Array.Resize(ref Terraria.Map.MapHelper.snowTypes, newLen);
            }

            if (Main.SceneMetrics?._tileCounts != null && Main.SceneMetrics._tileCounts.Length <= required)
            {
                int newLen = Math.Max(required, Main.SceneMetrics._tileCounts.Length * 2);
                int[] newArr = new int[newLen];
                Array.Copy(Main.SceneMetrics._tileCounts, newArr, Main.SceneMetrics._tileCounts.Length);
                _tileCountsField?.SetValue(Main.SceneMetrics, newArr);
            }

            if (Main.tileGlowMask != null)
            {
                for (int i = 693; i < Main.tileGlowMask.Length; i++)
                {
                    if (Main.tileGlowMask[i] == 0) Main.tileGlowMask[i] = -1;
                }
            }
        }

        private static void ResizeSetsClass(Type type, int required, int minMatchLen)
        {
            if (type == null) return;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.FieldType.IsArray && field.FieldType.GetArrayRank() == 1)
                {
                    Array arr = field.GetValue(null) as Array;
                    if (arr != null && arr.Length >= minMatchLen && arr.Length <= required)
                    {
                        int newLen = Math.Max(required, arr.Length * 2);
                        Array newArr = Array.CreateInstance(field.FieldType.GetElementType(), newLen);
                        Array.Copy(arr, newArr, arr.Length);
                        field.SetValue(null, newArr);
                    }
                }
            }

            foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                ResizeSetsClass(nested, required, minMatchLen);
            }
        }

        private static void ScanAndResizeStaticArrays(Type targetType, int required)
        {
            foreach (FieldInfo field in targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (!field.FieldType.IsArray) continue;

                if (field.FieldType.GetArrayRank() == 1)
                {
                    Array arr = field.GetValue(null) as Array;
                    if (arr != null && arr.Length >= 693 && arr.Length <= required)
                    {
                        int newLen = Math.Max(required, arr.Length * 2);
                        Array newArr = Array.CreateInstance(field.FieldType.GetElementType(), newLen);
                        Array.Copy(arr, newArr, arr.Length);
                        field.SetValue(null, newArr);
                    }
                }
                else if (field.FieldType.GetArrayRank() == 2)
                {
                    Array arr2D = field.GetValue(null) as Array;
                    if (arr2D != null && arr2D.GetLength(0) >= 693 && arr2D.GetLength(0) <= required)
                    {
                        int oldRows = arr2D.GetLength(0);
                        int oldCols = arr2D.GetLength(1);
                        int newLen = Math.Max(required, oldRows * 2);
                        Type elemType = field.FieldType.GetElementType();
                        Array newArr2D = Array.CreateInstance(elemType, newLen, newLen);
                        for (int r = 0; r < oldRows; r++)
                        {
                            for (int c = 0; c < oldCols; c++)
                            {
                                newArr2D.SetValue(arr2D.GetValue(r, c), r, c);
                            }
                        }
                        field.SetValue(null, newArr2D);
                    }
                }
            }
        }

        public static void LoadTileTexture(ModTile tile)
        {
            try
            {
                EnsureArraySizes(tile.Type);
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;

                if (device == null) return;

                Texture2D texture = null;
                string texPath = tile.Texture;
                Assembly asm = tile.GetType().Assembly;
                string[] resNames = asm.GetManifestResourceNames();
                string targetRes = null;
                string tileName = tile.Name;

                foreach (var res in resNames)
                {
                    if (res.Equals($"{tileName}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{tileName}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.Equals($"{tileName}Tile.png", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{tileName}Tile.png", StringComparison.OrdinalIgnoreCase) ||
                        res.Equals($"{tileName}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{tileName}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                        (tileName.Equals("FishingMachineTile", StringComparison.OrdinalIgnoreCase) && res.EndsWith("AutofisherTile.png", StringComparison.OrdinalIgnoreCase)))
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
                            texture = Texture2D.FromStream(device, stream);
                        }
                    }
                }

                if (texture == null && tile.Mod != null)
                {
                    string cleanPath = texPath.Replace('\\', '/');
                    if (tile.Mod.HasAsset(cleanPath + ".png"))
                    {
                        using (Stream s = tile.Mod.GetFileStream(cleanPath + ".png"))
                        {
                            if (s != null) texture = Texture2D.FromStream(device, s);
                        }
                    }
                }

                if (texture == null)
                {
                    texture = new Texture2D(device, 16, 16);
                    Color[] data = new Color[16 * 16];
                    for (int i = 0; i < data.Length; i++) data[i] = Color.Magenta;
                    texture.SetData(data);
                }

                var asset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                _assetNameField?.SetValue(asset, $"ModTile_{tile.Name}");
                _assetValueField?.SetValue(asset, texture);
                _assetStateField?.SetValue(asset, AssetState.Loaded);

                TextureAssets.Tile[tile.Type] = asset;
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[TileLoader] 为物块 [{tile.FullName}] 加载材质异常: {ex.Message}");
            }
        }

        #region MonoMod Hooks

        private static void Hook_KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            if (tile.active() && tile.type >= ModTileOffset && _tilesByType.TryGetValue(tile.type, out ModTile modTile))
            {
                modTile.KillTile(i, j, ref fail, ref effectOnly, ref noItem);

                if (!fail && !effectOnly)
                {
                    TileObjectData data = TileObjectData.GetTileData(tile.type, 0);
                    if (data != null && (data.Width > 1 || data.Height > 1))
                    {
                        int fullWidth = data.Width * (data.CoordinateWidth + data.CoordinatePadding);
                        int partX = fullWidth > 0 ? ((tile.frameX % fullWidth) / (data.CoordinateWidth + data.CoordinatePadding)) : 0;

                        int fullHeight = 0;
                        if (data.CoordinateHeights != null && data.CoordinateHeights.Length > 0)
                        {
                            for (int h = 0; h < data.CoordinateHeights.Length; h++)
                            {
                                fullHeight += data.CoordinateHeights[h] + data.CoordinatePadding;
                            }
                        }
                        else
                        {
                            fullHeight = data.Height * 18;
                        }

                        int partY = fullHeight > 0 ? ((tile.frameY % fullHeight) / (data.CoordinateHeights != null && data.CoordinateHeights.Length > 0 ? (data.CoordinateHeights[0] + data.CoordinatePadding) : 18)) : 0;
                        int originX = i - partX;
                        int originY = j - partY;

                        // 1. 触发 KillMultiTile 释放战利品与实体清理
                        modTile.KillMultiTile(originX, originY, tile.frameX, tile.frameY);
                        foreach (var mte in TileEntityLoader.Entities)
                        {
                            mte.Kill(originX, originY);
                        }

                        // 2. 掉落物块本体物品（仅在整体破坏时掉落 1 次）
                        int dropItem = modTile.GetItemDrop(tile.type, tile.frameX, tile.frameY);
                        if (!noItem && dropItem > 0 && modTile.Drop(originX, originY))
                        {
                            IEntitySource src = new EntitySource_TileBreak(originX, originY);
                            Item.NewItem(src, new Vector2(originX * 16, originY * 16), dropItem, 1);
                        }

                        // 3. 一键清空整片多方块的所有关联图格，杜绝一格一格残破
                        for (int ox = 0; ox < data.Width; ox++)
                        {
                            for (int oy = 0; oy < data.Height; oy++)
                            {
                                int tx = originX + ox;
                                int ty = originY + oy;
                                Tile targetTile = Framing.GetTileSafely(tx, ty);
                                if (targetTile.active() && targetTile.type == tile.type)
                                {
                                    targetTile.active(false);
                                    targetTile.type = 0;
                                    targetTile.frameX = 0;
                                    targetTile.frameY = 0;
                                }
                            }
                        }

                        WorldGen.destroyObject = true;
                        NetMessage.SendTileSquare(-1, originX, originY, data.Width, data.Height);
                        return;
                    }
                    else
                    {
                        // 单格物块常规掉落
                        int dropItem = modTile.GetItemDrop(tile.type, tile.frameX, tile.frameY);
                        if (!noItem && dropItem > 0 && modTile.Drop(i, j))
                        {
                            IEntitySource src = new EntitySource_TileBreak(i, j);
                            Item.NewItem(src, new Vector2(i * 16, j * 16), dropItem, 1);
                        }
                    }
                }
            }

            orig(i, j, fail, effectOnly, noItem);
        }

        private static void Hook_TileFrame(On_WorldGen.orig_TileFrame orig, int i, int j, bool resetFrame, bool noBreak)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            if (tile.active() && tile.type >= ModTileOffset && _tilesByType.TryGetValue(tile.type, out ModTile modTile))
            {
                if (!modTile.TileFrame(i, j, ref resetFrame, ref noBreak))
                {
                    return;
                }
            }

            orig(i, j, resetFrame, noBreak);
        }

        private static void Hook_TileInteractionsCheck(On_Player.orig_TileInteractionsCheck orig, Player self, int myX, int myY)
        {
            Tile tile = Framing.GetTileSafely(myX, myY);
            if (tile.active() && tile.type >= ModTileOffset && _tilesByType.TryGetValue(tile.type, out ModTile modTile))
            {
                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    if (modTile.RightClick(myX, myY))
                    {
                        Main.mouseRightRelease = false;
                        return;
                    }
                }
            }

            orig(self, myX, myY);
        }

        private static void Hook_TileInteractionsMouseOver(On_Player.orig_TileInteractionsMouseOver orig, Player self, int myX, int myY)
        {
            Tile tile = Framing.GetTileSafely(myX, myY);
            if (tile.active() && tile.type >= ModTileOffset && _tilesByType.TryGetValue(tile.type, out ModTile modTile))
            {
                modTile.MouseOver(myX, myY);
            }

            orig(self, myX, myY);
        }

        private static readonly FieldInfo _inBeginField = typeof(SpriteBatch).GetField("inBeginEndPair", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                                         typeof(SpriteBatch).GetField("inBegin", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                                         typeof(SpriteBatch).GetField("_inBegin", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Texture2D _fallbackTexture;

        public static Texture2D GetFallbackTexture()
        {
            if (_fallbackTexture == null || _fallbackTexture.IsDisposed)
            {
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;
                if (device != null)
                {
                    _fallbackTexture = new Texture2D(device, 16, 16);
                    Color[] data = new Color[16 * 16];
                    for (int i = 0; i < data.Length; i++) data[i] = Color.Magenta;
                    _fallbackTexture.SetData(data);
                }
            }
            return _fallbackTexture;
        }

        public static void ResetState()
        {
            try
            {
                if (Main.spriteBatch != null)
                {
                    bool inBegin = false;
                    if (_inBeginField != null)
                    {
                        inBegin = (bool)_inBeginField.GetValue(Main.spriteBatch);
                    }
                    if (inBegin)
                    {
                        Main.spriteBatch.End();
                    }
                }
            }
            catch { }

            try
            {
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;
                if (device != null && device.GetRenderTargets().Length > 0)
                {
                    device.SetRenderTarget(null);
                }
            }
            catch { }
        }

        public static void ResetSpriteBatchIfInBegin()
        {
            ResetState();
        }

        private static DateTime _lastDoDrawErrorLogTime = DateTime.MinValue;
        private static DateTime _lastEndDrawErrorLogTime = DateTime.MinValue;

        private static void Hook_DoDraw(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
        {
            ResetState();
            try
            {
                orig(self, gameTime);
            }
            catch (Exception ex)
            {
                if ((DateTime.Now - _lastDoDrawErrorLogTime).TotalSeconds >= 5)
                {
                    _lastDoDrawErrorLogTime = DateTime.Now;
                    ModLoader.Log($"[TileLoader] Main.DoDraw 发生未捕获异常: {ex}");
                }
                ResetState();
            }
        }

        private static void Hook_EndDraw(On_Main.orig_EndDraw orig, Main self)
        {
            ResetState();
            try
            {
                orig(self);
            }
            catch (Exception ex)
            {
                if ((DateTime.Now - _lastEndDrawErrorLogTime).TotalSeconds >= 5)
                {
                    _lastEndDrawErrorLogTime = DateTime.Now;
                    ModLoader.Log($"[TileLoader] Main.EndDraw 发生异常: {ex}");
                }
                ResetState();
            }
        }

        private static readonly FieldInfo _tileCountsField = typeof(SceneMetrics).GetField("_tileCounts", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static void Hook_SceneMetrics_Scan(On_SceneMetrics.orig_Scan orig, SceneMetrics self, SceneMetricsScanSettings settings)
        {
            if (self != null && self._tileCounts != null && self._tileCounts.Length <= TileCount + 64)
            {
                int newLen = Math.Max(TileCount + 64, self._tileCounts.Length * 2);
                int[] newArr = new int[newLen];
                Array.Copy(self._tileCounts, newArr, self._tileCounts.Length);
                _tileCountsField?.SetValue(self, newArr);
            }
            orig(self, settings);
        }

        #endregion

        public static void Clear()
        {
            _tilesByType.Clear();
            _tilesByName.Clear();
            _nextTileID = ModTileOffset;
        }
    }
}
