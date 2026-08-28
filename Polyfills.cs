// C# 现代语法 polyfill（net472 专用）
// ---------------------------------------------------------------------------
// 为 .NET Framework 4.7.2 目标补齐 record / init（C# 9）与 required（C# 11）
// 所需的编译器辅助类型。本文件由 Directory.Build.props 全局注入每个 SDK 风格工程，
// 每个程序集各自编译一份 internal 副本：
//   - 声明 record / init / required 的程序集在自身副本上即可解析；
//   - 跨程序集使用（对象初始化器 new Record { X = 5 } 等）经实测同样可用。
// 注意：类型名与命名空间是编译器约定，禁止修改；本文件勿加入公共 API 文档范围。
// ---------------------------------------------------------------------------
#nullable disable
namespace System.Runtime.CompilerServices
{
    /// <summary>init 访问器与 record 需要（C# 9）。</summary>
    internal static class IsExternalInit
    {
    }

    /// <summary>required 成员需要（C# 11）。</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute
    {
    }

    /// <summary>required 成员需要（C# 11），随特性记录来源语言特性名。</summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }
    }
}
