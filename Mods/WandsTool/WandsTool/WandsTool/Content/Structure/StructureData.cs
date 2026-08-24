using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 建筑蓝图结构主数据对象
    /// </summary>
    [Serializable]
    public class StructureData
    {
        public string Name = "未命名结构";
        public string BuildTime = "";
        public int Width = 0;
        public int Height = 0;
        public int OriginX = 0;
        public int OriginY = 0;

        public TileSnapshot[,] Tiles = null;
        public List<string> SignTexts = new List<string>();

        public StructureData() { }

        public StructureData(int width, int height, string name = "未命名结构")
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Name = name;
            BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Tiles = new TileSnapshot[Width, Height];
        }

        /// <summary>
        /// 水平镜像翻转整套结构（包含相对坐标、斜坡角度、半砖与 Framing 纹理切片）
        /// </summary>
        public StructureData FlipHorizontal()
        {
            StructureData flipped = new StructureData(Width, Height, Name)
            {
                BuildTime = BuildTime,
                OriginX = Width - 1 - OriginX,
                OriginY = OriginY,
                SignTexts = new List<string>(SignTexts)
            };

            for (int x = 0; x < Width; x++)
            {
                int newX = Width - 1 - x;
                for (int y = 0; y < Height; y++)
                {
                    TileSnapshot snap = Tiles[x, y];
                    snap.FlipSlopeHorizontal();

                    if (snap.HasTile)
                    {
                        snap.TileFrameX = FlipTileFrameX(snap.TileType, snap.TileFrameX);
                    }
                    if (snap.HasWall)
                    {
                        snap.WallFrameX = FlipWallFrameX(snap.WallFrameX);
                    }

                    flipped.Tiles[newX, y] = snap;
                }
            }

            return flipped;
        }

        /// <summary>
        /// 垂直翻转整套结构（包含相对坐标、斜坡角度、半砖与 Framing 纹理切片）
        /// </summary>
        public StructureData FlipVertical()
        {
            StructureData flipped = new StructureData(Width, Height, Name)
            {
                BuildTime = BuildTime,
                OriginX = OriginX,
                OriginY = Height - 1 - OriginY,
                SignTexts = new List<string>(SignTexts)
            };

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int newY = Height - 1 - y;
                    TileSnapshot snap = Tiles[x, y];
                    snap.FlipSlopeVertical(snap.TileType);

                    if (snap.HasTile)
                    {
                        snap.TileFrameY = FlipTileFrameY(snap.TileType, snap.TileFrameY);
                    }
                    if (snap.HasWall)
                    {
                        snap.WallFrameY = FlipWallFrameY(snap.WallFrameY);
                    }

                    flipped.Tiles[x, newY] = snap;
                }
            }

            return flipped;
        }

        /// <summary>
        /// 对物块 Framing 纹理切片执行水平镜像
        /// </summary>
        public static short FlipTileFrameX(int tileType, short frameX)
        {
            TileObjectData data = TileObjectData.GetTileData(tileType, 0);
            if (data != null && (data.Width > 1 || data.Height > 1))
            {
                if (data.Direction != Terraria.Enums.TileObjectDirection.None && data.CoordinateFullWidth > 0)
                {
                    int partWidth = data.CoordinateFullWidth;
                    int subX = frameX % partWidth;
                    int baseBlockX = frameX - subX;
                    int invertedSubX = partWidth - 18 - subX;
                    if (invertedSubX >= 0)
                    {
                        return (short)(baseBlockX + invertedSubX);
                    }
                }
                return frameX;
            }

            // 平台楼梯与端头切片翻转
            if (tileType == Terraria.ID.TileID.Platforms)
            {
                if (frameX == 144) return 126;
                if (frameX == 126) return 144;
                if (frameX == 198) return 162;
                if (frameX == 162) return 198;
                if (frameX == 324) return 306;
                if (frameX == 306) return 324;
                if (frameX == 0) return 36;
                if (frameX == 36) return 0;
                return frameX;
            }

            // 标准 3x3 实体方块 18px 贴图切片左右镜像 (col 0 <-> col 2)
            int col = (frameX / 18) % 3;
            int baseCol = (frameX / 54) * 54;
            int offset = frameX % 18;

            if (col == 0) return (short)(baseCol + 36 + offset);
            if (col == 2) return (short)(baseCol + 0 + offset);
            return frameX;
        }

        /// <summary>
        /// 对物块 Framing 纹理切片执行垂直镜像
        /// </summary>
        public static short FlipTileFrameY(int tileType, short frameY)
        {
            TileObjectData data = TileObjectData.GetTileData(tileType, 0);
            if (data != null && (data.Width > 1 || data.Height > 1))
            {
                return frameY;
            }

            // 平台不翻转 Y
            if (tileType == Terraria.ID.TileID.Platforms) return frameY;

            // 标准 3x3 实体方块 18px 贴图切片上下镜像 (row 0 <-> row 2)
            int row = (frameY / 18) % 3;
            int baseRow = (frameY / 54) * 54;
            int offset = frameY % 18;

            if (row == 0) return (short)(baseRow + 36 + offset);
            if (row == 2) return (short)(baseRow + 0 + offset);
            return frameY;
        }

        /// <summary>
        /// 对背景墙切片执行水平镜像
        /// </summary>
        public static short FlipWallFrameX(short wallFrameX)
        {
            int col = (wallFrameX / 36) % 3;
            int baseCol = (wallFrameX / 108) * 108;
            int offset = wallFrameX % 36;

            if (col == 0) return (short)(baseCol + 72 + offset);
            if (col == 2) return (short)(baseCol + 0 + offset);
            return wallFrameX;
        }

        /// <summary>
        /// 对背景墙切片执行垂直镜像
        /// </summary>
        public static short FlipWallFrameY(short wallFrameY)
        {
            int row = (wallFrameY / 36) % 3;
            int baseRow = (wallFrameY / 108) * 108;
            int offset = wallFrameY % 36;

            if (row == 0) return (short)(baseRow + 72 + offset);
            if (row == 2) return (short)(baseRow + 0 + offset);
            return wallFrameY;
        }

        // 缓存图格/墙壁到物品ID的映射，极大提高统计与放置性能
        private static readonly Dictionary<int, int> TileItemCache = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> WallItemCache = new Dictionary<int, int>();

        /// <summary>
        /// 统计当前蓝图所需的所有物品清单及总数需求
        /// </summary>
        public Dictionary<int, int> GetRequiredItems()
        {
            Dictionary<int, int> requirements = new Dictionary<int, int>();

            Action<int, int> addItem = (itemId, count) =>
            {
                if (itemId <= 0 || count <= 0) return;
                if (requirements.ContainsKey(itemId)) requirements[itemId] += count;
                else requirements[itemId] = count;
            };

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    TileSnapshot snap = Tiles[x, y];

                    // 1. 统计背景墙
                    if (snap.HasWall)
                    {
                        int wallItem = GetWallItemId(snap.WallType);
                        if (wallItem > 0) addItem(wallItem, 1);
                    }

                    // 2. 统计方块与家具（多格物块仅在主原点处统计一次）
                    if (snap.HasTile)
                    {
                        TileObjectData data = TileObjectData.GetTileData(snap.TileType, 0);
                        if (data != null && (data.Width > 1 || data.Height > 1))
                        {
                            // 判断是否为多格物块的锚点/左上角
                            int subX = snap.TileFrameX % data.CoordinateFullWidth;
                            int subY = snap.TileFrameY % data.CoordinateFullHeight;
                            if (subX == 0 && subY == 0)
                            {
                                int tileItem = GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                                if (tileItem > 0) addItem(tileItem, 1);
                            }
                        }
                        else
                        {
                            int tileItem = GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                            if (tileItem > 0) addItem(tileItem, 1);
                        }
                    }

                    // 3. 统计电线
                    int wireCount = (snap.RedWire ? 1 : 0) + (snap.GreenWire ? 1 : 0) + (snap.BlueWire ? 1 : 0) + (snap.YellowWire ? 1 : 0);
                    if (wireCount > 0) addItem(ItemID.Wire, wireCount);

                    // 4. 统计促动器
                    if (snap.Actuator) addItem(ItemID.Actuator, 1);
                }
            }

            return requirements;
        }

        public static int GetTileItemId(int tileType, int frameX = 0, int frameY = 0)
        {
            int key = (tileType << 16) | ((frameX / 18) & 0xFF);
            if (TileItemCache.TryGetValue(key, out int cached)) return cached;

            if (ContentSamples.ItemsByType != null)
            {
                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it != null && it.createTile == tileType)
                    {
                        TileItemCache[key] = it.type;
                        return it.type;
                    }
                }
            }

            for (int i = 1; i < ItemID.Count; i++)
            {
                Item item = new Item();
                item.SetDefaults(i);
                if (item.createTile == tileType)
                {
                    TileItemCache[key] = item.type;
                    return item.type;
                }
            }

            TileItemCache[key] = 0;
            return 0;
        }

        public static int GetWallItemId(int wallType)
        {
            if (WallItemCache.TryGetValue(wallType, out int cached)) return cached;

            if (ContentSamples.ItemsByType != null)
            {
                foreach (var kvp in ContentSamples.ItemsByType)
                {
                    Item it = kvp.Value;
                    if (it != null && it.createWall == wallType)
                    {
                        WallItemCache[wallType] = it.type;
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
                    WallItemCache[wallType] = item.type;
                    return item.type;
                }
            }

            WallItemCache[wallType] = 0;
            return 0;
        }
    }
}
