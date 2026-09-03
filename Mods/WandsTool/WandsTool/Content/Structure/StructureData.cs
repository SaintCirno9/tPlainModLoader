using Microsoft.Xna.Framework;
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
                    snap.FlipSlopeHorizontal(snap.TileType);

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
                        // 平台楼梯与变体垂直翻转时需同步执行切片镜像转换
                        bool isPlatform = snap.TileType == TileID.Platforms || (snap.TileType >= 0 && snap.TileType < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[snap.TileType]);
                        if (isPlatform)
                        {
                            snap.TileFrameX = FlipTileFrameX(snap.TileType, snap.TileFrameX);
                        }
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
        /// 判断图格是否为具有左右朝向（Left / Right）的方向性家具
        /// </summary>
        public static bool IsDirectionalTile(int tileType, TileObjectData data)
        {
            if (tileType == TileID.OpenDoor) return true;
            if (tileType == TileID.Chairs || 
                tileType == TileID.Beds || 
                tileType == TileID.Bathtubs || 
                tileType == TileID.Statues || 
                tileType == TileID.Mannequin || 
                tileType == TileID.Womannequin || 
                tileType == TileID.DisplayDoll || 
                tileType == TileID.HatRack || 
                tileType == TileID.WeaponsRack || 
                tileType == TileID.WeaponsRack2 || 
                tileType == TileID.Sinks || 
                tileType == TileID.Benches || 
                tileType == TileID.Thrones ||
                tileType == 489) // TargetDummy
            {
                return true;
            }

            if (data != null)
            {
                if (data.Direction != Terraria.Enums.TileObjectDirection.None) return true;
                if (data.StyleMultiplier == 2) return true;
                if (data.Alternates != null)
                {
                    for (int i = 0; i < data.Alternates.Count; i++)
                    {
                        if (data.Alternates[i] != null && data.Alternates[i].Direction != Terraria.Enums.TileObjectDirection.None)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 对物块 Framing 纹理切片执行水平镜像
        /// </summary>
        public static short FlipTileFrameX(int tileType, short frameX)
        {
            // 1. 多格家具与带 Style 的物件（如椅子、床、桌子、浴缸、雕像、开门等）
            if (Main.tileFrameImportant[tileType] && tileType != TileID.Platforms && !(tileType >= 0 && tileType < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[tileType]))
            {
                TileObjectData data = TileObjectData.GetTileData(tileType, 0);
                int objW = 1;
                int fullW = 18;
                bool isDirectional = false;

                if (tileType == TileID.OpenDoor)
                {
                    objW = 2;
                    fullW = 36;
                    isDirectional = true;
                }
                else if (data != null)
                {
                    objW = Math.Max(1, data.Width);
                    fullW = data.CoordinateFullWidth > 0 ? data.CoordinateFullWidth : (objW * 18);
                    isDirectional = IsDirectionalTile(tileType, data);
                }

                int subX = frameX % fullW;
                int localCol = subX / 18;
                int baseBlockX = frameX - subX;
                int newLocalCol = objW - 1 - localCol;

                int newBaseBlockX = baseBlockX;
                if (isDirectional)
                {
                    int doubleW = fullW * 2;
                    int dir = (baseBlockX % doubleW) / fullW;
                    newBaseBlockX = (dir == 0) ? (baseBlockX + fullW) : (baseBlockX - fullW);
                }

                return (short)(newBaseBlockX + newLocalCol * 18);
            }

            // 2. 平台楼梯与端头切片翻转（精确覆盖原版与Mod平台全部 Framing 变体）
            if (tileType == TileID.Platforms || (tileType >= 0 && tileType < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[tileType]))
            {
                switch (frameX)
                {
                    // 2.1 平放平台端头与贴墙变体
                    case 18: return 36;   // 右端头 [---  <->  左端头 ---]
                    case 36: return 18;   // 左端头 ---]  <->  右端头 [---
                    case 54: return 72;   // 左侧贴实心方块 <-> 右侧贴实心方块
                    case 72: return 54;   // 右侧贴实心方块 <-> 左侧贴实心方块
                    case 90: return 108;  // 右单格贴墙 <-> 左单格贴墙
                    case 108: return 90;  // 左单格贴墙 <-> 右单格贴墙
                    case 126: return 126; // 双侧贴墙（自身对称保持）

                    // 2.2 接楼梯的平放过渡段
                    case 216: return 234; // 左接上坡(slope=2) <-> 右接下坡(slope=1)
                    case 234: return 216; // 右接下坡(slope=1) <-> 左接上坡(slope=2)
                    case 252: return 252; // 双侧接楼梯（自身对称保持）
                    case 270: return 288; // 左接上坡右悬空 <-> 左悬空右接下坡
                    case 288: return 270; // 左悬空右接下坡 <-> 左接上坡右悬空

                    // 2.3 楼梯核心切片 (Slope 1 下行 <-> Slope 2 上行)
                    case 180: return 144; // 下行中段 <-> 上行中段
                    case 144: return 180; // 上行中段 <-> 下行中段
                    case 360: return 342; // 下行上端头 <-> 上行上端头
                    case 342: return 360; // 上行上端头 <-> 下行上端头
                    case 396: return 378; // 下行下端头 <-> 上行下端头
                    case 378: return 396; // 上行下端头 <-> 下行下端头
                    case 432: return 414; // 下行独立段 <-> 上行独立段
                    case 414: return 432; // 上行独立段 <-> 下行独立段
                    case 468: return 450; // 下行顶接平台 <-> 上行顶接平台
                    case 450: return 468; // 上行顶接平台 <-> 下行顶接平台

                    // 2.4 立柱与下挂支柱变体
                    case 162: return 198; // 左下木桩 <-> 右下木桩
                    case 198: return 162; // 右下木桩 <-> 左下木桩
                    case 306: return 324; // 左立柱端头 <-> 右立柱端头
                    case 324: return 306; // 右立柱端头 <-> 左立柱端头

                    default: return frameX; // 0 (连通中间) 等对称切片保持不变
                }
            }

            // 3. 标准 3x3 实体方块 18px 贴图切片左右镜像 (col 0 <-> col 2)
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
            // 1. 多格家具在垂直翻转时需保持直立（Right-Side-Up）
            if (Main.tileFrameImportant[tileType] && tileType != TileID.Platforms && !(tileType >= 0 && tileType < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[tileType]))
            {
                TileObjectData data = TileObjectData.GetTileData(tileType, 0);
                int objH = 1;
                int fullH = 18;

                if (tileType == TileID.OpenDoor)
                {
                    objH = 3;
                    fullH = 54;
                }
                else if (data != null)
                {
                    objH = Math.Max(1, data.Height);
                    fullH = data.CoordinateFullHeight > 0 ? data.CoordinateFullHeight : (objH * 18);
                }

                int subY = frameY % fullH;
                int localRow = subY / 18;
                int baseBlockY = frameY - subY;
                int newLocalRow = objH - 1 - localRow;

                return (short)(baseBlockY + newLocalRow * 18);
            }

            // 2. 平台不翻转 Y
            if (tileType == TileID.Platforms || (tileType >= 0 && tileType < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[tileType])) return frameY;

            // 3. 标准 3x3 实体方块 18px 贴图切片上下镜像 (row 0 <-> row 2)
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

        /// <summary>
        /// 检查当前世界图格与蓝图切片是否已具有相同的内容（相同物块类型或相同家具样式）
        /// </summary>
        public static bool IsTileIdentical(Tile worldTile, TileSnapshot snap)
        {
            if (worldTile == null) return false;
            if (!snap.HasTile)
            {
                return !worldTile.active();
            }

            if (!worldTile.active()) return false;

            // 门类特殊比对：开门与关门之间若为同种木质/金属风格的门，视为相同门类
            bool isSnapDoor = snap.TileType == TileID.ClosedDoor || snap.TileType == TileID.OpenDoor;
            bool isWorldDoor = worldTile.type == TileID.ClosedDoor || worldTile.type == TileID.OpenDoor;
            if (isSnapDoor && isWorldDoor)
            {
                int worldStyle = GetTileStyle(worldTile.type, worldTile.frameX, worldTile.frameY);
                int snapStyle = GetTileStyle(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                return worldStyle == snapStyle;
            }

            if (worldTile.type != snap.TileType) return false;

            // 如果是家具或带 Style 的物块（如椅子、床、桌子、箱子、火把等）
            if (Main.tileFrameImportant[snap.TileType])
            {
                // 平台
                bool isPlatform = snap.TileType == TileID.Platforms || (snap.TileType >= 0 && snap.TileType < TileID.Sets.Platforms.Length && TileID.Sets.Platforms[snap.TileType]);
                if (isPlatform)
                {
                    return (worldTile.frameY / 18) == (snap.TileFrameY / 18);
                }

                // 家具：比对 Style
                int worldStyle = GetTileStyle(worldTile.type, worldTile.frameX, worldTile.frameY);
                int snapStyle = GetTileStyle(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                return worldStyle == snapStyle;
            }

            // 普通实体方块（泥土、石头、木方块、砖块等）：只要 type 一致即可
            return true;
        }

        /// <summary>
        /// 统计当前蓝图所需的所有物品清单及总数需求（支持差量智能比对：世界已有相同物块/墙壁/家具/电线直接免除，零重复扣料！）
        /// </summary>
        public Dictionary<int, int> GetRequiredItems(Point? originWorldTile = null, bool overwrite = true)
        {
            Dictionary<int, int> requirements = new Dictionary<int, int>();

            Action<int, int> addItem = (itemId, count) =>
            {
                if (itemId <= 0 || count <= 0) return;
                if (requirements.ContainsKey(itemId)) requirements[itemId] += count;
                else requirements[itemId] = count;
            };

            bool[,] tileCounted = new bool[Width, Height];
            int startX = originWorldTile.HasValue ? (originWorldTile.Value.X - OriginX) : 0;
            int startY = originWorldTile.HasValue ? (originWorldTile.Value.Y - OriginY) : 0;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    TileSnapshot snap = Tiles[x, y];
                    int wx = startX + x;
                    int wy = startY + y;
                    bool inWorldBounds = originWorldTile.HasValue && wx >= 0 && wx < Main.maxTilesX && wy >= 0 && wy < Main.maxTilesY;
                    Tile worldTile = inWorldBounds ? Main.tile[wx, wy] : null;

                    // 1. 统计背景墙（若世界该处已有完全相同的墙壁，零消耗免除；若关闭蓝图墙壁开关则不统计）
                    if (snap.HasWall && GameMain.Wand_StructureIncludeWall)
                    {
                        bool wallAlreadySame = worldTile != null && worldTile.wall == snap.WallType;
                        if (!wallAlreadySame)
                        {
                            if (overwrite || worldTile == null || worldTile.wall == 0)
                            {
                                int wallItem = GetWallItemId(snap.WallType);
                                if (wallItem > 0) addItem(wallItem, 1);
                            }
                        }
                    }

                    // 2. 统计方块与家具（差量比对：若世界已有完全相同的物块/家具，直接免除材料消耗！）
                    if (snap.HasTile && !tileCounted[x, y])
                    {
                        bool isSame = inWorldBounds && IsTileIdentical(worldTile, snap);
                        if (isSame)
                        {
                            // 世界上已有相同物块/家具，整块标记为已处理，无需任何材料！
                            TileObjectData data = TileObjectData.GetTileData(snap.TileType, 0);
                            if (data != null && (data.Width > 1 || data.Height > 1))
                            {
                                int objW = Math.Max(1, data.Width);
                                int objH = Math.Max(1, data.Height);
                                for (int dx = 0; dx < objW && x + dx < Width; dx++)
                                {
                                    for (int dy = 0; dy < objH && y + dy < Height; dy++)
                                    {
                                        tileCounted[x + dx, y + dy] = true;
                                    }
                                }
                            }
                            else
                            {
                                tileCounted[x, y] = true;
                            }
                            continue;
                        }

                        // 非覆盖模式下若世界该处已被其他物块占据，跳过
                        if (!overwrite && worldTile != null && worldTile.active())
                        {
                            tileCounted[x, y] = true;
                            continue;
                        }

                        TileObjectData data2 = TileObjectData.GetTileData(snap.TileType, 0);
                        if (data2 != null && (data2.Width > 1 || data2.Height > 1))
                        {
                            int objW = Math.Max(1, data2.Width);
                            int objH = Math.Max(1, data2.Height);
                            for (int dx = 0; dx < objW && x + dx < Width; dx++)
                            {
                                for (int dy = 0; dy < objH && y + dy < Height; dy++)
                                {
                                    tileCounted[x + dx, y + dy] = true;
                                }
                            }

                            int tileItem = GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                            if (tileItem > 0) addItem(tileItem, 1);
                        }
                        else
                        {
                            tileCounted[x, y] = true;
                            int tileItem = GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                            if (tileItem > 0) addItem(tileItem, 1);
                        }
                    }

                    // 3. 统计电线（已有则免除）
                    if (snap.RedWire && (worldTile == null || !worldTile.wire())) addItem(ItemID.Wire, 1);
                    if (snap.GreenWire && (worldTile == null || !worldTile.wire3())) addItem(ItemID.Wire, 1);
                    if (snap.BlueWire && (worldTile == null || !worldTile.wire2())) addItem(ItemID.Wire, 1);
                    if (snap.YellowWire && (worldTile == null || !worldTile.wire4())) addItem(ItemID.Wire, 1);

                    // 4. 统计促动器（已有则免除）
                    if (snap.Actuator && (worldTile == null || !worldTile.actuator())) addItem(ItemID.Actuator, 1);
                }
            }

            return requirements;
        }

        /// <summary>
        /// 根据图格类型与切片帧坐标，精确解析家具/物块的样式（Style）
        /// </summary>
        public static int GetTileStyle(int tileType, int frameX, int frameY)
        {
            return TPML.Content.Core.TileItemResolver.CalculateTileStyle(tileType, frameX, frameY);
        }

        /// <summary>
        /// 根据图格类型与切片帧坐标，精确解析对应的放置物品 ID（支持不同木质/金属风格的家具精准匹配与开门反查）
        /// </summary>
        public static int GetTileItemId(int tileType, int frameX = 0, int frameY = 0)
        {
            return TPML.Content.Core.TileItemResolver.GetTileItemId(tileType, frameX, frameY);
        }

        /// <summary>
        /// 根据背景墙类型解析对应的放置物品 ID
        /// </summary>
        public static int GetWallItemId(int wallType)
        {
            return TPML.Content.Core.TileItemResolver.GetWallItemId(wallType);
        }
    }
}
