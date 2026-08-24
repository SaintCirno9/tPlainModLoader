using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Content.Cheat.Function2;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tContentPatch;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    public class StructurePin
    {
        public Vector2 PositionInTiles;
        public string Name;
        public Color Color;
        public string Category; // "Plantera", "SwordShrine", "Larva", "Shimmer", "Pyramid", "Temple"
        public string ItemTexturePath;
    }

    /// <summary>
    /// 关键结构小地图标记（金字塔、世纪之花花苞、附魔剑冢、蜂巢幼虫、微光湖、神庙祭坛）
    /// 作者: SaintCirno9
    /// </summary>
    public class StructureMarker : PatchMain
    {
        private static List<StructurePin> _pins = new List<StructurePin>();
        private static bool _isScanning = false;

        public override void OnEnterWorld()
        {
            TriggerRescan();
        }

        public static void TriggerRescan()
        {
            if (_isScanning) return;

            _ = Task.Run(() =>
            {
                try
                {
                    _isScanning = true;
                    ScanWorldStructures();
                }
                catch (Exception ex)
                {
                    Main.NewText($"[结构标记] 扫描失败: {ex.Message}");
                }
                finally
                {
                    _isScanning = false;
                }
            });
        }

        private static void ScanWorldStructures()
        {
            List<StructurePin> list = new List<StructurePin>();

            int maxX = Main.maxTilesX;
            int maxY = Main.maxTilesY;

            // 微光湖聚类
            List<Vector2> shimmerTiles = new List<Vector2>();

            // 金字塔聚类
            List<Vector2> pyramidTiles = new List<Vector2>();

            for (int x = 10; x < maxX - 10; x++)
            {
                for (int y = 10; y < maxY - 10; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile == null) continue;

                    // 1. 世纪之花花苞
                    if (tile.active() && tile.type == TileID.PlanteraBulb)
                    {
                        // 取花苞左上角
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            list.Add(new StructurePin
                            {
                                PositionInTiles = new Vector2(x, y),
                                Name = "世纪之花花苞",
                                Color = Color.Magenta,
                                Category = "Plantera",
                                ItemTexturePath = "Images/Item_1157"
                            });
                        }
                    }

                    // 2. 附魔剑冢
                    if (tile.active() && tile.type == TileID.LargePiles2)
                    {
                        if (tile.frameX >= 17 * 18 && tile.frameX <= 19 * 18 && tile.frameY == 0)
                        {
                            list.Add(new StructurePin
                            {
                                PositionInTiles = new Vector2(x, y),
                                Name = "附魔剑冢",
                                Color = Color.Cyan,
                                Category = "SwordShrine",
                                ItemTexturePath = "Images/Item_72"
                            });
                        }
                    }

                    // 3. 蜂巢幼虫
                    if (tile.active() && tile.type == TileID.Larva)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            list.Add(new StructurePin
                            {
                                PositionInTiles = new Vector2(x, y),
                                Name = "蜂巢幼虫",
                                Color = Color.Gold,
                                Category = "Larva",
                                ItemTexturePath = "Images/Item_1133"
                            });
                        }
                    }

                    // 4. 丛林神庙祭坛
                    if (tile.active() && tile.type == TileID.LihzahrdAltar)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            list.Add(new StructurePin
                            {
                                PositionInTiles = new Vector2(x, y),
                                Name = "丛林神庙石巨人祭坛",
                                Color = Color.OrangeRed,
                                Category = "Temple",
                                ItemTexturePath = "Images/Item_1293"
                            });
                        }
                    }

                    // 5. 微光湖探测
                    if (tile.liquidType() == LiquidID.Shimmer && tile.liquid > 150)
                    {
                        shimmerTiles.Add(new Vector2(x, y));
                    }

                    // 6. 金字塔砖块探测
                    if (tile.active() && tile.type == TileID.SandstoneBrick && y < Main.worldSurface)
                    {
                        pyramidTiles.Add(new Vector2(x, y));
                    }
                }
            }

            // 计算微光湖中心
            if (shimmerTiles.Count > 20)
            {
                float sumX = 0f, sumY = 0f;
                foreach (Vector2 p in shimmerTiles)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                Vector2 centroid = new Vector2(sumX / shimmerTiles.Count, sumY / shimmerTiles.Count);
                list.Add(new StructurePin
                {
                    PositionInTiles = centroid,
                    Name = "以太微光湖",
                    Color = Color.MediumPurple,
                    Category = "Shimmer",
                    ItemTexturePath = "Images/Item_5334"
                });
            }

            // 计算金字塔中心
            if (pyramidTiles.Count > 100)
            {
                float sumX = 0f, sumY = 0f;
                foreach (Vector2 p in pyramidTiles)
                {
                    sumX += p.X;
                    sumY += p.Y;
                }
                Vector2 centroid = new Vector2(sumX / pyramidTiles.Count, sumY / pyramidTiles.Count);
                list.Add(new StructurePin
                {
                    PositionInTiles = centroid,
                    Name = "沙漠金字塔",
                    Color = Color.SandyBrown,
                    Category = "Pyramid",
                    ItemTexturePath = "Images/Item_857"
                });
            }

            lock (_pins)
            {
                _pins = list;
            }

            Main.NewText($"[结构标记] 扫描完成，共找到 {_pins.Count} 处关键结构！");
        }

        public override void DrawMapPostfix(GameTime gameTime)
        {
            if (!QoLValSet.markStructuresOnMap.val) return;
            if (!Main.mapEnabled || !Main.mapReady) return;

            List<StructurePin> currentPins;
            lock (_pins)
            {
                currentPins = new List<StructurePin>(_pins);
            }

            if (currentPins.Count == 0) return;

            string hoveredTooltip = null;

            foreach (StructurePin pin in currentPins)
            {
                // 分类开关过滤
                if (pin.Category == "Plantera" && !QoLValSet.markPlanteraBulb.val) continue;
                if (pin.Category == "SwordShrine" && !QoLValSet.markSwordShrine.val) continue;
                if (pin.Category == "Larva" && !QoLValSet.markBeeHive.val) continue;
                if (pin.Category == "Shimmer" && !QoLValSet.markShimmer.val) continue;
                if (pin.Category == "Pyramid" && !QoLValSet.markPyramid.val) continue;
                if (pin.Category == "Temple" && !QoLValSet.markTempleAltar.val) continue;

                // 1. 全屏大地图
                if (Main.mapFullscreen)
                {
                    Vector2 centerPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                    Vector2 drawPos = centerPos - Main.mapFullscreenPos * Main.mapFullscreenScale;
                    drawPos += pin.PositionInTiles * Main.mapFullscreenScale;

                    DrawPinMarker(drawPos, pin, 1.2f);

                    if (IsMouseHovering(drawPos, 14f * Main.UIScale))
                    {
                        hoveredTooltip = $"{pin.Name}\n坐标: [X: {(int)pin.PositionInTiles.X}, Y: {(int)pin.PositionInTiles.Y}]";
                    }
                }
                // 2. 右上角小地图
                else if (Main.mapStyle == 1)
                {
                    float scale = (Main.mapMinimapScale * 0.25f * 2f + 1f) / 3f;
                    if (scale > 1f) scale = 1f;

                    Vector2 worldCenter = Main.screenPosition;
                    worldCenter.X += PlayerInput.RealScreenWidth / 2f;
                    worldCenter.Y += PlayerInput.RealScreenHeight / 2f;

                    Vector2 offset = pin.PositionInTiles * 16f - worldCenter;
                    Vector2 drawPos = new Vector2(Main.miniMapX + Main.miniMapWidth / 2f, Main.miniMapY + Main.miniMapHeight / 2f);
                    drawPos += (offset / 16f) * Main.mapMinimapScale;

                    if (drawPos.X > Main.miniMapX + 4 &&
                        drawPos.X < Main.miniMapX + Main.miniMapWidth - 4 &&
                        drawPos.Y > Main.miniMapY + 4 &&
                        drawPos.Y < Main.miniMapY + Main.miniMapHeight - 4)
                    {
                        DrawPinMarker(drawPos, pin, scale);

                        if (IsMouseHovering(drawPos, 10f))
                        {
                            hoveredTooltip = $"{pin.Name}\n坐标: [X: {(int)pin.PositionInTiles.X}, Y: {(int)pin.PositionInTiles.Y}]";
                        }
                    }
                }
                // 3. 屏幕中央半透明覆盖地图
                else if (Main.mapStyle == 2)
                {
                    float scale = (Main.mapOverlayScale * 0.2f * 2f + 1f) / 3f;
                    if (scale > 1f) scale = 1f;
                    scale *= Main.UIScale;

                    Vector2 worldCenter = Main.screenPosition;
                    worldCenter.X += PlayerInput.RealScreenWidth / 2f;
                    worldCenter.Y += PlayerInput.RealScreenHeight / 2f;

                    Vector2 offset = pin.PositionInTiles * 16f - worldCenter;
                    Vector2 drawPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                    drawPos += (offset / 16f) * Main.mapOverlayScale;

                    DrawPinMarker(drawPos, pin, scale);

                    if (IsMouseHovering(drawPos, 12f * scale))
                    {
                        hoveredTooltip = $"{pin.Name}\n坐标: [X: {(int)pin.PositionInTiles.X}, Y: {(int)pin.PositionInTiles.Y}]";
                    }
                }
            }

            if (hoveredTooltip != null)
            {
                Main.instance.MouseText(hoveredTooltip);
            }
        }

        private static void DrawPinMarker(Vector2 pos, StructurePin pin, float scale)
        {
            Texture2D texture = TextureAssets.MagicPixel.Value;
            if (texture == null) return;

            // 绘制发光外框与核心标记
            int size = (int)(10 * scale);
            Rectangle rect = new Rectangle((int)pos.X - size / 2, (int)pos.Y - size / 2, size, size);

            // 黑色背景边框
            Main.spriteBatch.Draw(texture, new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), Color.Black * 0.8f);
            // 实体彩色核心
            Main.spriteBatch.Draw(texture, rect, pin.Color * 0.9f);
        }

        private static bool IsMouseHovering(Vector2 pos, float size)
        {
            return Main.mouseX >= pos.X - size && Main.mouseX <= pos.X + size &&
                   Main.mouseY >= pos.Y - size && Main.mouseY <= pos.Y + size;
        }
    }
}
