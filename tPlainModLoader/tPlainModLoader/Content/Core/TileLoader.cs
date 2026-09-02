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
using TPML.Content.Assets;
using TPML.Content.Core;
using TPML.Content.Engine;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生物块 (ModTile) 注册、数组扩容与生命周期分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class TileLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("TileLoader");

        public static readonly int ModTileOffset = TileID.Count;
        private static int _nextTileID = TileID.Count;
        private static readonly Dictionary<int, ModTile> _tilesByType = new Dictionary<int, ModTile>();
        private static readonly Dictionary<string, int> _tilesByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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
            On_Player.PickTile += Hook_PickTile;
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
                        TextureAssets.Tile[i] = AssetFactory.CreateLoaded(fallback, string.Empty);
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
                        TextureAssets.HighlightMask[i] = AssetFactory.CreateLoaded(fallback, string.Empty);
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
            ArrayResizer.ResizeSets(typeof(TileID.Sets), required, 693);

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
                System.Runtime.CompilerServices.Unsafe.AsRef(in Main.SceneMetrics._tileCounts) = newArr;
            }

            // 5. 扩容配方系统与玩家临近物块数组 (防止 UpdateRecipeList 访问 player.adjTile 或 TileUsedInRecipes 发生 IndexOutOfRangeException)
            if (Recipe.TileUsedInRecipes != null && Recipe.TileUsedInRecipes.Length <= required)
            {
                int newLen = Math.Max(required, Recipe.TileUsedInRecipes.Length * 2);
                Array.Resize(ref Recipe.TileUsedInRecipes, newLen);
            }

            if (Recipe.TileCountsAs != null && Recipe.TileCountsAs.Length <= required)
            {
                int newLen = Math.Max(required, Recipe.TileCountsAs.Length * 2);
                Array.Resize(ref Recipe.TileCountsAs, newLen);
            }

            ScanAndResizeStaticArrays(typeof(Recipe), required);
            ScanAndResizeStaticArrays(typeof(Terraria.GameContent.UI.NewCraftingUI), required);

            if (Main.player != null)
            {
                for (int i = 0; i < Main.player.Length; i++)
                {
                    var p = Main.player[i];
                    if (p != null && (p.adjTile == null || p.adjTile.Length <= required))
                    {
                        int curLen = p.adjTile?.Length ?? 0;
                        int newLen = Math.Max(required, Math.Max(curLen * 2, 800));
                        bool[] newAdj = new bool[newLen];
                        if (p.adjTile != null)
                        {
                            Array.Copy(p.adjTile, newAdj, p.adjTile.Length);
                        }
                        p.adjTile = newAdj;
                    }
                }
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
            EnsureArraySizes(tile.Type);
            string[] extraAliases = tile.Name.Equals("FishingMachineTile", StringComparison.OrdinalIgnoreCase)
                ? new[] { $"{tile.Name}Tile", "AutofisherTile" }
                : new[] { $"{tile.Name}Tile" };

            ContentTextureLoader.Load(
                tile.Mod,
                tile.GetType().Assembly,
                tile.Texture,
                tile.Name,
                tile.FullName,
                tile.Type,
                asset => TextureAssets.Tile[tile.Type] = asset,
                () => GetFallbackTexture(),
                assetNameOverride: $"ModTile_{tile.Name}",
                extraAliases: extraAliases
            );
        }

        public static void KillMultiTileStructure(int i, int j, int type, bool noItem = false)
        {
            TileObjectData tileData = TileObjectData.GetTileData(type, 0);
            if (tileData == null) return;

            Tile originTile = Framing.GetTileSafely(i, j);
            int frameX = originTile.frameX;
            int frameY = originTile.frameY;
            tileData = TileObjectData.GetTileData(originTile) ?? tileData;

            int num4 = frameX % tileData.CoordinateFullWidth;
            int num5 = frameY % tileData.CoordinateFullHeight;
            int num6 = num4 / (tileData.CoordinateWidth + tileData.CoordinatePadding);
            int k = 0;
            for (int num7 = num5; k + 1 < tileData.Height && tileData.CoordinateHeights != null && k < tileData.CoordinateHeights.Length && num7 - tileData.CoordinateHeights[k] - tileData.CoordinatePadding >= 0; k++)
            {
                num7 -= tileData.CoordinateHeights[k] + tileData.CoordinatePadding;
            }
            int originX = i - num6;
            int originY = j - k;

            WorldGen.destroyObject = true;

            // 1. 掉落物块本体物品（仅在多方块原点掉落 1 次）
            if (!noItem && (tileData.Width != 1 || tileData.Height != 1) && _tilesByType.TryGetValue(type, out ModTile mt))
            {
                int dropItem = mt.GetItemDrop(type, frameX, frameY);
                if (dropItem > 0 && mt.Drop(originX, originY))
                {
                    IEntitySource src = new EntitySource_TileBreak(originX, originY);
                    Item.NewItem(src, new Vector2(originX * 16, originY * 16), dropItem, 1);
                }
            }

            // 2. 清空整片多方块区域所有关联图格
            for (int n = originX; n < originX + tileData.Width; n++)
            {
                for (int num8 = originY; num8 < originY + tileData.Height; num8++)
                {
                    Tile t = Framing.GetTileSafely(n, num8);
                    if (t.type == type && t.active())
                    {
                        WorldGen.KillTile(n, num8);
                        t.active(false);
                        t.type = 0;
                        t.frameX = 0;
                        t.frameY = 0;
                    }
                }
            }

            // 3. 触发 KillMultiTile 释放战利品与实体清理
            if (_tilesByType.TryGetValue(type, out ModTile modTile))
            {
                modTile.KillMultiTile(originX, originY, frameX - num4, frameY - num5);
            }
            foreach (var mte in TileEntityLoader.Entities)
            {
                mte.Kill(originX, originY);
            }

            WorldGen.destroyObject = false;

            // 4. 网络同步与周围物理更新
            if (Main.netMode != 0)
            {
                NetMessage.SendTileSquare(-1, originX, originY, tileData.Width, tileData.Height);
            }
            for (int num9 = originX - 1; num9 < originX + tileData.Width + 2; num9++)
            {
                for (int num10 = originY - 1; num10 < originY + tileData.Height + 2; num10++)
                {
                    WorldGen.TileFrame(num9, num10);
                }
            }
            TileObject.objectPreview.Active = false;
        }

        public static void CheckModTile(int i, int j, int type)
        {
            if (type < ModTileOffset || WorldGen.destroyObject)
            {
                return;
            }
            TileObjectData tileData = TileObjectData.GetTileData(type, 0);
            if (tileData == null)
            {
                return;
            }
            Tile originTile = Framing.GetTileSafely(i, j);
            if (!originTile.active() || originTile.type != type) return;

            int frameX = originTile.frameX;
            int frameY = originTile.frameY;
            int num = frameX / tileData.CoordinateFullWidth;
            int num2 = frameY / tileData.CoordinateFullHeight;
            int num3 = tileData.StyleWrapLimit;
            if (num3 == 0)
            {
                num3 = 1;
            }
            int styleLineSkip = tileData.StyleLineSkip;
            if (styleLineSkip == 0) styleLineSkip = 1;
            int styleMultiplier = tileData.StyleMultiplier;
            if (styleMultiplier == 0) styleMultiplier = 1;
            int style = (tileData.StyleHorizontal ? (num2 / styleLineSkip * num3 + num) : (num / styleLineSkip * num3 + num2)) / styleMultiplier;
            tileData = TileObjectData.GetTileData(originTile) ?? tileData;

            int num4 = frameX % tileData.CoordinateFullWidth;
            int num5 = frameY % tileData.CoordinateFullHeight;
            int num6 = num4 / (tileData.CoordinateWidth + tileData.CoordinatePadding);
            int k = 0;
            for (int num7 = num5; k + 1 < tileData.Height && tileData.CoordinateHeights != null && k < tileData.CoordinateHeights.Length && num7 - tileData.CoordinateHeights[k] - tileData.CoordinatePadding >= 0; k++)
            {
                num7 -= tileData.CoordinateHeights[k] + tileData.CoordinatePadding;
            }
            int originX = i - num6;
            int originY = j - k;
            int x2 = originX + tileData.Origin.X;
            int y2 = originY + tileData.Origin.Y;

            // 1. 检查多方块内部所有格子是否依然完整存在
            bool missingPart = false;
            for (int l = originX; l < originX + tileData.Width; l++)
            {
                for (int m = originY; m < originY + tileData.Height; m++)
                {
                    Tile checkTile = Framing.GetTileSafely(l, m);
                    if (!checkTile.active() || checkTile.type != type)
                    {
                        missingPart = true;
                        break;
                    }
                }
                if (missingPart) break;
            }

            // 2. 100% 对齐 tML 官方 CheckModTile：直接调用带 checkStay 的 CanPlace，完整校验所有 Anchor 与支撑
            if (missingPart || !TileObjectExt.CanPlace(x2, y2, type, style, 0, out var _, onlyCheck: true, null, checkStay: true))
            {
                KillMultiTileStructure(originX, originY, type, false);
            }
            TileObject.objectPreview.Active = false;
        }

        #region MonoMod Hooks

        private static void Hook_KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            if (tile.active() && tile.type >= ModTileOffset && _tilesByType.TryGetValue(tile.type, out ModTile modTile))
            {
                modTile.KillTile(i, j, ref fail, ref effectOnly, ref noItem);

                if (!fail && !effectOnly && !WorldGen.destroyObject)
                {
                    TileObjectData data = TileObjectData.GetTileData(tile.type, 0);
                    if (data != null && (data.Width > 1 || data.Height > 1))
                    {
                        KillMultiTileStructure(i, j, tile.type, noItem);
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
                if (!noBreak)
                {
                    CheckModTile(i, j, tile.type);
                }
            }

            orig(i, j, resetFrame, noBreak);
        }

        private static void Hook_PickTile(On_Player.orig_PickTile orig, Player self, int x, int y, int pickPower, int dealDamageAsIfBaseNumberIs)
        {
            orig(self, x, y, pickPower, dealDamageAsIfBaseNumberIs);
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
            catch (Exception ex)
            {
                Logger.Warn($"ResetState End 异常: {ex.Message}");
            }

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
            catch (Exception ex)
            {
                Logger.Warn($"ResetState SetRenderTarget 异常: {ex.Message}");
            }
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

        private static void Hook_SceneMetrics_Scan(On_SceneMetrics.orig_Scan orig, SceneMetrics self, SceneMetricsScanSettings settings)
        {
            if (self != null && self._tileCounts != null && self._tileCounts.Length <= TileCount + 64)
            {
                int newLen = Math.Max(TileCount + 64, self._tileCounts.Length * 2);
                int[] newArr = new int[newLen];
                Array.Copy(self._tileCounts, newArr, self._tileCounts.Length);
                System.Runtime.CompilerServices.Unsafe.AsRef(in self._tileCounts) = newArr;
            }
            orig(self, settings);
        }

        #endregion

        public static void Clear()
        {
            ContentTextureLoader.ClearAssets(TextureAssets.Tile, ModTileOffset, _nextTileID, TileLoader.GetFallbackTexture());
            _tilesByType.Clear();
            _tilesByName.Clear();
            _nextTileID = ModTileOffset;
        }
    }
}
