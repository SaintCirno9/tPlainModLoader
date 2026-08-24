using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace Instavator.Content.Logic
{
    /// <summary>
    /// 地狱直通车世界采掘与垂直通道快速建造引擎。
    /// 建造任务按帧分批执行，且目标坐标在启动时固定，避免卡顿和鼠标移动导致重复建造。
    /// </summary>
    public static class InstavatorShaftBuilder
    {
        private const int MaxCellsPerUpdate = 128;
        private static readonly InstavatorUseGate UseGate = new InstavatorUseGate();
        private static BuildJob _activeJob;

        public static bool IsBuildRunning => _activeJob != null;
        public static bool IsInputLocked => UseGate.IsLocked;
        public static int PendingCellCount => _activeJob == null ? 0 : _activeJob.Plan.CellCount - _activeJob.NextCell;

        public static bool CanUse(Player player)
        {
            if (player == null || player.whoAmI != Main.myPlayer)
            {
                return true;
            }

            return _activeJob == null && !UseGate.IsLocked;
        }

        public static bool OkayToDestroyTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 0)) return false;
            Tile tile = Main.tile[x, y];
            if (tile == null) return false;

            // 保护不可破坏方块（如未击败骷髅王的地牢砖、神庙砖、不可破坏祭坛等）
            if (tile.active())
            {
                int type = tile.type;
                if (type == TileID.LihzahrdBrick || type == TileID.LihzahrdAltar) return false;
                if (type == TileID.BlueDungeonBrick || type == TileID.GreenDungeonBrick || type == TileID.PinkDungeonBrick)
                {
                    if (!NPC.downedBoss3) return false;
                }
            }

            return true;
        }

        public static void ClearTileAndWall(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 0)) return;
            Tile tile = Main.tile[x, y];
            if (tile == null) return;

            // 只有确实存在对应内容时才调用 WorldGen，空格是最常见路径。
            if (tile.active())
            {
                WorldGen.KillTile(x, y, false, false, true);
                // 多格箱体在第一次调用后可能仍保留锚点，再补一次才与原逻辑等价。
                if (tile.active()) WorldGen.KillTile(x, y, false, false, true);
            }

            if (tile.wall > 0)
            {
                WorldGen.KillWall(x, y, false);
            }

            tile.ClearEverything();
        }

        public static bool TryStartFullInstavator(Player player, Vector2 mouseWorld)
        {
            return TryStartBuild(player, mouseWorld, InstavatorVariant.Full, Main.maxTilesY - 40, -3, 3);
        }

        public static bool TryStartHalfInstavator(Player player, Vector2 mouseWorld)
        {
            int targetY = (int)(Main.rockLayer + ((Main.maxTilesY - 200) - Main.rockLayer) / 2.0);
            return TryStartBuild(player, mouseWorld, InstavatorVariant.Half, targetY, -2, 2);
        }

        public static bool TryStartDoubleObsidianInstavator(Player player, Vector2 mouseWorld)
        {
            return TryStartBuild(player, mouseWorld, InstavatorVariant.DoubleObsidian, Main.maxTilesY - 40, -5, 5);
        }

        // 保留原有入口，避免其他外部调用者改变行为；实际工作由分帧任务执行。
        public static void BuildFullInstavator(Player player, Vector2 mouseWorld)
        {
            TryStartFullInstavator(player, mouseWorld);
        }

        public static void BuildHalfInstavator(Player player, Vector2 mouseWorld)
        {
            TryStartHalfInstavator(player, mouseWorld);
        }

        public static void BuildDoubleObsidianInstavator(Player player, Vector2 mouseWorld)
        {
            TryStartDoubleObsidianInstavator(player, mouseWorld);
        }

        public static void Update()
        {
            if (Main.gameMenu)
            {
                _activeJob = null;
                UseGate.Reset();
                return;
            }

            Player player = Main.LocalPlayer;
            UseGate.Update(player == null || !player.controlUseItem);

            if (_activeJob == null)
            {
                return;
            }

            try
            {
                int processed = 0;
                while (_activeJob != null && processed++ < MaxCellsPerUpdate)
                {
                    if (_activeJob.NextCell >= _activeJob.Plan.CellCount)
                    {
                        _activeJob = null;
                        break;
                    }

                    InstavatorBuildCell cell = _activeJob.Plan.GetCell(_activeJob.NextCell++);
                    ProcessCell(_activeJob, cell);
                }

                if (_activeJob != null && _activeJob.NextCell >= _activeJob.Plan.CellCount)
                {
                    _activeJob = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Instavator] 分帧建造异常，已停止当前任务: {ex}");
                _activeJob = null;
            }
        }

        private static bool TryStartBuild(Player player, Vector2 mouseWorld, InstavatorVariant variant, int endY, int minOffset, int maxOffset)
        {
            if (player == null || player.whoAmI != Main.myPlayer || _activeJob != null || !UseGate.TryAcquire())
            {
                return false;
            }

            int startX = (int)(mouseWorld.X / 16f);
            int startY = (int)(mouseWorld.Y / 16f);
            var plan = new InstavatorBuildPlan(startX, startY, endY, minOffset, maxOffset);
            if (plan.CellCount == 0)
            {
                UseGate.Reset();
                return false;
            }

            _activeJob = new BuildJob(plan, variant, player.whoAmI);
            SoundEngine.PlaySound(SoundID.Item14, mouseWorld);
            return true;
        }

        private static void ProcessCell(BuildJob job, InstavatorBuildCell cell)
        {
            int x = cell.X;
            int y = cell.Y;
            if (x < 10 || x >= Main.maxTilesX - 10 || y < 10 || y >= Main.maxTilesY - 10) return;
            if (!OkayToDestroyTile(x, y)) return;

            Tile tile = Main.tile[x, y];
            int desiredTile = GetDesiredTile(job.Variant, cell.Offset, y);
            bool alreadyHasDesiredTile = desiredTile > 0 && tile.active() && tile.type == desiredTile;

            // 已经是目标方块时不清除，重复运行只补缺失的墙或设施，避免绳索被反复重建。
            if (!alreadyHasDesiredTile && (tile.active() || tile.wall > 0 || tile.liquid > 0))
            {
                ClearTileAndWall(x, y);
                tile = Main.tile[x, y];
            }

            bool changed = false;
            if (tile.wall != WallID.Stone)
            {
                WorldGen.PlaceWall(x, y, WallID.Stone, false);
                changed = tile.wall == WallID.Stone;
            }

            if (desiredTile > 0 && (!tile.active() || tile.type != desiredTile))
            {
                WorldGen.PlaceTile(x, y, desiredTile, false, false, job.PlayerWhoAmI, 0);
                changed = changed || (tile.active() && tile.type == desiredTile);
            }

            if (changed && Main.netMode == 2)
            {
                NetMessage.SendTileSquare(-1, x, y, 1, 1, TileChangeType.None);
            }
        }

        private static int GetDesiredTile(InstavatorVariant variant, int offset, int y)
        {
            if (variant == InstavatorVariant.Full)
            {
                if (offset == -3 || offset == 3) return TileID.ObsidianBrick;
                if ((offset == -2 || offset == 2) && y % 10 == 0) return TileID.Torches;
                if (offset == 0) return TileID.Rope;
            }
            else if (variant == InstavatorVariant.Half)
            {
                if (offset == 0) return TileID.Rope;
            }
            else
            {
                if (offset == -5 || offset == 5 || offset == 0) return TileID.ObsidianBrick;
                if ((offset == -4 || offset == 4 || offset == -1 || offset == 1) && y % 10 == 0) return TileID.Torches;
                if (offset == -2 || offset == 2) return TileID.Rope;
            }

            return 0;
        }

        private sealed class BuildJob
        {
            public BuildJob(InstavatorBuildPlan plan, InstavatorVariant variant, int playerWhoAmI)
            {
                Plan = plan;
                Variant = variant;
                PlayerWhoAmI = playerWhoAmI;
            }

            public InstavatorBuildPlan Plan { get; }
            public InstavatorVariant Variant { get; }
            public int PlayerWhoAmI { get; }
            public int NextCell { get; set; }
        }
    }

    public enum InstavatorVariant
    {
        Full,
        Half,
        DoubleObsidian
    }
}
