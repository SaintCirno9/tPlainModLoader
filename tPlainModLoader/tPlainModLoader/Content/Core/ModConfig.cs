using System;
using Terraria.Localization;

namespace TPML.Content
{
    /// <summary>
    /// 模组配置作用域枚举
    /// </summary>
    public enum ConfigScope
    {
        ServerSide,
        ClientSide
    }

    /// <summary>
    /// TPML 模组配置抽象基类（对齐 tModLoader ModConfig）
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ModConfig : ModType
    {
        public virtual ConfigScope Mode => ConfigScope.ClientSide;

        public virtual bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
        {
            return false;
        }

        public virtual bool NeedsReload(ModConfig pending)
        {
            return false;
        }

        public virtual void OnChanged()
        {
        }

        public virtual ModConfig Clone()
        {
            return (ModConfig)MemberwiseClone();
        }
    }

    /// <summary>
    /// 配置分组标题特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class HeaderAttribute : Attribute
    {
        public string Header { get; }

        public HeaderAttribute(string header)
        {
            Header = header;
        }
    }

    /// <summary>
    /// 配置数值范围特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RangeAttribute : Attribute
    {
        public object Min { get; }
        public object Max { get; }

        public RangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public RangeAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public RangeAttribute(uint min, uint max)
        {
            Min = min;
            Max = max;
        }

        public RangeAttribute(object min, object max)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// 配置数值步进量特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IncrementAttribute : Attribute
    {
        public object Increment { get; }

        public IncrementAttribute(float increment)
        {
            Increment = increment;
        }

        public IncrementAttribute(int increment)
        {
            Increment = increment;
        }

        public IncrementAttribute(uint increment)
        {
            Increment = increment;
        }

        public IncrementAttribute(object increment)
        {
            Increment = increment;
        }
    }

    /// <summary>
    /// 滑动条 UI 特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SliderAttribute : Attribute
    {
    }

    /// <summary>
    /// 刻度线 UI 特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class DrawTicksAttribute : Attribute
    {
    }

    /// <summary>
    /// 需重启游戏生效标记特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class ReloadRequiredAttribute : Attribute
    {
    }
}
