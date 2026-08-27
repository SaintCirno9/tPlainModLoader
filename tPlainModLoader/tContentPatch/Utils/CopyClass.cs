using TPML.Core.Reflection;

namespace tContentPatch.Utils
{
    /// <summary>
    /// 复制类的旧命名空间兼容门面，实现已迁移至 TPML.Core.Reflection.ObjectCopy。
    /// </summary>
    public static class CopyClass
    {
        /// <summary>
        /// 复制目标类型对象中的实例字段。
        /// </summary>
        public static T CopyField<T>(T obj, params object[] args)
        {
            return ObjectCopy.CopyField(obj, args);
        }
    }
}