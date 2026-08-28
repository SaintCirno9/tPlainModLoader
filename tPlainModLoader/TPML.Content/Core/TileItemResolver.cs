using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace TPML.Content.Core
{
    /// <summary>
    /// 图格/背景墙与物品之间的统一高性能反向映射解析器
    /// 完整对齐原版 TileObjectData 的 StyleLineSkip / StyleMultiplier / StyleWrapLimit 计算体系，
    /// 完美支持灰烬木等高索引变种家具、开门/关门反查与开门蓝图自动规范化。
    /// 作者: SaintCirno9
    /// </summary>
    public static class TileItemResolver
    {
        private static readonly Dictionary<int, int> TileItemCache = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> WallItemCache = new Dictionary<int, int>();
        private static readonly object SyncLock = new object();
        private static bool isInitialized = false;

        /// <summary>
        /// 预加载并初始化常用映射字典与缓存
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;
            lock (SyncLock)
            {
                if (isInitialized) return;
                TileItemCache.Clear();
                WallItemCache.Clear();

                if (ContentSamples.ItemsByType != null && ContentSamples.ItemsByType.Count > 0)
                {
                    foreach (var kvp in ContentSamples.ItemsByType)
                    {
                        Item item = kvp.Value;
                        if (item == null || item.type <= 0) continue;

                        if (item.createTile >= 0)
                        {
                            int key = (item.createTile << 16) | (item.placeStyle & 0xFFFF);
                            if (!TileItemCache.ContainsKey(key))
                            {
                                TileItemCache[key] = item.type;
                            }

                            // 若为闭门物品，同时注册至开门（TileID.OpenDoor）映射，确保开门状态 O(1) 命中
                            if (item.createTile == TileID.ClosedDoor)
                            {
                                int openKey = ((int)TileID.OpenDoor << 16) | (item.placeStyle & 0xFFFF);
                                if (!TileItemCache.ContainsKey(openKey))
                                {
                                    TileItemCache[openKey] = item.type;
                                }
                            }
                        }

                        if (item.createWall > 0)
                        {
                            if (!WallItemCache.ContainsKey(item.createWall))
                            {
                                WallItemCache[item.createWall] = item.type;
                            }
                        }
                    }
                }

                isInitialized = true;
            }
        }

        /// <summary>
        /// 根据图格类型与切片帧坐标，完全遵循原版 TileObjectData 计算家具/物块的样式 Style
        /// </summary>
        public static int CalculateTileStyle(int tileType, int frameX, int frameY)
        {
            if (tileType < 0) return 0;

            // 1. 平台：每 18 像素为一种 Style
            if (tileType == TileID.Platforms)
            {
                return frameY / 18;
            }

            // 2. 闭合的门（TileID 10）：每列 3 个变种（54px），每大列 36 扇门（1944px）
            if (tileType == TileID.ClosedDoor)
            {
                return (frameY / 54) + 36 * (frameX / 54);
            }

            // 3. 打开的门（TileID 11）：每扇门宽 2 格（36px），左右 2 方向（72px），每大列 36 扇门（1944px）
            if (tileType == TileID.OpenDoor)
            {
                return (frameY / 54) + 36 * (frameX / 72);
            }

            // 4. 标准 TileObjectData 算法（支持 StyleLineSkip / StyleMultiplier / StyleWrapLimit 等）
            TileObjectData data = TileObjectData.GetTileData(tileType, 0);
            if (data == null) return 0;

            int fullW = data.CoordinateFullWidth > 0 ? data.CoordinateFullWidth : 18;
            int fullH = data.CoordinateFullHeight > 0 ? data.CoordinateFullHeight : 18;
            if (fullW <= 0 || fullH <= 0) return 0;

            int num = frameX / fullW;
            int num2 = frameY / fullH;
            int wrap = data.StyleWrapLimit > 0 ? data.StyleWrapLimit : 1;
            int num4 = (!data.StyleHorizontal) ? (num * wrap + num2) : (num2 * wrap + num);
            int mult = data.StyleMultiplier > 0 ? data.StyleMultiplier : 1;
            int num5 = num4 / mult;

            int lineSkip = data.StyleLineSkip;
            if (lineSkip > 1)
            {
                if (data.StyleHorizontal)
                {
                    num5 = (num2 / lineSkip) * wrap + num;
                }
                else
                {
                    num5 = (num / lineSkip) * wrap + num2;
                }
            }

            return num5;
        }

        /// <summary>
        /// 根据图格类型与切片帧坐标，精确解析对应的放置物品 ID（支持不同材质/样式的家具与开门反查）
        /// </summary>
        public static int GetTileItemId(int tileType, int frameX = 0, int frameY = 0)
        {
            if (tileType < 0) return 0;
            if (!isInitialized) Initialize();

            int style = CalculateTileStyle(tileType, frameX, frameY);
            int lookupTile = (tileType == TileID.OpenDoor) ? TileID.ClosedDoor : tileType;
            int key = (tileType << 16) | (style & 0xFFFF);

            lock (SyncLock)
            {
                if (TileItemCache.TryGetValue(key, out int cached)) return cached;
            }

            // 1. 优先精确匹配：createTile == lookupTile 且 placeStyle == style
            if (ContentSamples.ItemsByType != null)
            {
                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it != null && it.createTile == lookupTile && it.placeStyle == style)
                    {
                        lock (SyncLock) { TileItemCache[key] = it.type; }
                        return it.type;
                    }
                }
            }

            for (int i = 1; i < ItemID.Count; i++)
            {
                Item item = ContentSamples.ItemsByType != null && ContentSamples.ItemsByType.TryGetValue(i, out var it) ? it : null;
                if (item == null)
                {
                    item = new Item();
                    item.SetDefaults(i);
                }
                if (item.createTile == lookupTile && item.placeStyle == style)
                {
                    lock (SyncLock) { TileItemCache[key] = item.type; }
                    return item.type;
                }
            }

            // 2. 降级宽容匹配：匹配任意 createTile == lookupTile 的物品
            if (ContentSamples.ItemsByType != null)
            {
                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it != null && it.createTile == lookupTile)
                    {
                        lock (SyncLock) { TileItemCache[key] = it.type; }
                        return it.type;
                    }
                }
            }

            for (int i = 1; i < ItemID.Count; i++)
            {
                Item item = new Item();
                item.SetDefaults(i);
                if (item.createTile == lookupTile)
                {
                    lock (SyncLock) { TileItemCache[key] = item.type; }
                    return item.type;
                }
            }

            lock (SyncLock) { TileItemCache[key] = 0; }
            return 0;
        }

        /// <summary>
        /// 根据背景墙类型解析对应的放置物品 ID
        /// </summary>
        public static int GetWallItemId(int wallType)
        {
            if (wallType <= 0) return 0;
            if (!isInitialized) Initialize();

            lock (SyncLock)
            {
                if (WallItemCache.TryGetValue(wallType, out int cached)) return cached;
            }

            if (ContentSamples.ItemsByType != null)
            {
                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it != null && it.createWall == wallType)
                    {
                        lock (SyncLock) { WallItemCache[wallType] = it.type; }
                        return it.type;
                    }
                }
            }

            for (int i = 1; i < ItemID.Count; i++)
            {
                Item item = new Item();
                item.SetDefaults(i);
                if (item.createWall == wallType)
                {
                    lock (SyncLock) { WallItemCache[wallType] = item.type; }
                    return item.type;
                }
            }

            lock (SyncLock) { WallItemCache[wallType] = 0; }
            return 0;
        }

        /// <summary>
        /// 根据图格信息与玩家状态解析对应的放置物品 ID（吸管与拾取通用入口）
        /// </summary>
        public static int ResolveTileOrWallToItemId(Tile tile, Player player, bool allowWall)
        {
            if (tile == null) return -1;
            if (!isInitialized) Initialize();

            // 1. 优先解析物块
            if (tile.active())
            {
                int specialItem = ResolveSpecialTile(tile, player);
                if (specialItem > 0)
                {
                    return specialItem;
                }

                int itemId = GetTileItemId(tile.type, tile.frameX, tile.frameY);
                if (itemId > 0)
                {
                    return itemId;
                }
            }

            // 2. 物块为空时，若允许吸取背景墙且存在背景墙
            if (allowWall && tile.wall > 0)
            {
                int wallItemId = GetWallItemId(tile.wall);
                if (wallItemId > 0)
                {
                    return wallItemId;
                }
            }

            return -1;
        }

        /// <summary>
        /// 特殊物块、草地种子、树木、宝石等的智能反查
        /// </summary>
        private static int ResolveSpecialTile(Tile tile, Player player)
        {
            switch (tile.type)
            {
                // 普通草：身上有草种子则拿出种子，否则拿出泥土块
                case TileID.Grass:
                    if (player != null && HasItemInInventory(player, ItemID.GrassSeeds)) return ItemID.GrassSeeds;
                    return ItemID.DirtBlock;

                // 腐化草
                case TileID.CorruptGrass:
                    if (player != null && HasItemInInventory(player, ItemID.CorruptSeeds)) return ItemID.CorruptSeeds;
                    return ItemID.DirtBlock;

                // 神圣草
                case TileID.HallowedGrass:
                    if (player != null && HasItemInInventory(player, ItemID.HallowedSeeds)) return ItemID.HallowedSeeds;
                    return ItemID.DirtBlock;

                // 猩红草
                case TileID.CrimsonGrass:
                    if (player != null && HasItemInInventory(player, ItemID.CrimsonSeeds)) return ItemID.CrimsonSeeds;
                    return ItemID.DirtBlock;

                // 丛林草：身上有丛林种子则拿出种子，否则拿出泥块
                case TileID.JungleGrass:
                    if (player != null && HasItemInInventory(player, ItemID.JungleGrassSeeds)) return ItemID.JungleGrassSeeds;
                    return ItemID.MudBlock;

                // 蘑菇草
                case TileID.MushroomGrass:
                    if (player != null && HasItemInInventory(player, ItemID.MushroomGrassSeeds)) return ItemID.MushroomGrassSeeds;
                    return ItemID.MudBlock;

                // 灰烬草
                case TileID.AshGrass:
                    if (player != null && HasItemInInventory(player, ItemID.AshGrassSeeds)) return ItemID.AshGrassSeeds;
                    return ItemID.AshBlock;

                // 树木类 -> 木材
                case TileID.Trees:
                    return ItemID.Wood;
                case TileID.PalmTree:
                    return ItemID.PalmWood;
                case TileID.Bamboo:
                    return ItemID.BambooBlock;
                case TileID.Cactus:
                    return ItemID.Cactus;
                case TileID.MushroomPlants:
                case TileID.MushroomTrees:
                    return ItemID.GlowingMushroom;

                // 地下裸露宝石 (Exposed Gems)
                case TileID.ExposedGems:
                    int gemIndex = tile.frameX / 18;
                    switch (gemIndex)
                    {
                        case 0: return ItemID.Amethyst;
                        case 1: return ItemID.Topaz;
                        case 2: return ItemID.Sapphire;
                        case 3: return ItemID.Emerald;
                        case 4: return ItemID.Ruby;
                        case 5: return ItemID.Diamond;
                        case 6: return ItemID.Amber;
                    }
                    break;
            }

            return -1;
        }

        /// <summary>
        /// 将打开的门（TileID 11）图格规范化转换为闭门（TileID 10）标准快照
        /// </summary>
        /// <param name="tile">世界图格</param>
        /// <param name="normalizedType">规范化后的物块类型（门轴列为 TileID.ClosedDoor，门扇列为 -1 空气）</param>
        /// <param name="normalizedFrameX">规范化闭门 frameX</param>
        /// <param name="normalizedFrameY">规范化闭门 frameY</param>
        /// <param name="isHingeColumn">是否为门轴所在的承重列</param>
        /// <returns>若为开门且成功识别返回 true，否则 false</returns>
        public static bool NormalizeOpenDoor(Tile tile, out short normalizedType, out short normalizedFrameX, out short normalizedFrameY, out bool isHingeColumn)
        {
            if (tile == null || !tile.active() || tile.type != TileID.OpenDoor)
            {
                normalizedType = (short)(tile != null && tile.active() ? tile.type : -1);
                normalizedFrameX = (short)(tile != null ? tile.frameX : 0);
                normalizedFrameY = (short)(tile != null ? tile.frameY : 0);
                isHingeColumn = false;
                return false;
            }

            int doorStyle = (tile.frameY / 54) + 36 * (tile.frameX / 72);
            int subX = tile.frameX % 72;

            // 原版开门朝向：
            // subX < 36: 向右开 (PlaceRight)，门轴在左列 (subX < 18 为轴，subX >= 18 为扇)
            // subX >= 36: 向左开 (PlaceLeft)，门轴在右列 (subX >= 54 为轴，subX < 54 为扇)
            if (subX < 36)
            {
                isHingeColumn = (subX < 18);
            }
            else
            {
                isHingeColumn = (subX >= 54);
            }

            if (isHingeColumn)
            {
                normalizedType = (short)TileID.ClosedDoor;
                normalizedFrameX = (short)((doorStyle / 36) * 54); // 闭门标准第 0 变种帧
                normalizedFrameY = (short)((doorStyle % 36) * 54 + (tile.frameY % 54));
            }
            else
            {
                // 门扇展开区域在关门后恢复为空气
                normalizedType = -1;
                normalizedFrameX = 0;
                normalizedFrameY = 0;
            }

            return true;
        }

        private static bool HasItemInInventory(Player player, int itemId)
        {
            if (player?.inventory == null) return false;
            for (int i = 0; i < 50; i++)
            {
                if (player.inventory[i] != null && player.inventory[i].type == itemId && player.inventory[i].stack > 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
