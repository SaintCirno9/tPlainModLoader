using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace OptimizeAndTool.Content.QoL.Pipette
{
    /// <summary>
    /// 图格与背景墙到物品的反向智能映射解析器
    /// </summary>
    public static class TileToItemResolver
    {
        private static readonly Dictionary<Tuple<int, int>, int> tileStyleToItem = new Dictionary<Tuple<int, int>, int>();
        private static readonly Dictionary<int, int> tileDefaultItem = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> wallToItem = new Dictionary<int, int>();
        private static bool isInitialized = false;

        /// <summary>
        /// 初始化反向映射字典
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;

            tileStyleToItem.Clear();
            tileDefaultItem.Clear();
            wallToItem.Clear();

            // 遍历原版所有物品类型
            for (int id = 1; id < ItemID.Count; id++)
            {
                Item item = new Item();
                item.netDefaults(id);

                // 物块放置物
                if (item.createTile >= 0)
                {
                    var key = Tuple.Create(item.createTile, item.placeStyle);
                    if (!tileStyleToItem.ContainsKey(key))
                    {
                        tileStyleToItem[key] = item.type;
                    }

                    if (!tileDefaultItem.ContainsKey(item.createTile))
                    {
                        tileDefaultItem[item.createTile] = item.type;
                    }
                }

                // 背景墙放置物
                if (item.createWall > 0)
                {
                    if (!wallToItem.ContainsKey(item.createWall))
                    {
                        wallToItem[item.createWall] = item.type;
                    }
                }
            }

            isInitialized = true;
        }

        /// <summary>
        /// 根据图格信息解析对应的放置物品 ID
        /// </summary>
        public static int ResolveTileOrWallToItemId(Tile tile, Player player, bool allowWall)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            if (tile == null) return -1;

            // 1. 优先解析物块
            if (tile.active())
            {
                int specialItem = ResolveSpecialTile(tile, player);
                if (specialItem > 0)
                {
                    return specialItem;
                }

                // 计算多格家具或变种 Style
                int style = CalculateTileStyle(tile);
                var key = Tuple.Create((int)tile.type, style);

                if (tileStyleToItem.TryGetValue(key, out int styleItemId))
                {
                    return styleItemId;
                }

                // 回退到默认物品
                if (tileDefaultItem.TryGetValue(tile.type, out int defaultItemId))
                {
                    return defaultItemId;
                }
            }

            // 2. 物块为空时，若允许吸取背景墙且存在背景墙
            if (allowWall && tile.wall > 0)
            {
                if (wallToItem.TryGetValue(tile.wall, out int wallItemId))
                {
                    return wallItemId;
                }
            }

            return -1;
        }

        /// <summary>
        /// 特殊物块、草地、树木、宝石等的智能处理
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

                // 火把：frameY / 22 是 Style
                case TileID.Torches:
                    int torchStyle = tile.frameY / 22;
                    if (tileStyleToItem.TryGetValue(Tuple.Create((int)TileID.Torches, torchStyle), out int torchId))
                    {
                        return torchId;
                    }
                    return ItemID.Torch;

                // 平台：frameY / 18 是 Style
                case TileID.Platforms:
                    int platStyle = tile.frameY / 18;
                    if (tileStyleToItem.TryGetValue(Tuple.Create((int)TileID.Platforms, platStyle), out int platId))
                    {
                        return platId;
                    }
                    return ItemID.WoodPlatform;

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
        /// 利用 TileObjectData 计算物块的样式 Style
        /// </summary>
        private static int CalculateTileStyle(Tile tile)
        {
            TileObjectData data = TileObjectData.GetTileData(tile);
            if (data == null) return 0;

            int fullW = data.CoordinateFullWidth;
            int fullH = data.CoordinateFullHeight;
            if (fullW <= 0 || fullH <= 0) return 0;

            int col = tile.frameX / fullW;
            int row = tile.frameY / fullH;
            int wrap = data.StyleWrapLimit > 0 ? data.StyleWrapLimit : 1;
            int rawStyle = data.StyleHorizontal ? (row * wrap + col) : (col * wrap + row);
            int mult = data.StyleMultiplier > 0 ? data.StyleMultiplier : 1;

            return rawStyle / mult;
        }

        /// <summary>
        /// 快速检测玩家身上是否有指定物品
        /// </summary>
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
