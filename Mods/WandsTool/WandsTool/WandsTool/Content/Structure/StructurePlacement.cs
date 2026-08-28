using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 分帧协程放置引擎（八阶段流水线、前置原子扣料、分帧限速、进度提示、防家具崩塌、原子性剪切平移与网络同步）
    /// 材料校验与扣除在 BeginPlace 中一次性同步完成，落格主体跨帧分批执行，避免大蓝图单帧卡顿。
    /// </summary>
    public static class StructurePlacement
    {
        /// <summary>
        /// 一次蓝图放置/剪切平移作业（协程状态与进度跟踪）
        /// </summary>
        private class PlacementJob
        {
            public StructureData Data;
            public Player Player;
            public bool Overwrite;
            public bool IsCutRelocation;
            public Action OnSuccess;
            public int StartX, StartY;
            public int MinX, MinY, MaxX, MaxY;
            public IEnumerator Routine;

            // 进度跟踪：已完成阶段数 + 当前阶段内比例
            public int CompletedPhases = 0;
            public int CurrentPhase = -1;
            public string PhaseName = "";
            public int PhaseOpsDone = 0;
            public int PhaseOpsTotal = 1;
            public int PhaseBudget = 1;

            public void BeginPhase(int index, string name, int totalOps, int budget)
            {
                CurrentPhase = index;
                PhaseName = name;
                PhaseOpsDone = 0;
                PhaseOpsTotal = Math.Max(1, totalOps);
                PhaseBudget = Math.Max(1, budget);
            }

            public bool ShouldYield()
            {
                PhaseOpsDone++;
                return PhaseOpsDone % PhaseBudget == 0;
            }

            public void EndPhase()
            {
                PhaseOpsDone = PhaseOpsTotal;
                CompletedPhases++;
                CurrentPhase = -1;
            }

            public float GetProgress()
            {
                float frac = 0f;
                if (CurrentPhase >= 0)
                {
                    frac = Math.Min(1f, PhaseOpsDone / (float)PhaseOpsTotal);
                }
                return Math.Min(1f, (CompletedPhases + frac) / 8f);
            }
        }

        private static PlacementJob _job = null;

        /// <summary>是否有放置作业正在进行</summary>
        public static bool IsPlacing => _job != null;

        /// <summary>放置完成后的"已放置"驻留提示倒计时（tick）</summary>
        public static int FinishTipCountdown = 0;

        /// <summary>当前放置进度 0~1（无作业时为 0）</summary>
        public static float Progress => _job?.GetProgress() ?? 0f;

        /// <summary>当前阶段名称（无作业时为空）</summary>
        public static string PhaseName => _job?.PhaseName ?? "";

        // 各阶段单帧操作预算（格数），大蓝图按此速率分帧落地
        //（200×200 约 3~4 秒、500×500 约 20~25 秒 @60fps；放置期间原版 UI 点击不受影响，见 WALKTHROUGH 5.4.4）
        private const int BudgetCleanup = 400;
        private const int BudgetWalls = 5000;
        private const int BudgetSupports = 2500;
        private const int BudgetFraming = 800;
        private const int BudgetFurniture = 2000;
        private const int BudgetWires = 10000;
        private const int BudgetSigns = 1500;

        /// <summary>
        /// 发起蓝图放置或剪切平移搬家（材料校验与扣除同步原子完成，落格主体转分帧协程）
        /// 返回 false 表示校验失败或已有作业进行中，不修改世界。
        /// </summary>
        public static bool BeginPlace(StructureData data, Point originWorldTile, Player player, bool overwrite = true, Action onSuccess = null)
        {
            if (data == null || data.Tiles == null || player == null) return false;
            if (IsPlacing) return false;

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

                // 0.1 若为剪切平移，在发起落下的瞬间，原子性清除原区域建筑（无地面掉落碎屑，保障世界平移干净利落）
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

                    gameMain.CutSourceRect = null; // 搬家任务已确认发起，重置标记
                }

                // 1. 构建分帧作业（八阶段流水线协程）
                PlacementJob job = new PlacementJob
                {
                    Data = data,
                    Player = player,
                    Overwrite = overwrite,
                    IsCutRelocation = isCutRelocation,
                    OnSuccess = onSuccess,
                    StartX = originWorldTile.X - data.OriginX,
                    StartY = originWorldTile.Y - data.OriginY,
                    MinX = Math.Max(0, originWorldTile.X - data.OriginX),
                    MinY = Math.Max(0, originWorldTile.Y - data.OriginY),
                    MaxX = Math.Min(Main.tile.GetLength(0) - 1, originWorldTile.X - data.OriginX + data.Width - 1),
                    MaxY = Math.Min(Main.tile.GetLength(1) - 1, originWorldTile.Y - data.OriginY + data.Height - 1),
                };
                job.Routine = PlaceRoutine(job);
                _job = job;
                return true;
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 放置结构时发生异常: {ex.Message}", 255, 80, 80);
                return false;
            }
        }

        /// <summary>
        /// 每帧驱动放置协程（由 feces 主循环调用，仅在魔杖启用时执行）
        /// </summary>
        public static void Update()
        {
            if (FinishTipCountdown > 0) FinishTipCountdown--;
            if (_job == null) return;

            // 模式卫语句：作业期间仅允许粘贴模式，面板等其他路径切换模式即中止（防止协程与世界写入交错）
            if (gameMain.Wand_StructureMode != gameMain.StructureMode.Paste)
            {
                Player modeExiter = Main.LocalPlayer;
                Abort();
                if (modeExiter != null)
                {
                    CombatText.NewText(modeExiter.getRect(), Color.Orange, "放置已中止", true, false);
                }
                return;
            }

            Player player = _job.Player;

            // 安全卫语句：玩家死亡或回到主菜单时中止作业（已落格部分保留，材料不回滚）
            if (player == null || player.dead || Main.gameMenu)
            {
                bool wasDead = player != null && player.dead;
                Abort();
                if (wasDead)
                {
                    CombatText.NewText(player.getRect(), Color.Orange, "放置已中止", true, false);
                }
                return;
            }

            try
            {
                if (!_job.Routine.MoveNext())
                {
                    _job = null;
                    FinishTipCountdown = 90;
                }
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 放置结构时发生异常: {ex.Message}", 255, 80, 80);
                _job = null;
            }
        }

        /// <summary>
        /// 安全中止当前作业（魔杖关闭/回主菜单/模式切换时调用）。
        /// 联机下若作业已开始落格，补发一次区域图格同步，避免本地与服务器状态不一致。
        /// </summary>
        public static void Abort()
        {
            if (_job == null) return;

            if (Main.netMode == 1 && (_job.CompletedPhases > 0 || _job.CurrentPhase >= 0))
            {
                int who = _job.Player?.whoAmI ?? Main.myPlayer;
                NetMessage.SendTileSquare(who, _job.MinX, _job.MinY, _job.MaxX - _job.MinX + 1, _job.MaxY - _job.MinY + 1);
            }

            _job = null;
        }

        /// <summary>
        /// 在玩家头顶绘制放置进度提示（Game 层调用）：
        /// 放置中显示黄色"放置中.. 百分比 (阶段)"，完成后绿色"已放置"驻留一段时间
        /// </summary>
        public static void DrawProgress()
        {
            try
            {
                if (Main.gameMenu) return;
                Player player = Main.LocalPlayer;
                if (player == null) return;

                if (_job != null)
                {
                    string dots = "";
                    int dotCount = (int)(Main.GameUpdateCount % 60) / 15;
                    for (int i = 0; i < dotCount; i++) dots += ".";

                    int pct = (int)(Progress * 100f);
                    string text = $"放置中{dots} {pct}% ({PhaseName})";
                    Vector2 pos = player.Bottom - Main.screenPosition + new Vector2(0, 16f);
                    Terraria.Utils.DrawBorderString(Main.spriteBatch, text, pos, Color.Yellow, 0.85f, 0.5f, 0.5f);
                }
                else if (FinishTipCountdown > 0)
                {
                    Vector2 pos = player.Bottom - Main.screenPosition + new Vector2(0, 16f);
                    Terraria.Utils.DrawBorderString(Main.spriteBatch, "已放置", pos, Color.LightGreen, 0.85f, 0.5f, 0.5f);
                }
            }
            catch
            {
                // 静默容错，避免异常冒泡到 XNA 绘制线程
            }
        }

        /// <summary>
        /// 八阶段放置流水线协程：清理 -> 背景墙 -> 支撑方块 -> Framing -> 家具 -> 电线涂层 -> 网络同步 -> 标牌恢复
        /// </summary>
        private static IEnumerator PlaceRoutine(PlacementJob job)
        {
            StructureData data = job.Data;
            Player player = job.Player;
            bool overwrite = job.Overwrite;
            bool isCutRelocation = job.IsCutRelocation;
            int startX = job.StartX;
            int startY = job.StartY;
            int w = data.Width;
            int h = data.Height;

            // 阶段 1：清理区域已有不一致的物块与墙壁（覆盖模式：被拆除的旧物块/墙壁/家具自动回收返还背包！）
            job.BeginPhase(0, "清理旧区域", w * h, BudgetCleanup);
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

                        if (job.ShouldYield()) yield return null;
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
            job.EndPhase();

            // 阶段 2：铺设背景墙（差量铺设：仅在墙壁不一致时写入）
            job.BeginPhase(1, "铺设背景墙", w * h, BudgetWalls);
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

                    if (job.ShouldYield()) yield return null;
                }
            }
            job.EndPhase();

            // 阶段 3：自底向上铺设实体支撑方块与平台（先打好地板与承重基础）
            job.BeginPhase(2, "铺设支撑方块", w * h, BudgetSupports);
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

                    if (job.ShouldYield()) yield return null;
                }
            }
            job.EndPhase();

            // 阶段 4：仅对实体方块与墙壁执行 Framing 连接相融（家具绝对不执行 Framing，彻底杜绝崩塌）
            job.BeginPhase(3, "连接 Framing", w * h, BudgetFraming);
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

                    if (job.ShouldYield()) yield return null;
                }
            }
            job.EndPhase();

            // 阶段 5：铺设所有多格家具与装饰物（椅子、桌子、床、箱子、门、火把、吊灯等，稳稳落位在已建好的地板上）
            job.BeginPhase(4, "摆放家具", w * h, BudgetFurniture);
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

                    if (job.ShouldYield()) yield return null;
                }
            }
            job.EndPhase();

            // 阶段 6：铺设四色电线、制动器与涂层
            job.BeginPhase(5, "电线与涂层", w * h, BudgetWires);
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

                    if (job.ShouldYield()) yield return null;
                }
            }
            job.EndPhase();

            // 阶段 7：网络同步（完成时一次性广播）
            job.BeginPhase(6, "网络同步", 1, 1);
            if (Main.netMode == 1)
            {
                NetMessage.SendTileSquare(player.whoAmI, job.MinX, job.MinY, job.MaxX - job.MinX + 1, job.MaxY - job.MinY + 1);
            }
            job.EndPhase();

            // 阶段 8：标牌文本恢复
            job.BeginPhase(7, "恢复标牌", w * h, BudgetSigns);
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

                        if (job.ShouldYield()) yield return null;
                    }
                }
            }
            job.EndPhase();

            // 收尾：音效、剪切模式还原与完成反馈
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37, player.position);
            if (isCutRelocation)
            {
                StructureStorage.Clipboard = null;
                gameMain.Wand_StructureMode = gameMain.StructureMode.Cut;
            }
            string finishMsg = isCutRelocation ? "平移成功 (回到剪切模式)" : $"已放置 {data.Name}";
            CombatText.NewText(player.getRect(), isCutRelocation ? Color.Gold : Color.LimeGreen, finishMsg, true, false);

            job.OnSuccess?.Invoke();
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
        /// 统计玩家主背包 (0~57 号槽位) 中特定物品的总数量（不含外部融合容器，与 GetPlayerInventorySnapshot 口径不同）
        /// </summary>
        public static int CountItemInInventory(Player player, int itemId)
        {
            if (player?.inventory == null || itemId <= 0) return 0;
            return player.CountItem(itemId);
        }

        private static bool InBounds(int x, int y)
        {
            return x >= 0 && x < Main.tile.GetLength(0) && y >= 0 && y < Main.tile.GetLength(1);
        }
    }
}
