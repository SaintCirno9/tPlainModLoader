using System;

namespace TPML.Prepatcher
{
    /// <summary>
    /// 标记用于访问通过 Prepatcher 动态注入到目标类中的原生字段扩展方法。<para/>
    /// 扩展方法必须为静态方法，首个参数为目标宿主类（如 <c>this Player player</c>）。<para/>
    /// 返回值可以是 <c>ref T</c>（读写引用）或 <c>T</c>（只读值）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class PrepatcherFieldAttribute : Attribute
    {
        /// <summary>
        /// 自定义注入字段的名称。若为 null，则由 Prepatcher 自动生成唯一字段名。
        /// </summary>
        public string FieldName { get; }

        public PrepatcherFieldAttribute(string fieldName = null)
        {
            FieldName = fieldName;
        }
    }
}
