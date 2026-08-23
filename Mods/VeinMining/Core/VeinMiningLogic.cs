using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using VeinMining.Config;

namespace VeinMining.Core
{
    /// <summary>
    /// 连锁挖矿 BFS 广度优先搜索核心算法与方块批量破坏逻辑
    /// </summary>
    public static class VeinMiningLogic
    {
        /// <summary>
        /// 递归/重入保护标志，避免 WorldGen.KillTile 级联触发连锁
        /// </summary>
        public static bool IsExecuting { get; private set; } = false;

        /// <summary>
        /// 启动 BFS 连锁挖掘逻辑
        /// </summary>
        /// <param name="player">触发挖掘的玩家</param>
        /// <param name="startX">初始被破坏方块 X 坐标</param>
        /// <param name="startY">初始被破坏方块 Y 坐标</param>
        /// <param name="targetType">被挖掘的目标物块类型 ID</param>
        /// <param name="targetFrameX">目标物块的 frameX (用于区分嵌石宝石种类)</param>
        /// <param name="pickPower">玩家当前镐力</param>
        public static void StartMining(Player player, int startX, int startY, ushort targetType, short targetFrameX, int pickPower)
        {
            if (IsExecuting) return;
            if (!VeinMiningConfig.Enable) return;

            IsExecuting = true;
            try
            {
                Queue<Point> queue = new Queue<Point>();
                HashSet<long> visited = new HashSet<long>();

                // 标记初始位置已访问
                visited.Add(((long)startX << 32) | (uint)startY);

                // 将初始破坏点周边的 8 个相邻格加入 BFS 搜索队列
                EnqueueNeighbors(startX, startY, queue, visited);

                int maxLimit = Math.Max(1, Math.Min(1000, VeinMiningConfig.MaxTiles));
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

                    // 破坏当前相连同类方块并正常产生掉落物与音效粒子
                    WorldGen.KillTile(cx, cy, fail: false, effectOnly: false, noItem: false);
                    minedCount++;

                    // 多人联机环境下向服务端同步方块破坏
                    if (Main.netMode == 1)
                    {
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, cx, cy, 0, 0, 0, 0);
                    }

                    // 若方块已被成功摧毁，将其周边的 8 个相邻格入队继续扩散
                    if (!curTile.active() || curTile.type != targetType)
                    {
                        EnqueueNeighbors(cx, cy, queue, visited);
                    }
                }
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 将指定坐标周边的 8 方向邻居加入待搜索队列
        /// </summary>
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

        /// <summary>
        /// 判定相邻图格是否与源挖掘目标匹配
        /// </summary>
        private static bool IsMatchingTile(ushort targetType, ushort neighborType, short targetFrameX, short neighborFrameX)
        {
            if (neighborType != targetType) return false;

            // 嵌石宝石 (TileID.ExposedGems 178) 需按 frameX / 18 区分宝石品种
            if (targetType == TileID.ExposedGems)
            {
                if ((targetFrameX / 18) != (neighborFrameX / 18))
                {
                    return false;
                }
            }

            return VeinMiningSets.IsMinable(neighborType);
        }
    }
}
