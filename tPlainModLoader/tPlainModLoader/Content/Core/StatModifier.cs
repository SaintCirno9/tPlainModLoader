using System;

namespace TPML.Content
{
    /// <summary>
    /// 数值修饰结构（对齐 tModLoader StatModifier）
    /// 支持基础加成、乘算加成与固定增量运算
    /// 作者: SaintCirno9
    /// </summary>
    public struct StatModifier : IEquatable<StatModifier>
    {
        public static readonly StatModifier Default = new StatModifier(1f, 1f, 0f, 0f);

        public float Additive;
        public float Multiplicative;
        public float Flat;
        public float Base;

        public StatModifier(float additive = 1f, float multiplicative = 1f, float flat = 0f, float @base = 0f)
        {
            Additive = additive;
            Multiplicative = multiplicative;
            Flat = flat;
            Base = @base;
        }

        public float ApplyTo(float value)
        {
            return (value + Base) * Additive * Multiplicative + Flat;
        }

        public static StatModifier operator +(StatModifier m, float add)
            => new StatModifier(m.Additive + add, m.Multiplicative, m.Flat, m.Base);

        public static StatModifier operator -(StatModifier m, float sub)
            => new StatModifier(m.Additive - sub, m.Multiplicative, m.Flat, m.Base);

        public static StatModifier operator *(StatModifier m, float mul)
            => new StatModifier(m.Additive, m.Multiplicative * mul, m.Flat, m.Base);

        public static StatModifier operator /(StatModifier m, float div)
            => new StatModifier(m.Additive, m.Multiplicative / div, m.Flat, m.Base);

        public static StatModifier operator +(float add, StatModifier m) => m + add;
        public static StatModifier operator *(float mul, StatModifier m) => m * mul;

        public static StatModifier operator +(StatModifier m1, StatModifier m2)
            => new StatModifier(m1.Additive + m2.Additive - 1f, m1.Multiplicative * m2.Multiplicative, m1.Flat + m2.Flat, m1.Base + m2.Base);

        public static StatModifier Combine(StatModifier m1, StatModifier m2) => m1 + m2;

        public static StatModifier Scale(StatModifier m, float scale)
            => new StatModifier(1f + (m.Additive - 1f) * scale, 1f + (m.Multiplicative - 1f) * scale, m.Flat * scale, m.Base * scale);

        public bool Equals(StatModifier other)
            => Additive == other.Additive && Multiplicative == other.Multiplicative && Flat == other.Flat && Base == other.Base;

        public override bool Equals(object obj)
            => obj is StatModifier other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Additive.GetHashCode();
                hash = (hash * 397) ^ Multiplicative.GetHashCode();
                hash = (hash * 397) ^ Flat.GetHashCode();
                hash = (hash * 397) ^ Base.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(StatModifier left, StatModifier right) => left.Equals(right);
        public static bool operator !=(StatModifier left, StatModifier right) => !left.Equals(right);
    }
}
