using System;
using TPML.Core.Json;

namespace tContentPatch.Utils
{
    /// <summary>
    /// Json 简易操作的旧命名空间兼容门面，实现已迁移至 TPML.Core.Json.JsonHelper。
    /// </summary>
    public static class MyJson1
    {
        /// <summary>
        /// 从文件读取并反序列化指定类型对象。
        /// </summary>
        public static T Get2<T>(string FilePath1)
        {
            return JsonHelper.Get2<T>(FilePath1);
        }

        /// <summary>
        /// 从文件读取并反序列化指定类型对象。
        /// </summary>
        public static object Get2(string FilePath1, Type type)
        {
            return JsonHelper.Get2(FilePath1, type);
        }

        /// <summary>
        /// 序列化对象并保存到文件。
        /// </summary>
        public static void Save(object val, string FilePath1, bool indented = false)
        {
            JsonHelper.Save(val, FilePath1, indented);
        }

        /// <summary>
        /// 将字符串反序列化为指定类型对象。
        /// </summary>
        public static T StringToObject<T>(string string1)
        {
            return JsonHelper.StringToObject<T>(string1);
        }
    }
}