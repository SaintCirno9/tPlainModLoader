using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ObjectData;
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
            if (!GameMain.Wand_isEnable || GameMain.Wand_StructureMode != GameMain.StructureMode.Paste) return;

            StructureData data = StructureStorage.Clipboard;
            if (data == null || data.Tiles == null) return;

            Point mouseTile = Main.MouseWorld.ToTileCoordinates();
            int startX = mouseTile.X - data.OriginX;
            int startY = mouseTile.Y - data.OriginY;

            // 缺料掩码（与材料计划缓存同频刷新，仅计划失败时非空）
            TryGetPlan(data, mouseTile);
            bool[,] missingMask = _missingMask;

            Vector2 screenPos = new Vector2(startX * 16, startY * 16) - Main.screenPosition;
            int pixelWidth = data.Width * 16;
            int pixelHeight = data.Height * 16;

            // 1. 视口范围剔除（只绘制当前屏幕可见的图格，超大蓝图依然 60 帧丝滑）
            int minTileX = Math.Max(0, (int)Math.Floor(Main.screenPosition.X / 16f) - startX - 2);
            int maxTileX = Math.Min(data.Width - 1, (int)Math.Ceiling((Main.screenPosition.X + Main.screenWidth) / 16f) - startX + 2);
            int minTileY = Math.Max(0, (int)Math.Floor(Main.screenPosition.Y / 16f) - startY - 2);
            int maxTileY = Math.Min(data.Height - 1, (int)Math.Ceiling((Main.screenPosition.Y + Main.screenHeight) / 16f) - startY + 2);

            // 2. 第一阶段：绘制真实半透明背景墙纹理（仅在开启蓝图背景墙开关时绘制）
            if (GameMain.Wand_StructureIncludeWall)
            {
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

                                // 缺料格子以红色调渲染（含促动未激活的半透明态）
                                Color wallCol = (missingMask != null && missingMask[x, y])
                                    ? new Color(255, 80, 80) * 0.55f
                                    : Color.White * 0.55f;
                                sb.Draw(wallTex, drawPos - new Vector2(8, 8), src, wallCol);
                            }
                        }
                        catch { }
                    }
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

                            // 缺料格子以红色调渲染，其余保持原半透明材质
                            Color tileCol;
                            if (missingMask != null && missingMask[x, y])
                            {
                                tileCol = snap.InActive ? new Color(255, 80, 80) * 0.45f : new Color(255, 80, 80) * 0.75f;
                            }
                            else
                            {
                                tileCol = snap.InActive ? Color.White * 0.35f : Color.White * 0.70f;
                            }

                            // 1. 平台（Platforms）专属虚影渲染（区分顶部平台、下移半砖平台与楼梯平台）
                            if (snap.TileType == Terraria.ID.TileID.Platforms ||
                                (snap.TileType >= 0 && snap.TileType < Terraria.ID.TileID.Sets.Platforms.Length && Terraria.ID.TileID.Sets.Platforms[snap.TileType]))
                            {
                                if (snap.Slope == 5 || snap.HalfBlock)
                                {
                                    // 半砖平台：完整横杠切片下移 8 像素绘制
                                    Rectangle platSrc = new Rectangle(safeX, safeY, 16, 16);
                                    sb.Draw(tileTex, drawPos + new Vector2(0, 8), platSrc, tileCol);
                                }
                                else
                                {
                                    // 普通平台与楼梯平台：直接按准确切片绘制
                                    Rectangle platSrc = new Rectangle(safeX, safeY, 16, 16);
                                    sb.Draw(tileTex, drawPos, platSrc, tileCol);
                                }
                            }
                            // 2. 实体物块半砖渲染 (5=下半砖, 6=上半砖)
                            else if (snap.HalfBlock || snap.Slope == 5 || snap.Slope == 6)
                            {
                                int halfOffsetY = (snap.Slope == 6) ? 0 : 8;
                                int srcOffsetY = (snap.Slope == 6) ? 0 : 8;
                                Rectangle halfSrc = new Rectangle(safeX, safeY + srcOffsetY, 16, 8);
                                sb.Draw(tileTex, drawPos + new Vector2(0, halfOffsetY), halfSrc, tileCol);
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
        private static bool _lastConsume = false;
        private static bool _lastAutoCraft = false;
        private static bool _lastReqStation = false;
        private static bool _lastOverwrite = true;
        private static bool _lastIncludeWall = true;
        private static Point _lastMouseTile = Point.Zero;
        private static StructureCraftingEngine.CraftingPlan _cachedPlan = null;
        private static bool[,] _missingMask = null;

        /// <summary>
        /// 获取（或按缓存键重建）当前蓝图的材料计划与缺料掩码；免消耗模式返回 null。
        /// 缓存键：蓝图引用 + 背包指纹 + 配置开关 + 鼠标落点格（决定世界差量免除结果）。
        /// </summary>
        private static StructureCraftingEngine.CraftingPlan TryGetPlan(StructureData data, Point mouseTile)
        {
            Player player = Main.LocalPlayer;
            if (data == null || player == null) return null;

            // 剪切搬家零消耗：不构建计划与缺料掩码（避免整幅虚影误报标红与无效计算）
            if (GameMain.CutSourceRect.HasValue)
            {
                _cachedPlan = null;
                _missingMask = null;
                return null;
            }

            bool overwrite = GameMain.Wand_StructureOverwrite;
            bool includeWall = GameMain.Wand_StructureIncludeWall;
            bool consume = GameMain.Wand_StructureConsumeMaterials && ModConfig.IsConsumablesItem();
            if (!consume)
            {
                _cachedPlan = null;
                _missingMask = null;
                return null;
            }

            bool autoCraft = GameMain.Wand_StructureAutoCraft && ModConfig.IsAutoCraftMaterials();
            bool reqStation = GameMain.Wand_StructureAutoCraftRequireStation && ModConfig.IsAutoCraftRequireStation();
            int invHash = StructureCraftingEngine.GetInventoryHash(player);

            if (_cachedPlan == null || _lastInvHash != invHash || _lastData != data || _lastConsume != consume
                || _lastAutoCraft != autoCraft || _lastReqStation != reqStation || _lastOverwrite != overwrite || _lastIncludeWall != includeWall || _lastMouseTile != mouseTile)
            {
                _cachedPlan = StructureCraftingEngine.BuildPlan(data, player, autoCraft, reqStation, mouseTile, overwrite);
                _lastInvHash = invHash;
                _lastData = data;
                _lastConsume = consume;
                _lastAutoCraft = autoCraft;
                _lastReqStation = reqStation;
                _lastOverwrite = overwrite;
                _lastIncludeWall = includeWall;
                _lastMouseTile = mouseTile;
                _missingMask = BuildMissingMask(data, mouseTile, overwrite, _cachedPlan);
            }
            return _cachedPlan;
        }

        /// <summary>
        /// 逐格构建缺料掩码：按 GetRequiredItems 同款差量/锚点规则判定每格所需物品是否命中计划缺失集合。
        /// 仅当计划失败且缺失集合非空时返回非 null；多格家具缺料时整件标红。
        /// </summary>
        private static bool[,] BuildMissingMask(StructureData data, Point mouseTile, bool overwrite, StructureCraftingEngine.CraftingPlan plan)
        {
            if (plan == null || plan.IsPossible || plan.MissingItemIds.Count == 0) return null;

            int w = data.Width;
            int h = data.Height;
            bool[,] mask = new bool[w, h];
            bool[,] counted = new bool[w, h];
            int startX = mouseTile.X - data.OriginX;
            int startY = mouseTile.Y - data.OriginY;
            HashSet<int> missing = plan.MissingItemIds;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    TileSnapshot snap = data.Tiles[x, y];
                    int wx = startX + x;
                    int wy = startY + y;
                    bool inWorldBounds = wx >= 0 && wx < Main.maxTilesX && wy >= 0 && wy < Main.maxTilesY;
                    Tile worldTile = inWorldBounds ? Main.tile[wx, wy] : null;

                    // 1. 背景墙：与 GetRequiredItems 相同的差量免除规则（仅在开启背景墙开关时）
                    if (GameMain.Wand_StructureIncludeWall && snap.HasWall)
                    {
                        bool wallAlreadySame = worldTile != null && worldTile.wall == snap.WallType;
                        if (!wallAlreadySame && (overwrite || worldTile == null || worldTile.wall == 0))
                        {
                            int wallItem = StructureData.GetWallItemId(snap.WallType);
                            if (wallItem > 0 && missing.Contains(wallItem)) mask[x, y] = true;
                        }
                    }

                    // 2. 物块与家具：锚点定料，多格整件展开
                    if (snap.HasTile && !counted[x, y])
                    {
                        bool isSame = inWorldBounds && StructureData.IsTileIdentical(worldTile, snap);
                        if (isSame)
                        {
                            // 世界上已有相同物块/家具，整件标记为已处理
                            MarkCounted(counted, data, x, y, snap.TileType);
                            continue;
                        }

                        // 非覆盖模式下若世界该处已被其他物块占据，跳过
                        if (!overwrite && worldTile != null && worldTile.active())
                        {
                            counted[x, y] = true;
                            continue;
                        }

                        TileObjectData od = TileObjectData.GetTileData(snap.TileType, 0);
                        if (od != null && (od.Width > 1 || od.Height > 1))
                        {
                            MarkCounted(counted, data, x, y, snap.TileType);
                            int tileItem = StructureData.GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                            if (tileItem > 0 && missing.Contains(tileItem))
                            {
                                for (int dx = 0; dx < od.Width && x + dx < w; dx++)
                                {
                                    for (int dy = 0; dy < od.Height && y + dy < h; dy++)
                                    {
                                        mask[x + dx, y + dy] = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            counted[x, y] = true;
                            int tileItem = StructureData.GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                            if (tileItem > 0 && missing.Contains(tileItem)) mask[x, y] = true;
                        }
                    }

                    // 3. 电线与促动器
                    if (missing.Contains(ItemID.Wire))
                    {
                        if ((snap.RedWire && (worldTile == null || !worldTile.wire())) ||
                            (snap.GreenWire && (worldTile == null || !worldTile.wire3())) ||
                            (snap.BlueWire && (worldTile == null || !worldTile.wire2())) ||
                            (snap.YellowWire && (worldTile == null || !worldTile.wire4())))
                        {
                            mask[x, y] = true;
                        }
                    }
                    if (snap.Actuator && (worldTile == null || !worldTile.actuator()) && missing.Contains(ItemID.Actuator))
                    {
                        mask[x, y] = true;
                    }
                }
            }

            return mask;
        }

        /// <summary>标记多格家具在掩码计数中的全部覆盖格（锚点展开规则与 GetRequiredItems 一致）</summary>
        private static void MarkCounted(bool[,] counted, StructureData data, int x, int y, int tileType)
        {
            int w = data.Width;
            int h = data.Height;
            TileObjectData od = TileObjectData.GetTileData(tileType, 0);
            if (od != null && (od.Width > 1 || od.Height > 1))
            {
                for (int dx = 0; dx < od.Width && x + dx < w; dx++)
                {
                    for (int dy = 0; dy < od.Height && y + dy < h; dy++)
                    {
                        counted[x + dx, y + dy] = true;
                    }
                }
            }
            else
            {
                counted[x, y] = true;
            }
        }

        private static void DrawMaterialTooltip(StructureData data)
        {
            try
            {
                if (GameMain.CutSourceRect.HasValue)
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
                bool overwrite = GameMain.Wand_StructureOverwrite;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[蓝图放置] {data.Name} ({data.Width}×{data.Height})");
                sb.AppendLine($"快捷键: [H] 水平镜像 | [V] 垂直翻转");
                bool consume = GameMain.Wand_StructureConsumeMaterials && ModConfig.IsConsumablesItem();
                bool autoCraft = GameMain.Wand_StructureAutoCraft && ModConfig.IsAutoCraftMaterials();
                bool reqStation = GameMain.Wand_StructureAutoCraftRequireStation && ModConfig.IsAutoCraftRequireStation();

                string consumeText = !consume ? "关闭 (免消耗自由摆放)" : (autoCraft ? "开启 (缺料自动消耗原料制造)" : "开启 (需备齐成品材料)");
                sb.AppendLine($"材料消耗: {consumeText} [覆盖:{(overwrite ? "开" : "关")}] [背景墙:{(GameMain.Wand_StructureIncludeWall ? "开" : "关")}]");
                sb.AppendLine($"操作: [左键] 确认放置 | [右键] 取消放置");
                sb.AppendLine($"-------------------");

                Dictionary<int, int> req = data.GetRequiredItems(mouseTile, overwrite);
                Player player = Main.LocalPlayer;

                StructureCraftingEngine.CraftingPlan plan = TryGetPlan(data, mouseTile);

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
