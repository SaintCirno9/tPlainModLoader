using System;
using System.Reflection;

namespace TPML.Content
{
    /// <summary>
    /// 原版与模组静态 Sets 集合数组自动扩容工具类
    /// 作者: SaintCirno9
    /// </summary>
    public static class ArrayResizer
    {
        /// <summary>
        /// 递归遍历指定类型及其嵌套类型中的所有一维静态数组字段，当其长度在 [minMatchLen, required] 范围内时执行动态扩容
        /// </summary>
        /// <param name="type">目标宿主类型（例如 ItemID.Sets）</param>
        /// <param name="required">所需的最小容量</param>
        /// <param name="minMatchLen">匹配字段的原数组最小长度门槛（防止误扩容非相关小数组）</param>
        public static void ResizeSets(Type type, int required, int minMatchLen = 0)
        {
            if (type == null) return;

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.FieldType.IsArray && field.FieldType.GetArrayRank() == 1)
                {
                    Array arr = field.GetValue(null) as Array;
                    if (arr != null && arr.Length >= minMatchLen && arr.Length <= required)
                    {
                        int newLen = Math.Max(required, arr.Length * 2);
                        Array newArr = Array.CreateInstance(field.FieldType.GetElementType(), newLen);
                        Array.Copy(arr, newArr, arr.Length);
                        field.SetValue(null, newArr);
                    }
                }
            }

            Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < nestedTypes.Length; i++)
            {
                ResizeSets(nestedTypes[i], required, minMatchLen);
            }
        }
    }
}
