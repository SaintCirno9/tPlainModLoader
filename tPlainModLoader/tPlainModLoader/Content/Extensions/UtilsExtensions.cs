using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.Utilities;

namespace Terraria
{
    /// <summary>
    /// 对齐 tML <c>Utils.TML.cs</c> 的随机数与坐标转换扩展。
    /// 放在 <c>namespace Terraria</c>，使 <c>using Terraria;</c> 下 <c>Main.rand.NextBool()</c> 即可解析。
    /// 作者: SaintCirno9
    /// </summary>
    public static class TPMLUtilsExtensions
    {
        /// <summary>对齐 tML：等概率返回 true/false。</summary>
        public static bool NextBool(this UnifiedRandom r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            return r.NextDouble() < 0.5;
        }

        /// <summary>对齐 tML：1/<paramref name="consequent"/> 概率返回 true。</summary>
        public static bool NextBool(this UnifiedRandom r, int consequent)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (consequent < 1)
                throw new ArgumentOutOfRangeException(nameof(consequent), "consequent 必须 >= 1");
            return r.Next(consequent) == 0;
        }

        /// <summary>对齐 tML：<paramref name="antecedent"/>/<paramref name="consequent"/> 概率返回 true。</summary>
        public static bool NextBool(this UnifiedRandom r, int antecedent, int consequent)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (antecedent > consequent)
                throw new ArgumentOutOfRangeException(nameof(antecedent), "antecedent 必须 <= consequent");
            return r.Next(consequent) < antecedent;
        }

        /// <summary>对齐 tML：返回 [0, maxValue) 的 float。</summary>
        public static float NextFloat(this UnifiedRandom r, float maxValue)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            return (float)r.NextDouble() * maxValue;
        }

        /// <summary>对齐 tML：返回 [minValue, maxValue) 的 float。</summary>
        public static float NextFloat(this UnifiedRandom r, float minValue, float maxValue)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            return (float)r.NextDouble() * (maxValue - minValue) + minValue;
        }

        /// <summary>对齐 tML：从数组中随机取一个元素。</summary>
        public static T Next<T>(this UnifiedRandom r, T[] array)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (array == null || array.Length == 0)
                throw new ArgumentException("array 不能为空", nameof(array));
            return array[r.Next(array.Length)];
        }

        /// <summary>对齐 tML：从列表中随机取一个元素。</summary>
        public static T Next<T>(this UnifiedRandom r, IList<T> list)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            if (list == null || list.Count == 0)
                throw new ArgumentException("list 不能为空", nameof(list));
            return list[r.Next(list.Count)];
        }

        /// <summary>对齐 tML：世界坐标 = 物块坐标 * 16 + 偏移。</summary>
        public static Vector2 ToWorldCoordinates(this Vector2 v, float autoAddX = 8f, float autoAddY = 8f)
            => v * 16f + new Vector2(autoAddX, autoAddY);

        /// <summary>对齐 tML：世界坐标 = 物块坐标 * 16 + 偏移。</summary>
        public static Vector2 ToWorldCoordinates(this Vector2 v, Vector2 autoAddXY)
            => v * 16f + autoAddXY;

        /// <summary>对齐 tML：Point16 → Point。</summary>
        public static Point ToPoint(this Point16 p) => new Point(p.X, p.Y);

        /// <summary>对齐 tML：向 0 截断为 Point16。世界坐标转物块请用 <c>ToTileCoordinates16</c>。</summary>
        public static Point16 ToPoint16(this Vector2 v) => new Point16((short)v.X, (short)v.Y);

        /// <summary>对齐 tML：支持 <c>var (x, y) = point;</c>。</summary>
        public static void Deconstruct(this Point point, out int x, out int y)
        {
            x = point.X;
            y = point.Y;
        }
    }
}
