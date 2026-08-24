using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 六阶段分步安全放置引擎（防多格家具崩塌、原子性剪切平移、物料前置校验与扣除、网络同步）
    /// </summary>
    public static class StructurePlacement
    {
        /// <summary>
        /// 放置蓝图结构或执行剪切平移搬家
        /// </summary>
        public static bool Place(StructureData data, Point originWorldTile, Player player, bool overwrite = true)
        {
            if (data == null || data.Tiles == null || player == null) return false;

            bool isCutRelocation = gameMain.CutSourceRect.HasValue;
            Rectangle? cutSrc = gameMain.CutSourceRect;

            // 0. 前置材料校验：仅常规蓝图放置且开启材料消耗时校验；剪切平移为同一建筑整栋物理搬迁，零消耗免材料！
            if (!isCutRelocation && gameMain.Wand_StructureConsumeMaterials && ModConfig.IsConsumablesItem())
            {
                if (!CheckMaterials(data, player, out List<string> missing))
                {
                    string missingText = string.Join("，", missing);
                    Main.NewText($"[魔杖] 材料不足！缺少: {missingText}", 255, 80, 80);
                    CombatText.NewText(player.getRect(), Color.Red, "材料不足！", true, false);
                    return false;
                }
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
                            if (tile.active()) WorldGen.KillTile(x, y, false, false, true);
                            if (tile.wall > 0) WorldGen.KillWall(x, y, false);
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

            // 1. 阶段 1：清理区域已有物块与墙壁（若开启覆盖模式）
            if (overwrite)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        int wx = startX + x;
                        int wy = startY + y;
                        if (!InBounds(wx, wy)) continue;

                        Tile tile = Main.tile[wx, wy];
                        if (tile != null)
                        {
                            if (tile.active()) WorldGen.KillTile(wx, wy, false, false, true);
                            if (tile.wall > 0) WorldGen.KillWall(wx, wy, false);
                        }
                    }
                }
            }

            // 2. 阶段 2：铺设背景墙
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int wx = startX + x;
                    int wy = startY + y;
                    if (!InBounds(wx, wy)) continue;

                    TileSnapshot snap = data.Tiles[x, y];
                    if (snap.HasWall)
                    {
                        int wallItem = StructureData.GetWallItemId(snap.WallType);
                        if (TryConsumeItem(player, wallItem, 1, isCutRelocation))
                        {
                            WorldGen.PlaceWall(wx, wy, snap.WallType, true);
                            Tile t = Main.tile[wx, wy];
                            if (t != null && snap.WallColor > 0) t.wallColor(snap.WallColor);
                        }
                    }
                }
            }

            // 3. 阶段 3：自底向上铺设单格实体方块与斜坡
            for (int x = 0; x < w; x++)
            {
                for (int y = h - 1; y >= 0; y--) // 从底部向上铺设，保证物理支撑
                {
                    int wx = startX + x;
                    int wy = startY + y;
                    if (!InBounds(wx, wy)) continue;

                    TileSnapshot snap = data.Tiles[x, y];
                    if (snap.HasTile)
                    {
                        TileObjectData objData = TileObjectData.GetTileData(snap.TileType, 0);
                        // 仅处理单格物块与平台
                        if (objData == null || (objData.Width <= 1 && objData.Height <= 1))
                        {
                            int style = (snap.TileType == Terraria.ID.TileID.Platforms) ? (snap.TileFrameY / 18) : 0;
                            int tileItem = StructureData.GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                            if (TryConsumeItem(player, tileItem, 1, isCutRelocation))
                            {
                                WorldGen.PlaceTile(wx, wy, snap.TileType, true, true, player.whoAmI, style);

                                Tile t = Main.tile[wx, wy];
                                if (t != null)
                                {
                                    // 恢复斜坡 / 半砖
                                    if (snap.Slope == 5)
                                    {
                                        t.halfBrick(true);
                                    }
                                    else if (snap.Slope > 0)
                                    {
                                        WorldGen.SlopeTile(wx, wy, snap.Slope);
                                    }

                                    if (snap.TileColor > 0) t.color(snap.TileColor);
                                    if (snap.InActive) t.inActive(true);
                                }
                            }
                        }
                    }
                }
            }

            // 4. 阶段 4：铺设多格家具与功能物块（锚点对齐）
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int wx = startX + x;
                    int wy = startY + y;
                    if (!InBounds(wx, wy)) continue;

                    TileSnapshot snap = data.Tiles[x, y];
                    if (snap.HasTile)
                    {
                        TileObjectData objData = TileObjectData.GetTileData(snap.TileType, 0);
                        if (objData != null && (objData.Width > 1 || objData.Height > 1))
                        {
                            // 检查是否为家具的左上角主原点
                            int subX = snap.TileFrameX % objData.CoordinateFullWidth;
                            int subY = snap.TileFrameY % objData.CoordinateFullHeight;
                            if (subX == 0 && subY == 0)
                            {
                                int tileItem = StructureData.GetTileItemId(snap.TileType, snap.TileFrameX, snap.TileFrameY);
                                if (TryConsumeItem(player, tileItem, 1, isCutRelocation))
                                {
                                    int style = snap.TileFrameY / objData.CoordinateFullHeight;
                                    WorldGen.PlaceObject(wx, wy, snap.TileType, false, style);
                                }
                            }
                        }
                    }
                }
            }

            // 5. 阶段 5：铺设四色电线、制动器与涂层
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int wx = startX + x;
                    int wy = startY + y;
                    if (!InBounds(wx, wy)) continue;

                    TileSnapshot snap = data.Tiles[x, y];
                    if (snap.RedWire)
                    {
                        if (TryConsumeItem(player, ItemID.Wire, 1, isCutRelocation))
                            WorldGen.PlaceWire(wx, wy);
                    }
                    if (snap.GreenWire)
                    {
                        if (TryConsumeItem(player, ItemID.Wire, 1, isCutRelocation))
                            WorldGen.PlaceWire3(wx, wy);
                    }
                    if (snap.BlueWire)
                    {
                        if (TryConsumeItem(player, ItemID.Wire, 1, isCutRelocation))
                            WorldGen.PlaceWire2(wx, wy);
                    }
                    if (snap.YellowWire)
                    {
                        if (TryConsumeItem(player, ItemID.Wire, 1, isCutRelocation))
                            WorldGen.PlaceWire4(wx, wy);
                    }
                    if (snap.Actuator)
                    {
                        if (TryConsumeItem(player, ItemID.Actuator, 1, isCutRelocation))
                            WorldGen.PlaceActuator(wx, wy);
                    }

                    Tile t = Main.tile[wx, wy];
                    if (t != null)
                    {
                        if ((snap.Coating & 1) != 0) t.fullbrightBlock(true);
                        if ((snap.Coating & 2) != 0) t.invisibleBlock(true);
                        if ((snap.Coating & 4) != 0) t.fullbrightWall(true);
                        if ((snap.Coating & 8) != 0) t.invisibleWall(true);
                    }
                }
            }

            // 6. 阶段 6：图格与墙壁连接相融自适应（Framing）与网络同步
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int wx = startX + x;
                    int wy = startY + y;
                    if (!InBounds(wx, wy)) continue;

                    WorldGen.SquareTileFrame(wx, wy, true);
                    WorldGen.SquareWallFrame(wx, wy, true);
                }
            }

            if (Main.netMode == 1)
            {
                NetMessage.SendTileSquare(player.whoAmI, minX, minY, maxX - minX + 1, maxY - minY + 1);
            }

            // 7. 阶段 7：标牌文本恢复
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

        /// <summary>
        /// 校验玩家背包物料是否足以摆放该蓝图
        /// </summary>
        public static bool CheckMaterials(StructureData data, Player player, out List<string> missingItems)
        {
            missingItems = new List<string>();
            if (data == null || player == null) return true;

            Dictionary<int, int> req = data.GetRequiredItems();
            foreach (var kvp in req)
            {
                int itemId = kvp.Key;
                int needed = kvp.Value;
                int owned = CountItemInInventory(player, itemId);
                if (owned < needed)
                {
                    string name = Lang.GetItemNameValue(itemId);
                    missingItems.Add($"{name} (缺 {needed - owned})");
                }
            }

            return missingItems.Count == 0;
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

        /// <summary>
        /// 从玩家背包中安全扣除指定数量的物品（支持跨槽位拆分扣除；剪切平移免消耗）
        /// </summary>
        private static bool TryConsumeItem(Player player, int itemId, int count = 1, bool isCutRelocation = false)
        {
            if (isCutRelocation || !gameMain.Wand_StructureConsumeMaterials || !ModConfig.IsConsumablesItem() || player?.inventory == null || itemId <= 0 || count <= 0)
                return true;

            int remaining = count;
            for (int i = 0; i < 58 && remaining > 0; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.type == itemId && item.stack > 0)
                {
                    int take = Math.Min(item.stack, remaining);
                    item.stack -= take;
                    remaining -= take;
                    if (item.stack <= 0) item.TurnToAir();
                }
            }

            return remaining == 0;
        }
    }
}
