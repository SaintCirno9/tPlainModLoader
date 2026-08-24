using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 蓝图真实物块/墙壁纹理半透明虚影预览与物料统计提示渲染器
    /// </summary>
    public static class StructurePreview
    {
        public static void Draw(SpriteBatch sb)
        {
            if (!gameMain.Wand_isEnable || gameMain.Wand_StructureMode != gameMain.StructureMode.Paste) return;

            StructureData data = StructureStorage.Clipboard;
            if (data == null || data.Tiles == null) return;

            Point mouseTile = Main.MouseWorld.ToTileCoordinates();
            int startX = mouseTile.X - data.OriginX;
            int startY = mouseTile.Y - data.OriginY;

            Vector2 screenPos = new Vector2(startX * 16, startY * 16) - Main.screenPosition;
            int pixelWidth = data.Width * 16;
            int pixelHeight = data.Height * 16;

            // 1. 视口范围剔除（只绘制当前屏幕可见的图格，超大蓝图依然 60 帧丝滑）
            int minTileX = Math.Max(0, (int)Math.Floor(Main.screenPosition.X / 16f) - startX - 2);
            int maxTileX = Math.Min(data.Width - 1, (int)Math.Ceiling((Main.screenPosition.X + Main.screenWidth) / 16f) - startX + 2);
            int minTileY = Math.Max(0, (int)Math.Floor(Main.screenPosition.Y / 16f) - startY - 2);
            int maxTileY = Math.Min(data.Height - 1, (int)Math.Ceiling((Main.screenPosition.Y + Main.screenHeight) / 16f) - startY + 2);

            // 2. 第一阶段：绘制真实半透明背景墙纹理
            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    TileSnapshot snap = data.Tiles[x, y];
                    if (!snap.HasWall || snap.WallType <= 0 || snap.WallType >= TextureAssets.Wall.Length) continue;

                    try
                    {
                        Main.instance.LoadWall(snap.WallType);
                        Asset<Texture2D> wallAsset = TextureAssets.Wall[snap.WallType];
                        if (wallAsset?.Value != null)
                        {
                            Texture2D wallTex = wallAsset.Value;
                            Vector2 drawPos = new Vector2((startX + x) * 16, (startY + y) * 16) - Main.screenPosition;

                            // 原版墙壁贴图为 32x32 纹理，相对图格偏移 (-8, -8)
                            int frameX = snap.WallFrameX;
                            int frameY = snap.WallFrameY;
                            Rectangle src = new Rectangle(
                                Math.Max(0, Math.Min(frameX, wallTex.Width - 32)),
                                Math.Max(0, Math.Min(frameY, wallTex.Height - 32)),
                                32, 32
                            );

                            Color wallCol = Color.White * 0.55f;
                            sb.Draw(wallTex, drawPos - new Vector2(8, 8), src, wallCol);
                        }
                    }
                    catch { }
                }
            }

            // 3. 第二阶段：绘制真实半透明物块/家具/斜坡纹理
            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    TileSnapshot snap = data.Tiles[x, y];
                    if (!snap.HasTile || snap.TileType < 0 || snap.TileType >= TextureAssets.Tile.Length) continue;

                    try
                    {
                        Main.instance.LoadTiles(snap.TileType);
                        Asset<Texture2D> tileAsset = TextureAssets.Tile[snap.TileType];
                        if (tileAsset?.Value != null)
                        {
                            Texture2D tileTex = tileAsset.Value;
                            Vector2 drawPos = new Vector2((startX + x) * 16, (startY + y) * 16) - Main.screenPosition;
                            int frameX = snap.TileFrameX;
                            int frameY = snap.TileFrameY;
                            int safeX = Math.Max(0, Math.Min(frameX, tileTex.Width - 16));
                            int safeY = Math.Max(0, Math.Min(frameY, tileTex.Height - 16));
                            Rectangle tileSrc = new Rectangle(safeX, safeY, 16, 16);

                            Color tileCol = snap.InActive ? Color.White * 0.35f : Color.White * 0.70f;

                            // 1. 平台（Platforms）专属虚影渲染（区分顶部平台、下移半砖平台与楼梯平台）
                            if (snap.TileType == Terraria.ID.TileID.Platforms ||
                                (snap.TileType >= 0 && snap.TileType < Terraria.ID.TileID.Sets.Platforms.Length && Terraria.ID.TileID.Sets.Platforms[snap.TileType]))
                            {
                                if (snap.Slope == 1 || snap.Slope == 2)
                                {
                                    // 楼梯平台切片 (198=下行坡, 162=上行坡)
                                    int frameXPlatform = (snap.Slope == 1) ? 198 : 162;
                                    Rectangle platSrc = new Rectangle(frameXPlatform, safeY, 16, 16);
                                    sb.Draw(tileTex, drawPos, platSrc, tileCol);
                                }
                                else if (snap.Slope == 5 || snap.HalfBlock)
                                {
                                    // 半砖平台：完整横杠切片下移 8 像素绘制
                                    Rectangle platSrc = new Rectangle(safeX, safeY, 16, 16);
                                    sb.Draw(tileTex, drawPos + new Vector2(0, 8), platSrc, tileCol);
                                }
                                else
                                {
                                    // 普通顶部平台：正常绘制在顶部
                                    Rectangle platSrc = new Rectangle(safeX, safeY, 16, 16);
                                    sb.Draw(tileTex, drawPos, platSrc, tileCol);
                                }
                            }
                            // 2. 实体物块半砖渲染 (5=下半砖, 6=上半砖)
                            else if (snap.HalfBlock || snap.Slope == 5 || snap.Slope == 6)
                            {
                                int halfOffsetY = (snap.Slope == 6) ? 0 : 8;
                                tileSrc.Height = 8;
                                sb.Draw(tileTex, drawPos + new Vector2(0, halfOffsetY), tileSrc, tileCol);
                            }
                            // 3. 原版级精准实体斜坡切片渲染
                            else if (snap.Slope >= 1 && snap.Slope <= 4)
                            {
                                int slopeType = (int)snap.Slope;
                                int sliceWidth = 2;
                                for (int i = 0; i < 8; i++)
                                {
                                    int num3 = i * -2;
                                    int num4 = 16 - i * 2;
                                    int num5 = 16 - num4;
                                    int num6;
                                    switch (slopeType)
                                    {
                                        case 1: // SlopeDownRight (左上至右下, 顶部直角)
                                            num3 = 0;
                                            num6 = i * 2;
                                            num4 = 14 - i * 2;
                                            num5 = 0;
                                            break;
                                        case 2: // SlopeDownLeft (右上至左下, 顶部直角)
                                            num3 = 0;
                                            num6 = 16 - i * 2 - 2;
                                            num4 = 14 - i * 2;
                                            num5 = 0;
                                            break;
                                        case 3: // SlopeUpRight (左下至右上, 底部直角)
                                            num6 = i * 2;
                                            break;
                                        default: // case 4: SlopeUpLeft (右下至左上, 底部直角)
                                            num6 = 16 - i * 2 - 2;
                                            break;
                                    }

                                    Rectangle sliceSrc = new Rectangle(safeX + num6, safeY + num5, sliceWidth, num4);
                                    Vector2 slicePos = drawPos + new Vector2(num6, i * sliceWidth + num3);
                                    sb.Draw(tileTex, slicePos, sliceSrc, tileCol);
                                }
                                int num20 = (slopeType <= 2) ? 14 : 0;
                                sb.Draw(tileTex, drawPos + new Vector2(0f, num20), new Rectangle(safeX, safeY + num20, 16, 2), tileCol);
                            }
                            // 4. 标准完整单格 / 多格家具图元
                            else
                            {
                                sb.Draw(tileTex, drawPos, tileSrc, tileCol);
                            }
                        }
                    }
                    catch { }
                }
            }

            // 4. 第三阶段：绘制电线与促动器指示
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            if (magicPixel != null)
            {
                for (int x = minTileX; x <= maxTileX; x++)
                {
                    for (int y = minTileY; y <= maxTileY; y++)
                    {
                        TileSnapshot snap = data.Tiles[x, y];
                        if (!snap.RedWire && !snap.GreenWire && !snap.BlueWire && !snap.YellowWire && !snap.Actuator) continue;

                        Vector2 drawPos = new Vector2((startX + x) * 16, (startY + y) * 16) - Main.screenPosition;
                        if (snap.RedWire) sb.Draw(magicPixel, new Rectangle((int)drawPos.X + 2, (int)drawPos.Y + 2, 4, 4), Color.Red * 0.8f);
                        if (snap.GreenWire) sb.Draw(magicPixel, new Rectangle((int)drawPos.X + 10, (int)drawPos.Y + 2, 4, 4), Color.Lime * 0.8f);
                        if (snap.BlueWire) sb.Draw(magicPixel, new Rectangle((int)drawPos.X + 2, (int)drawPos.Y + 10, 4, 4), Color.DeepSkyBlue * 0.8f);
                        if (snap.YellowWire) sb.Draw(magicPixel, new Rectangle((int)drawPos.X + 10, (int)drawPos.Y + 10, 4, 4), Color.Yellow * 0.8f);
                        if (snap.Actuator) sb.Draw(magicPixel, new Rectangle((int)drawPos.X + 6, (int)drawPos.Y + 6, 4, 4), Color.Gray * 0.8f);
                    }
                }

                // 绘制整体建筑蓝图区域的青色半透明外边框
                Rectangle previewRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, pixelWidth, pixelHeight);
                Color borderColor = Color.Cyan * 0.9f;
                sb.Draw(magicPixel, new Rectangle(previewRect.X, previewRect.Y, previewRect.Width, 2), borderColor);
                sb.Draw(magicPixel, new Rectangle(previewRect.X, previewRect.Bottom - 2, previewRect.Width, 2), borderColor);
                sb.Draw(magicPixel, new Rectangle(previewRect.X, previewRect.Y, 2, previewRect.Height), borderColor);
                sb.Draw(magicPixel, new Rectangle(previewRect.Right - 2, previewRect.Y, 2, previewRect.Height), borderColor);
            }

            // 5. 绘制光标悬浮物料清单与快捷操作提示
            DrawMaterialTooltip(data);
        }
        private static int _lastInvHash = -1;
        private static StructureData _lastData = null;
        private static bool _lastAutoCraft = false;
        private static bool _lastReqStation = false;
        private static bool _lastOverwrite = true;
        private static Point _lastMouseTile = Point.Zero;
        private static StructureCraftingEngine.CraftingPlan _cachedPlan = null;

        private static void DrawMaterialTooltip(StructureData data)
        {
            try
            {
                if (gameMain.CutSourceRect.HasValue)
                {
                    StringBuilder sbCut = new StringBuilder();
                    sbCut.AppendLine($"[✂️ 建筑剪切搬家] {data.Name} ({data.Width}×{data.Height})");
                    sbCut.AppendLine($"快捷键: [H] 水平镜像 | [V] 垂直翻转");
                    sbCut.AppendLine($"状态: 搬家中 (原建筑完好未受损)");
                    sbCut.AppendLine($"材料消耗: 零消耗 (原建筑整栋物理转移)");
                    sbCut.AppendLine($"-------------------");
                    sbCut.AppendLine($"[左键] 确认放置完成搬家");
                    sbCut.AppendLine($"[右键] 取消 (原建筑完全不变)");
                    Main.instance.MouseText(sbCut.ToString().TrimEnd());
                    return;
                }

                Point mouseTile = Main.MouseWorld.ToTileCoordinates();
                bool overwrite = gameMain.Wand_StructureOverwrite;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[蓝图放置] {data.Name} ({data.Width}×{data.Height})");
                sb.AppendLine($"快捷键: [H] 水平镜像 | [V] 垂直翻转");
                bool consume = gameMain.Wand_StructureConsumeMaterials && ModConfig.IsConsumablesItem();
                bool autoCraft = gameMain.Wand_StructureAutoCraft && ModConfig.IsAutoCraftMaterials();
                bool reqStation = gameMain.Wand_StructureAutoCraftRequireStation && ModConfig.IsAutoCraftRequireStation();

                string consumeText = !consume ? "关闭 (免消耗自由摆放)" : (autoCraft ? "开启 (缺料自动消耗原料制造)" : "开启 (需备齐成品材料)");
                sb.AppendLine($"材料消耗: {consumeText} [覆盖:{(overwrite ? "开" : "关")}]");
                sb.AppendLine($"操作: [左键] 确认放置 | [右键] 取消放置");
                sb.AppendLine($"-------------------");

                Dictionary<int, int> req = data.GetRequiredItems(mouseTile, overwrite);
                Player player = Main.LocalPlayer;

                StructureCraftingEngine.CraftingPlan plan = null;
                if (consume)
                {
                    int invHash = StructureCraftingEngine.GetInventoryHash(player);
                    if (_cachedPlan == null || _lastInvHash != invHash || _lastData != data || _lastAutoCraft != autoCraft || _lastReqStation != reqStation || _lastOverwrite != overwrite || _lastMouseTile != mouseTile)
                    {
                        _cachedPlan = StructureCraftingEngine.BuildPlan(data, player, autoCraft, reqStation, mouseTile, overwrite);
                        _lastInvHash = invHash;
                        _lastData = data;
                        _lastAutoCraft = autoCraft;
                        _lastReqStation = reqStation;
                        _lastOverwrite = overwrite;
                        _lastMouseTile = mouseTile;
                    }
                    plan = _cachedPlan;
                }
                int lineCount = 0;
                foreach (var kvp in req)
                {
                if (lineCount++ >= 6) // 最多展示前 6 种关键物料
                {
                    sb.AppendLine($"... 及其余 {req.Count - 6} 种材料");
                    break;
                }

                int itemId = kvp.Key;
                int count = kvp.Value;
                int owned = StructurePlacement.CountItemInInventory(player, itemId);
                string itemName = Lang.GetItemNameValue(itemId);

                if (!consume || owned >= count)
                {
                    sb.AppendLine($"{itemName}: {count} (拥有: {owned}) ✔");
                }
                else if (plan != null && plan.IsPossible && plan.CraftedCounts.ContainsKey(itemId))
                {
                    int craftNum = plan.CraftedCounts[itemId];
                    sb.AppendLine($"{itemName}: {count} (拥有: {owned} [自动合成: +{craftNum}]) ✔");
                }
                else
                {
                    sb.AppendLine($"{itemName}: {count} (拥有: {owned}) ✘");
                }
            }

            if (plan != null && plan.HasCrafting && plan.IsPossible)
            {
                sb.AppendLine($"-------------------");
                string craftList = string.Join("，", plan.CraftedCounts.Take(3).Select(kv => $"{kv.Value}×{Lang.GetItemNameValue(kv.Key)}"));
                if (plan.CraftedCounts.Count > 3) craftList += $" 等{plan.CraftedCounts.Count}种";
                sb.AppendLine($"🛠️ 自动代工制造: {craftList}");
            }
            else if (plan != null && !plan.IsPossible && plan.MissingMessages.Count > 0)
            {
                sb.AppendLine($"-------------------");
                string missingList = string.Join("，", plan.MissingMessages.Take(2));
                sb.AppendLine($"⚠️ 材料不足: {missingList}");
            }

                Main.instance.MouseText(sb.ToString().TrimEnd());
            }
            catch
            {
                // 静默容错，避免任何异常冒泡到 XNA Draw 线程
            }
        }
    }
}
