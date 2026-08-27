using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace TPML.Core.Json
{
    /// <summary>
    /// Json 简易序列化与文件读写工具，供宿主与模组复用。
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// 从文件读取并反序列化指定类型对象。
        /// </summary>
        public static T Get2<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) throw new Exception($"文件不存在:{filePath}");

                return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                MethodBase mb = MethodBase.GetCurrentMethod();
                throw new Exception($"{mb?.DeclaringType.Name}.{mb?.Name}:{ex.Message}");
            }
        }

        /// <summary>
        /// 从文件读取并反序列化指定类型对象。
        /// </summary>
        public static object Get2(string filePath, Type type)
        {
            try
            {
                if (!File.Exists(filePath)) throw new Exception($"文件不存在:{filePath}");

                return JsonConvert.DeserializeObject(File.ReadAllText(filePath, Encoding.UTF8), type);
            }
            catch (Exception ex)
            {
                MethodBase mb = MethodBase.GetCurrentMethod();
                throw new Exception($"{mb?.DeclaringType.Name}.{mb?.Name}:{ex.Message}");
            }
        }

        /// <summary>
        /// 序列化对象并写入文件。
        /// </summary>
        public static void Save(object val, string filePath, bool indented = false)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory)) throw new Exception($"目录不存在:{directory}");

                string text = indented
                    ? JsonConvert.SerializeObject(val, Formatting.Indented)
                    : JsonConvert.SerializeObject(val);

                File.WriteAllText(filePath, text, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MethodBase mb = MethodBase.GetCurrentMethod();
                throw new Exception($"{mb?.DeclaringType.Name}.{mb?.Name}:{ex.Message}");
            }
        }

        /// <summary>
        /// 字符串转对象。
        /// </summary>
        public static T StringToObject<T>(string text)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(text);
            }
            catch (Exception ex)
            {
                MethodBase mb = MethodBase.GetCurrentMethod();
                throw new Exception($"{mb?.DeclaringType.Name}.{mb?.Name}:{ex.Message}");
            }
        }
    }
}