using System;

namespace TPML.Content
{
    /// <summary>
    /// 伤害类型基类（对齐 tModLoader DamageClass）
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class DamageClass
    {
        public static DamageClass Default { get; } = new GenericDamageClass();
        public static DamageClass Generic => Default;
        public static DamageClass Melee { get; } = new NamedDamageClass("Melee");
        public static DamageClass Ranged { get; } = new NamedDamageClass("Ranged");
        public static DamageClass Magic { get; } = new NamedDamageClass("Magic");
        public static DamageClass Summon { get; } = new NamedDamageClass("Summon");
        public static DamageClass Throwing { get; } = new NamedDamageClass("Throwing");

        public virtual string DisplayName => GetType().Name;
    }

    internal class GenericDamageClass : DamageClass
    {
        public override string DisplayName => "Generic";
    }

    internal class NamedDamageClass : DamageClass
    {
        private readonly string _name;
        public NamedDamageClass(string name) => _name = name;
        public override string DisplayName => _name;
    }

    /// <summary>
    /// 命中信息结构体（对齐 tML HitInfo 兼容层）
    /// </summary>
    public struct HitInfo
    {
        public int Damage;
        public float Knockback;
        public int HitDirection;
        public bool Crit;
        public DamageClass DamageType;
        public bool InstantKill;
        public bool HideCombatText;

        public HitInfo(int damage = 0, float knockback = 0f, int hitDirection = 0, bool crit = false, DamageClass damageType = null)
        {
            Damage = damage;
            Knockback = knockback;
            HitDirection = hitDirection;
            Crit = crit;
            DamageType = damageType ?? DamageClass.Default;
            InstantKill = false;
            HideCombatText = false;
        }

        public int SourceDamage => Damage;
    }

    /// <summary>
    /// 命中参数修饰结构体（对齐 tML HitModifiers 兼容层）
    /// </summary>
    public struct HitModifiers
    {
        public DamageClass DamageType;
        public StatModifier SourceDamage;
        public StatModifier FinalDamage;
        public StatModifier Knockback;
        public StatModifier CritDamage;
        public StatModifier Defense;
        public StatModifier ArmorPenetration;
        public int? HitDirectionOverride;

        public HitModifiers()
        {
            DamageType = DamageClass.Default;
            SourceDamage = StatModifier.Default;
            FinalDamage = StatModifier.Default;
            Knockback = StatModifier.Default;
            CritDamage = StatModifier.Default;
            Defense = StatModifier.Default;
            ArmorPenetration = StatModifier.Default;
            HitDirectionOverride = null;
        }
    }

    /// <summary>
    /// 受击信息结构体（对齐 tML HurtInfo 兼容层）
    /// </summary>
    public struct HurtInfo
    {
        public int Damage;
        public float Knockback;
        public int HitDirection;
        public bool Crit;
        public bool Pvp;
        public bool CooldownCounter;
        public DamageClass DamageType;
        public int SourceDamage;
    }

    /// <summary>
    /// 受击参数修饰结构体（对齐 tML HurtModifiers 兼容层）
    /// </summary>
    public struct HurtModifiers
    {
        public DamageClass DamageType;
        public StatModifier SourceDamage;
        public StatModifier FinalDamage;
        public StatModifier Knockback;
        public StatModifier Defense;
        public StatModifier ArmorPenetration;
        public int? HitDirectionOverride;

        public HurtModifiers()
        {
            DamageType = DamageClass.Default;
            SourceDamage = StatModifier.Default;
            FinalDamage = StatModifier.Default;
            Knockback = StatModifier.Default;
            Defense = StatModifier.Default;
            ArmorPenetration = StatModifier.Default;
            HitDirectionOverride = null;
        }
    }

    public abstract class ExtraJump
    {
        public static ExtraJump Flipper { get; }
        public static ExtraJump Basilisk { get; }
        public static ExtraJump BlizzardInABottle { get; }
        public static ExtraJump CloudInABottle { get; }
        public static ExtraJump FartInAJar { get; }
        public static ExtraJump GoatMount { get; }
        public static ExtraJump SandstormInABottle { get; }
        public static ExtraJump SantankMount { get; }
        public static ExtraJump TsunamiInABottle { get; }
        public static ExtraJump UnicornMount { get; }
    }

    /// <summary>
    /// 对齐 tML ExtraJumpState 兼容结构体
    /// </summary>
    public struct ExtraJumpState
    {
        public bool Enabled;
        public bool Available;
        public bool Active;
    }
}

namespace Terraria.ID
{
    public static class TileIDSetsPatches
    {
        public static bool[] CountsAsWaterSource { get; } = new bool[TileID.Count + 1000];
        public static bool[] CountsAsLavaSource { get; } = new bool[TileID.Count + 1000];
        public static bool[] CountsAsHoneySource { get; } = new bool[TileID.Count + 1000];
    }

    public static class NPCIDSetsPatches
    {
        public static System.Collections.Generic.Dictionary<int, int>[] SpecificDebuffImmunity { get; } = new System.Collections.Generic.Dictionary<int, int>[NPCID.Count + 1000];
        public static bool[] CannotSitOnFurniture { get; } = new bool[NPCID.Count + 1000];
    }
}
