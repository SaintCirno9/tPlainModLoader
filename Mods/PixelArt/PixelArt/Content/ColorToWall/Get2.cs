using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelArt.Content.ColorToWall
{
    internal class Get2 : IGetColorWall
    {
        private List<ColorWall> palette;
        private int[] pr, pg, pb; // precomputed channels
        private List<int>[] buckets;
        private int gridSize;
        private int gridCount;
        private int cellSize; // 256 / gridSize (floor)
        private int maxRadius;

        // 构造或 Init 可指定网格分辨率（默认 8）
        // maxRadius 控制搜索附近桶的半径，越大越精确越慢。对大多数情况 1 或 2 足够。
        public Get2(int gridSize = 8, int maxRadius = 2)
        {
            if (gridSize <= 0) throw new ArgumentException(nameof(gridSize));
            this.gridSize = gridSize;
            this.gridCount = gridSize * gridSize * gridSize;
            this.cellSize = 256 / gridSize;
            this.maxRadius = maxRadius;
        }

        // 初始化，传入大约 2000 长度的颜色数组
        public void Init(List<ColorWall> colors)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));

            palette = colors.ToList();
            int n = palette.Count;
            pr = new int[n];
            pg = new int[n];
            pb = new int[n];

            // 初始化桶
            buckets = new List<int>[gridCount];
            for (int i = 0; i < gridCount; i++) buckets[i] = null;

            for (int i = 0; i < n; i++)
            {
                Color c = palette[i].color;
                pr[i] = c.R;
                pg[i] = c.G;
                pb[i] = c.B;

                int bi = BucketIndex(c.R, c.G, c.B);
                if (buckets[bi] == null) buckets[bi] = new List<int>(4);
                buckets[bi].Add(i);
            }
        }

        // 快速获取最接近的颜色（近似且非常快）
        public ColorWall Get(Color target)
        {
            if (palette == null) throw new InvalidOperationException("Palette not initialized.");

            int tr = target.R, tg = target.G, tb = target.B;
            int br = tr * gridSize / 256;
            int bg = tg * gridSize / 256;
            int bb = tb * gridSize / 256;
            if (br == gridSize) br = gridSize - 1;
            if (bg == gridSize) bg = gridSize - 1;
            if (bb == gridSize) bb = gridSize - 1;

            int bestIdx = -1;
            int bestDist2 = int.MaxValue;

            // Search from radius 0..maxRadius
            for (int r = 0; r <= maxRadius; r++)
            {
                bool foundAnyInThisRadius = false;
                // loop through cube [-r, r] for each axis
                for (int dx = -r; dx <= r; dx++)
                {
                    int nx = br + dx;
                    if (nx < 0 || nx >= gridSize) continue;
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int ny = bg + dy;
                        if (ny < 0 || ny >= gridSize) continue;
                        for (int dz = -r; dz <= r; dz++)
                        {
                            int nz = bb + dz;
                            if (nz < 0 || nz >= gridSize) continue;

                            // Optionally skip inner shells if we only want shell r,
                            // but for simplicity we check all cubes up to r.
                            int bi = (nx * gridSize + ny) * gridSize + nz;
                            var list = buckets[bi];
                            if (list == null) continue;
                            foundAnyInThisRadius = true;
                            // check candidates
                            foreach (int idx in list)
                            {
                                int dr = pr[idx] - tr;
                                int dg = pg[idx] - tg;
                                int db = pb[idx] - tb;
                                int d2 = dr * dr + dg * dg + db * db;
                                if (d2 < bestDist2)
                                {
                                    bestDist2 = d2;
                                    bestIdx = idx;
                                    if (bestDist2 == 0) return palette[bestIdx]; // exact match
                                }
                            }
                        }
                    }
                }
                // If we found some candidate in this radius and we are happy with it, we can break early.
                // A heuristic: if bestDist2 is less than or equal to the squared diagonal of a grid cell,
                // it's unlikely a closer color lies outside current radius.
                if (foundAnyInThisRadius)
                {
                    int diag = cellSize * cellSize * 3; // approximate cell diagonal^2
                    if (bestIdx != -1 && bestDist2 <= diag) break;
                }
            }

            // Fallback: if nothing found in limited radius (rare), do full scan (still acceptable sometimes)
            if (bestIdx == -1)
            {
                int n = palette.Count;
                for (int i = 0; i < n; i++)
                {
                    int dr = pr[i] - tr;
                    int dg = pg[i] - tg;
                    int db = pb[i] - tb;
                    int d2 = dr * dr + dg * dg + db * db;
                    if (d2 < bestDist2)
                    {
                        bestDist2 = d2;
                        bestIdx = i;
                    }
                }
            }

            return palette[bestIdx];
        }

        private int BucketIndex(int r, int g, int b)
        {
            int br = r * gridSize / 256;
            int bg = g * gridSize / 256;
            int bb = b * gridSize / 256;
            if (br == gridSize) br = gridSize - 1;
            if (bg == gridSize) bg = gridSize - 1;
            if (bb == gridSize) bb = gridSize - 1;
            return (br * gridSize + bg) * gridSize + bb;
        }
    }
}
