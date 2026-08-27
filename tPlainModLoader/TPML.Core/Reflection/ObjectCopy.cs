using System;
using System.Reflection;

namespace TPML.Core.Reflection
{
    /// <summary>
    /// 字段级浅复制工具。
    /// </summary>
    public static class ObjectCopy
    {
        /// <summary>
        /// 复制目标类型对象中的实例字段。
        /// </summary>
        public static T CopyField<T>(T obj, params object[] args)
        {
            if (obj == null) return default;

            FieldInfo[] fis = typeof(T).GetFields();

            T robj = (T)Activator.CreateInstance(typeof(T), args);

            foreach (FieldInfo fi in fis)
            {
                fi.SetValue(robj, fi.GetValue(obj));
            }

            return robj;
        }
    }
}