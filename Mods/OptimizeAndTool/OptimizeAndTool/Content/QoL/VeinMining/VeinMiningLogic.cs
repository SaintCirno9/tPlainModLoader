using CommandHelp;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using TPML.Core.Diagnostics;

namespace OptimizeAndTool.Content.QoL.VeinMining
{
    /// <summary>
    /// 连锁挖矿 BFS 广度优先搜索核心算法与方块批量破坏逻辑
    /// 作者: SaintCirno9
    /// </summary>
    public static class VeinMiningLogic
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> MaxTiles = new GetSetReset<int>(128, 128);
        public static GetSetReset<bool> IncludeOres = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> IncludeGems = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> IncludeTrash = new GetSetReset<bool>(false, false);

        public static bool IsExecuting { get; private set; } = false;

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("veinMining", Enable),
                CommandBuild.get1("veinMiningMaxTiles", Enable, MaxTiles, new CommandInt()),
                CommandBuild.get2("veinMiningIncludeOres", IncludeOres),
                CommandBuild.get2("veinMiningIncludeGems", IncludeGems),
                CommandBuild.get2("veinMiningIncludeTrash", IncludeTrash)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get1(Enable, MaxTiles, int.Parse, "破坏矿石/宝石时自动连带破坏相连同类方块（输入框调节单次最大连锁数量）", "Images/Item_3509", "简单连锁挖矿"),
                UIBuild.get2(IncludeOres, "包含肉前、肉后所有品类矿石与沙漠化石", "Images/Item_11", "包含全品类矿石"),
                UIBuild.get2(IncludeGems, "包含紫晶、黄玉、蓝玉、翡翠、红玉、钻石与嵌石宝石", "Images/Item_180", "包含地下天然宝石"),
                UIBuild.get2(IncludeTrash, "开启后破坏泥土/石头也将连锁扩散（谨慎使用）", "Images/Item_2", "包含泥土与石头等杂块")
            };
        }

        public static void StartMining(Player player, int startX, int startY, ushort targetType, short targetFrameX, int pickPower)
        {
            if (IsExecuting || !Enable.val) return;

            IsExecuting = true;
            try
            {
                using (PerformanceProfiler.Measure("OptimizeAndTool", "VeinMining.StartMining"))
                {
                    Queue<Point> queue = new Queue<Point>();
                    HashSet<long> visited = new HashSet<long>();

                visited.Add(((long)startX << 32) | (uint)startY);
                EnqueueNeighbors(startX, startY, queue, visited);

                int maxLimit = Math.Max(1, Math.Min(1000, MaxTiles.val));
                // 多人模式客户端每块需 SendData 同步，限制单次连锁量避免网络洪泛（原版 PickTile 校验稿力同理）
                if (Main.netMode == 1)
                {
                    maxLimit = Math.Min(maxLimit, 200);
                }
                int minedCount = 0;

                while (queue.Count > 0 && minedCount < maxLimit)
                {
                    Point current = queue.Dequeue();
                    int cx = current.X;
                    int cy = current.Y;

                    if (!WorldGen.InWorld(cx, cy, 1)) continue;

                    Tile curTile = Main.tile[cx, cy];
                    if (curTile == null || !curTile.active()) continue;

                    if (!IsMatchingTile(targetType, curTile.type, targetFrameX, curTile.frameX)) continue;

                    // 稿力校验：复用原版 PickTile_DetermineDamage（GetPickaxeDamage 系列判定），
                    // 伤害 <= 0 表示当前镐挖不动（如铜镐挖叶绿/精金/神庙砖），跳过不连锁破坏
                    player.PickTile_DetermineDamage(cx, cy, pickPower, curTile, respectTransformingTiles: false, out int bufferIndex, out int damage);
                    if (damage <= 0) continue;

                    WorldGen.KillTile(cx, cy, fail: false, effectOnly: false, noItem: false);
                    minedCount++;

                    if (Main.netMode == 1)
                    {
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, cx, cy, 0, 0, 0, 0);
                    }

                    if (!curTile.active() || curTile.type != targetType)
                    {
                        EnqueueNeighbors(cx, cy, queue, visited);
                    }
                }
                }
            }
            finally
            {
                IsExecuting = false;
            }
        }

        private static void EnqueueNeighbors(int x, int y, Queue<Point> queue, HashSet<long> visited)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx < 0 || nx >= Main.maxTilesX || ny < 0 || ny >= Main.maxTilesY) continue;

                    long key = ((long)nx << 32) | (uint)ny;
                    if (visited.Add(key))
                    {
                        queue.Enqueue(new Point(nx, ny));
                    }
                }
            }
        }

        private static bool IsMatchingTile(ushort targetType, ushort neighborType, short targetFrameX, short neighborFrameX)
        {
            if (neighborType != targetType) return false;

            if (targetType == TileID.ExposedGems)
            {
                if ((targetFrameX / 18) != (neighborFrameX / 18))
                {
                    return false;
                }
            }

            return VeinMiningSets.ShouldMine(neighborType, IncludeOres.val, IncludeGems.val, IncludeTrash.val);
        }
    }
}
