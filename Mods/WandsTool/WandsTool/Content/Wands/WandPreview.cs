using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace WandsTool.Content
{
    /// <summary>
    /// 高性能半透明材质施工虚影渲染器。
    /// 拖拽框选阶段基于当前选区点集实时渲染真实物块/背景墙半透明虚影（放置）、红色破坏遮罩（破坏）
    /// 或对应液体色彩（液体），并严格视口裁剪保证超大框选依然 60 FPS 满帧。
    /// 作者: SaintCirno9
    /// </summary>
    public static class WandPreview
    {
        private static HashSet<Point> _cachedSet = null;
        private static List<Point> _cachedSource = null;

        public static void Draw(SpriteBatch sb, List<Point> shapes)
        {
            if (shapes == null || shapes.Count == 0) return;
            if (sb == null) return;

            try
            {
                Player player = Main.LocalPlayer;
                if (player == null) return;

                // 1. 液体模式：按液体种类渲染半透明水体色块
                if (GameMain.Wand_LiquidMode != GameMain.LiquidMode.None)
                {
                    DrawLiquid(sb, shapes);
                    return;
                }

                // 2. 蓝图/结构模式（复制/剪切/删除）保持原有边框与方块提示，不渲染材质虚影
                if (GameMain.Wand_StructureMode != GameMain.StructureMode.None) return;

                // 3. 放置模式：真实物块/背景墙半透明材质虚影 + 淡绿微光
                if (GameMain.Wand_isPlace)
                {
                    if (!GameMain.Wand_Tile && !GameMain.Wand_Wall) return;

                    Item tileItem = GameMain.Wand_Tile ? WandAction.FirstItem_Tile(player) : null;
                    Item wallItem = GameMain.Wand_Wall ? WandAction.FirstItem_Wall(player) : null;
                    if (tileItem == null && wallItem == null) return; // 缺材料时不渲染材质虚影

                    DrawPlacement(sb, shapes, tileItem, wallItem);
                    return;
                }

                // 4. 破坏模式：对选区既有物块/背景墙覆盖红色高亮遮罩与裂纹虚影
                DrawBreak(sb, shapes);
            }
            catch
            {
                // 静默容错：任何绘制异常都不允许冒泡到 XNA Draw 线程
            }
        }

        /// <summary>
        /// 获取选区点集的快速哈希集合（shapes 引用未变时复用缓存）
        /// </summary>
        private static HashSet<Point> GetShapeSet(List<Point> shapes)
        {
            if (_cachedSource == shapes && _cachedSet != null) return _cachedSet;

            HashSet<Point> set = new HashSet<Point>(shapes);
            _cachedSet = set;
            _cachedSource = shapes;
            return set;
        }

        /// <summary>
        /// 当前屏幕可视图格范围（含 1 格外扩，避免边缘闪烁）
        /// </summary>
        private static void GetViewportTileRange(out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = (int)Math.Floor(Main.screenPosition.X / 16f) - 1;
            minY = (int)Math.Floor(Main.screenPosition.Y / 16f) - 1;
            maxX = (int)Math.Ceiling((Main.screenPosition.X + Main.screenWidth) / 16f) + 1;
            maxY = (int)Math.Ceiling((Main.screenPosition.Y + Main.screenHeight) / 16f) + 1;
        }

        /// <summary>
        /// 放置模式：半透明真实物块/背景墙纹理虚影（50% 透明度 + 淡绿微光）
        /// </summary>
        private static void DrawPlacement(SpriteBatch sb, List<Point> shapes, Item tileItem, Item wallItem)
        {
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            if (magicPixel == null) return;

            HashSet<Point> shapeSet = GetShapeSet(shapes);
            GetViewportTileRange(out int minX, out int minY, out int maxX, out int maxY);

            // 物块对象图与物品类型一致性
            Texture2D tileTex = null;
            int tileCreateType = 0;
            int tilePlaceStyle = 0;
            if (tileItem != null && tileItem.createTile >= 0 && tileItem.createTile < TextureAssets.Tile.Length)
            {
                try
                {
                    Main.instance.LoadTiles(tileItem.createTile);
                    tileTex = TextureAssets.Tile[tileItem.createTile]?.Value;
                    tileCreateType = tileItem.createTile;
                    tilePlaceStyle = tileItem.placeStyle;
                }
                catch { tileTex = null; }
            }

            Texture2D wallTex = null;
            int wallCreateType = 0;
            if (wallItem != null && wallItem.createWall > 0 && wallItem.createWall < TextureAssets.Wall.Length)
            {
                try
                {
                    Main.instance.LoadWall(wallItem.createWall);
                    wallTex = TextureAssets.Wall[wallItem.createWall]?.Value;
                    wallCreateType = wallItem.createWall;
                }
                catch { wallTex = null; }
            }

            if (tileTex == null && wallTex == null) return;

            foreach (Point p in shapes)
            {
                if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY) continue; // 视口裁剪

                Vector2 drawPos = new Vector2(p.X * 16, p.Y * 16) - Main.screenPosition;
                Tile dst = Main.tile[p.X, p.Y];

                // 淡绿微光底色（材料准备就绪的视觉暗示）
                sb.Draw(magicPixel, new Rectangle((int)drawPos.X, (int)drawPos.Y, 16, 16), Color.LimeGreen * 0.08f);

                // 1. 物块虚影
                bool canDrawTile = false;
                if (tileTex != null)
                {
                    if (dst != null && dst.active())
                    {
                        // 已有方块：需开启替换，且在过滤模式下必须与起点方块同材质
                        if (GameMain.Wand_ReplaceExisting && dst.type != tileCreateType)
                        {
                            if (!GameMain.Wand_MatchFilter || Wands.StartTileType < 0 || dst.type == Wands.StartTileType)
                            {
                                canDrawTile = true;
                            }
                        }
                    }
                    else
                    {
                        // 空白格：需开启填充空处
                        if (GameMain.Wand_FillEmpty)
                        {
                            canDrawTile = true;
                        }
                    }
                }

                if (canDrawTile)
                {
                    int frameX = 0;
                    int frameY = 0;
                    if (dst != null && dst.active() && dst.type == tileCreateType)
                    {
                        frameX = dst.frameX;
                        frameY = dst.frameY;
                    }
                    else
                    {
                        // 空白格：按放置样式估算标准物块帧
                        frameX = (tilePlaceStyle % 100) * 18;
                        frameY = (tilePlaceStyle / 100) * 36;
                    }

                    Rectangle src = new Rectangle(
                        Math.Max(0, Math.Min(frameX, tileTex.Width - 16)),
                        Math.Max(0, Math.Min(frameY, tileTex.Height - 16)),
                        16, 16);

                    sb.Draw(tileTex, drawPos, src, Color.White * 0.50f);
                }

                // 2. 背景墙虚影
                bool canDrawWall = false;
                if (wallTex != null)
                {
                    if (dst != null && dst.wall > 0)
                    {
                        if (GameMain.Wand_ReplaceExisting && dst.wall != wallCreateType)
                        {
                            if (!GameMain.Wand_MatchFilter || Wands.StartWallType <= 0 || dst.wall == Wands.StartWallType)
                            {
                                canDrawWall = true;
                            }
                        }
                    }
                    else
                    {
                        if (GameMain.Wand_FillEmpty)
                        {
                            canDrawWall = true;
                        }
                    }
                }

                if (canDrawWall)
                {
                    int wfX = 0;
                    int wfY = 0;
                    if (dst != null && dst.wall == wallCreateType)
                    {
                        wfX = dst.wallFrameX();
                        wfY = dst.wallFrameY();
                    }

                    Rectangle wallSrc = new Rectangle(
                        Math.Max(0, Math.Min(wfX, wallTex.Width - 32)),
                        Math.Max(0, Math.Min(wfY, wallTex.Height - 32)),
                        32, 32);

                    sb.Draw(wallTex, drawPos - new Vector2(8, 8), wallSrc, Color.White * 0.45f);
                }
            }
        }

        /// <summary>
        /// 破坏模式：对选区既有物块与背景墙覆盖 40% 红色高亮遮罩与裂纹虚影（支持同材质过滤精准遮罩与安全保护）
        /// </summary>
        private static void DrawBreak(SpriteBatch sb, List<Point> shapes)
        {
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            if (magicPixel == null) return;

            GetViewportTileRange(out int minX, out int minY, out int maxX, out int maxY);

            int filterTile = -1;
            int filterWall = -1;
            if (GameMain.Wand_MatchFilter)
            {
                // 层级优先：起点有物块则仅破坏同类物块（保护背景墙）；
                // 仅当起点无物块（纯背景墙）时才破坏同类背景墙；
                // 起点为空白空气时退化为全量破坏
                if (Wands.StartTileType >= 0)
                {
                    filterTile = Wands.StartTileType;
                    filterWall = -1;
                }
                else if (Wands.StartWallType > 0)
                {
                    filterTile = -1;
                    filterWall = Wands.StartWallType;
                }
            }

            foreach (Point p in shapes)
            {
                if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY) continue; // 视口裁剪

                Tile tile = Main.tile[p.X, p.Y];
                if (tile == null) continue;
                if (!tile.active() && tile.wall <= 0) continue; // 无可破坏内容

                // 过滤匹配判断
                bool tileMatch = GameMain.Wand_Tile && tile.active() && (filterTile < 0 || tile.type == filterTile);
                bool wallMatch = GameMain.Wand_Wall && (tile.wall > 0) && (filterWall < 0 || tile.wall == filterWall);

                if (GameMain.Wand_MatchFilter)
                {
                    if (filterTile >= 0 && !tileMatch) continue;
                    if (filterWall > 0 && !wallMatch) continue;
                    if (filterTile < 0 && filterWall < 0 && !tileMatch && !wallMatch) continue;
                }
                else
                {
                    if (!tileMatch && !wallMatch) continue;
                }

                Vector2 pos = new Vector2(p.X * 16, p.Y * 16) - Main.screenPosition;
                Rectangle rect = new Rectangle((int)pos.X, (int)pos.Y, 16, 16);

                // 40% 红色高亮遮罩
                sb.Draw(magicPixel, rect, Color.Red * 0.40f);

                // 裂纹虚影：两条对角细线
                Terraria.Utils.DrawLine(sb, pos, pos + new Vector2(16, 16), Color.Red * 0.65f, Color.Red * 0.65f, 1.5f);
                Terraria.Utils.DrawLine(sb, pos + new Vector2(16, 0), pos + new Vector2(0, 16), Color.Red * 0.65f, Color.Red * 0.65f, 1.5f);
            }
        }

        /// <summary>
        /// 液体模式：按液体种类渲染半透明水体色块
        /// </summary>
        private static void DrawLiquid(SpriteBatch sb, List<Point> shapes)
        {
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            if (magicPixel == null) return;

            Color liquidColor;
            switch (GameMain.Wand_LiquidMode)
            {
                case GameMain.LiquidMode.Water: liquidColor = new Color(30, 160, 255); break;
                case GameMain.LiquidMode.Lava: liquidColor = new Color(255, 90, 20); break;
                case GameMain.LiquidMode.Honey: liquidColor = new Color(255, 190, 0); break;
                case GameMain.LiquidMode.Shimmer: liquidColor = new Color(225, 120, 255); break;
                case GameMain.LiquidMode.Absorb: liquidColor = new Color(0, 230, 200); break;
                case GameMain.LiquidMode.Clear: liquidColor = new Color(220, 220, 220); break;
                default: liquidColor = Color.White; break;
            }

            GetViewportTileRange(out int minX, out int minY, out int maxX, out int maxY);

            foreach (Point p in shapes)
            {
                if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY) continue; // 视口裁剪

                Vector2 pos = new Vector2(p.X * 16, p.Y * 16) - Main.screenPosition;
                // 水体半透明色块 + 上方高光细边模拟液面
                sb.Draw(magicPixel, new Rectangle((int)pos.X, (int)pos.Y, 16, 16), liquidColor * 0.45f);
                sb.Draw(magicPixel, new Rectangle((int)pos.X, (int)pos.Y, 16, 2), liquidColor * 0.85f);
            }
        }
    }
}