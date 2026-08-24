using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 六阶段分步安全放置引擎（物理分层支撑、差量智能比对、防家具崩塌、原子性剪切平移与网络同步）
    /// </summary>
    public static class StructurePlacement
    {
        /// <summary>
        /// 放置蓝图结构或执行剪切平移搬家
        /// </summary>
        public static bool Place(StructureData data, Point originWorldTile, Player player, bool overwrite = true)
        {
            if (data == null || data.Tiles == null || player == null) return false;

            try
            {
                bool isCutRelocation = gameMain.CutSourceRect.HasValue;
                Rectangle? cutSrc = gameMain.CutSourceRect;

                // 0. 前置材料校验与原子扣除：仅常规蓝图放置且开启材料消耗时执行；差量智能比对：世界已有相同材料直接免除！
                if (!isCutRelocation && gameMain.Wand_StructureConsumeMaterials && ModConfig.IsConsumablesItem())
                {
                    bool allowAutoCraft = gameMain.Wand_StructureAutoCraft && ModConfig.IsAutoCraftMaterials();
                    bool reqStation = gameMain.Wand_StructureAutoCraftRequireStation && ModConfig.IsAutoCraftRequireStation();
                    var plan = StructureCraftingEngine.BuildPlan(data, player, allowAutoCraft, reqStation, originWorldTile, overwrite);

                    if (!plan.IsPossible)
                    {
                        string missingText = string.Join("，", plan.MissingMessages);
                        Main.NewText($"[魔杖] 材料不足！缺少: {missingText}", 255, 80, 80);
                        CombatText.NewText(player.getRect(), Color.Red, "材料不足！", true, false);
                        return false;
                    }

                    // 前置原子执行材料与原材料扣减，以及余数返还
                    StructureCraftingEngine.ExecutePlan(plan, player);
                }

                // 0.1 若为剪切平移，在正式落下的瞬间，原子性清除原区域建筑（无地面掉落碎屑，保障世界平移干净利落）
                if (isCutRelocation && cutSrc.HasValue)
                {
                    Rectangle src = cutSrc.Value;
                    for (int x = src.X; x < src.X + src.Width; x++)
                    {
                        for (int y = src.Y; y < src.Y + src.Height; y++)
                        {
                            if (!InBounds(x, y)) continue;

                            Tile tile = Main.tile[x, y];
                            if (tile != null)
                            {
                                tile.ClearTile();
                                tile.wall = 0;
                            }
                        }
                    }

                    if (Main.netMode == 1)
                    {
                        NetMessage.SendTileSquare(player.whoAmI, src.X, src.Y, src.Width, src.Height);
                    }

                    gameMain.CutSourceRect = null; // 搬家任务完成，重置标记
                }

                int startX = originWorldTile.X - data.OriginX;
                int startY = originWorldTile.Y - data.OriginY;

                int minX = Math.Max(0, startX);
                int minY = Math.Max(0, startY);
                int maxX = Math.Min(Main.tile.GetLength(0) - 1, startX + data.Width - 1);
                int maxY = Math.Min(Main.tile.GetLength(1) - 1, startY + data.Height - 1);

                int w = data.Width;
                int h = data.Height;

                // 1. 阶段 1：清理区域已有不一致的物块与墙壁（覆盖模式：被拆除的旧物块/墙壁/家具自动回收返还背包！）
                if (overwrite)
                {
                    bool[,] oldTileRefunded = new bool[w, h];
                    Dictionary<int, int> recycledItems = new Dictionary<int, int>();

                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int wx = startX + x;
                            int wy = startY + y;
                            if (!InBounds(wx, wy)) continue;

                            Tile tile = Main.tile[wx, wy];
                            if (tile == null) continue;

                            TileSnapshot snap = data.Tiles[x, y];

                            // 若世界已有物块与蓝图不同，拆除并回收旧物块材料
                            if (tile.active() && !StructureData.IsTileIdentical(tile, snap))
                            {
                                if (gameMain.Wand_CollectDrops && !isCutRelocation && !oldTileRefunded[x, y])
                                {
                                    TileObjectData objData = TileObjectData.GetTileData(tile.type, 0);
                                    if (objData != null && (objData.Width > 1 || objData.Height > 1))
                                    {
                                        for (int dx = 0; dx < objData.Width && x + dx < w; dx++)
                                        {
                                            for (int dy = 0; dy < objData.Height && y + dy < h; dy++)
                                            {
                                                oldTileRefunded[x + dx, y + dy] = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        oldTileRefunded[x, y] = true;
                                    }

                                    int dropItem = StructureData.GetTileItemId(tile.type, tile.frameX, tile.frameY);
                                    if (dropItem > 0)
                                    {
                                        if (recycledItems.ContainsKey(dropItem)) recycledItems[dropItem] += 1;
                                        else recycledItems[dropItem] = 1;

                                        Item refund = new Item();
                                        refund.SetDefaults(dropItem);
                                        refund.stack = 1;
                                        Item left = player.GetItem(refund, GetItemSettings.PickupItemFromWorld);
                                        if (left != null && !left.IsAir && left.stack > 0)
                                        {
                                            player.QuickSpawnItem(player.GetItemSource_InventoryOverflow(), left.type, left.stack);
                                        }
                                    }
                                }

                                tile.ClearTile();
                            }

                            // 若世界已有墙壁与蓝图不同，拆除并回收旧墙壁材料
                            if (tile.wall > 0 && (!snap.HasWall || tile.wall != snap.WallType))
                            {
                                if (gameMain.Wand_CollectDrops && !isCutRelocation)
                                {
                                    int dropWall = StructureData.GetWallItemId(tile.wall);
                                    if (dropWall > 0)
                                    {
                                        if (recycledItems.ContainsKey(dropWall)) recycledItems[dropWall] += 1;
                                        else recycledItems[dropWall] = 1;

                                        Item refund = new Item();
                                        refund.SetDefaults(dropWall);
                                        refund.stack = 1;
                                        Item left = player.GetItem(refund, GetItemSettings.PickupItemFromWorld);
                                        if (left != null && !left.IsAir && left.stack > 0)
                                        {
                                            player.QuickSpawnItem(player.GetItemSource_InventoryOverflow(), left.type, left.stack);
                                        }
                                    }
                                }

                                tile.wall = 0;
                            }
                        }
                    }

                    // 弹出回收通知
                    if (recycledItems.Count > 0)
                    {
                        List<string> recycledList = new List<string>();
                        foreach (var kvp in recycledItems)
                        {
                            string name = Lang.GetItemNameValue(kvp.Key);
                            recycledList.Add($"{kvp.Value}×{name}");
                        }
                        string summary = string.Join("，", recycledList);
                        Main.NewText($"[魔杖] 覆盖拆除已回收旧材料至背包: {summary}", 255, 215, 0);
                    }
                }
                // 2. 阶段 2：铺设背景墙（差量铺设：仅在墙壁不一致时写入）
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int wx = startX + x;
                        int wy = startY + y;
                        if (!InBounds(wx, wy)) continue;

                        TileSnapshot snap = data.Tiles[x, y];
                        Tile t = Main.tile[wx, wy];
                        if (t == null) continue;

                        if (snap.HasWall)
                        {
                            if (t.wall != snap.WallType && (overwrite || t.wall == 0))
                            {
                                t.wall = (ushort)snap.WallType;
                                if (snap.WallColor > 0) t.wallColor(snap.WallColor);
                            }
                        }
                    }
                }

                // 3. 阶段 3：自底向上铺设实体支撑方块与平台（先打好地板与承重基础）
                for (int x = 0; x < w; x++)
                {
                    for (int y = h - 1; y >= 0; y--) // 自底向上
                    {
                        int wx = startX + x;
                        int wy = startY + y;
                        if (!InBounds(wx, wy)) continue;

                        TileSnapshot snap = data.Tiles[x, y];
                        Tile t = Main.tile[wx, wy];
                        if (t == null) continue;

                        if (snap.HasTile)
                        {
                            bool isFrameImportant = Main.tileFrameImportant[snap.TileType];
                            // 仅处理实体方块与平台
                            if (!isFrameImportant || snap.TileType == TileID.Platforms)
                            {
                                bool isSame = StructureData.IsTileIdentical(t, snap);
                                if (!isSame && (overwrite || !t.active()))
                                {
                                    t.active(true);
                                    t.type = (ushort)snap.TileType;
                                    t.frameX = snap.TileFrameX;
                                    t.frameY = snap.TileFrameY;

                                    // 恢复斜坡 / 半砖
                                    if (snap.Slope == 5)
                                    {
                                        t.halfBrick(true);
                                        t.slope(0);
                                    }
                                    else if (snap.Slope > 0)
                                    {
                                        t.halfBrick(false);
                                        t.slope(snap.Slope);
                                    }
                                    else
                                    {
                                        t.halfBrick(false);
                                        t.slope(0);
                                    }

                                    if (snap.TileColor > 0) t.color(snap.TileColor);
                                    if (snap.InActive) t.inActive(true);
                                }
                                else if (isSame)
                                {
                                    // 相同方块仅无损同步斜坡和涂层
                                    if (snap.Slope == 5) t.halfBrick(true);
                                    else if (snap.Slope > 0) t.slope(snap.Slope);
                                    if (snap.TileColor > 0) t.color(snap.TileColor);
                                    if (snap.InActive) t.inActive(true);
                                }
                            }
                        }
                    }
                }

                // 4. 阶段 4：仅对实体方块与墙壁执行 Framing 连接相融（家具绝对不执行 Framing，彻底杜绝崩塌）
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int wx = startX + x;
                        int wy = startY + y;
                        if (!InBounds(wx, wy)) continue;

                        Tile t = Main.tile[wx, wy];
                        if (t != null)
                        {
                            if (t.active() && !Main.tileFrameImportant[t.type])
                            {
                                WorldGen.SquareTileFrame(wx, wy, true);
                            }
                            if (t.wall > 0)
                            {
                                WorldGen.SquareWallFrame(wx, wy, true);
                            }
                        }
                    }
                }

                // 5. 阶段 5：铺设所有多格家具与装饰物（椅子、桌子、床、箱子、门、火把、吊灯等，稳稳落位在已建好的地板上）
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int wx = startX + x;
                        int wy = startY + y;
                        if (!InBounds(wx, wy)) continue;

                        TileSnapshot snap = data.Tiles[x, y];
                        Tile t = Main.tile[wx, wy];
                        if (t == null) continue;

                        if (snap.HasTile)
                        {
                            bool isFrameImportant = Main.tileFrameImportant[snap.TileType];
                            if (isFrameImportant && snap.TileType != TileID.Platforms)
                            {
                                bool isSame = StructureData.IsTileIdentical(t, snap);
                                // 若世界上已有完全相同的家具，直接保留，不做任何破坏与重建！
                                if (!isSame && (overwrite || !t.active()))
                                {
                                    t.active(true);
                                    t.type = (ushort)snap.TileType;
                                    t.frameX = snap.TileFrameX;
                                    t.frameY = snap.TileFrameY;
                                    t.halfBrick(false);
                                    t.slope(0);

                                    if (snap.TileColor > 0) t.color(snap.TileColor);
                                    if (snap.InActive) t.inActive(true);
                                }
                            }
                        }
                    }
                }

                // 6. 阶段 6：铺设四色电线、制动器与涂层
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int wx = startX + x;
                        int wy = startY + y;
                        if (!InBounds(wx, wy)) continue;

                        TileSnapshot snap = data.Tiles[x, y];
                        Tile t = Main.tile[wx, wy];
                        if (t == null) continue;

                        if (snap.RedWire) t.wire(true);
                        if (snap.GreenWire) t.wire3(true);
                        if (snap.BlueWire) t.wire2(true);
                        if (snap.YellowWire) t.wire4(true);
                        if (snap.Actuator) t.actuator(true);

                        if ((snap.Coating & 1) != 0) t.fullbrightBlock(true);
                        if ((snap.Coating & 2) != 0) t.invisibleBlock(true);
                        if ((snap.Coating & 4) != 0) t.fullbrightWall(true);
                        if ((snap.Coating & 8) != 0) t.invisibleWall(true);
                    }
                }

                // 7. 阶段 7：网络同步
                if (Main.netMode == 1)
                {
                    NetMessage.SendTileSquare(player.whoAmI, minX, minY, maxX - minX + 1, maxY - minY + 1);
                }

                // 8. 阶段 8：标牌文本恢复
                if (data.SignTexts != null && data.SignTexts.Count > 0)
                {
                    int signIndex = 0;
                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int wx = startX + x;
                            int wy = startY + y;
                            if (!InBounds(wx, wy)) continue;

                            Tile t = Main.tile[wx, wy];
                            if (t != null && t.active() && Main.tileSign[t.type])
                            {
                                if (t.frameX / 18 % 2 == 0 && t.frameY / 18 == 0)
                                {
                                    int sId = Sign.ReadSign(wx, wy, true);
                                    if (sId >= 0 && signIndex < data.SignTexts.Count)
                                    {
                                        Sign.TextSign(sId, data.SignTexts[signIndex++]);
                                    }
                                }
                            }
                        }
                    }
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, player.position);
                if (isCutRelocation)
                {
                    StructureStorage.Clipboard = null;
                    gameMain.Wand_StructureMode = gameMain.StructureMode.Cut;
                }
                string finishMsg = isCutRelocation ? "平移成功 (回到剪切模式)" : $"已放置 {data.Name}";
                CombatText.NewText(player.getRect(), isCutRelocation ? Color.Gold : Color.LimeGreen, finishMsg, true, false);
                return true;
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 放置结构时发生异常: {ex.Message}", 255, 80, 80);
                return false;
            }
        }

        /// <summary>
        /// 校验玩家背包物料是否足以摆放该蓝图
        /// </summary>
        public static bool CheckMaterials(StructureData data, Player player, out List<string> missingItems, Point? originWorldTile = null, bool overwrite = true)
        {
            missingItems = new List<string>();
            if (data == null || player == null) return true;

            bool allowAutoCraft = gameMain.Wand_StructureAutoCraft && ModConfig.IsAutoCraftMaterials();
            bool reqStation = gameMain.Wand_StructureAutoCraftRequireStation && ModConfig.IsAutoCraftRequireStation();
            var plan = StructureCraftingEngine.BuildPlan(data, player, allowAutoCraft, reqStation, originWorldTile, overwrite);

            if (!plan.IsPossible)
            {
                missingItems = plan.MissingMessages;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 统计玩家背包中特定物品的总数量
        /// </summary>
        public static int CountItemInInventory(Player player, int itemId)
        {
            if (player?.inventory == null || itemId <= 0) return 0;
            int total = 0;
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.type == itemId)
                {
                    total += item.stack;
                }
            }
            return total;
        }

        private static bool InBounds(int x, int y)
        {
            return x >= 0 && x < Main.tile.GetLength(0) && y >= 0 && y < Main.tile.GetLength(1);
        }
    }
}
