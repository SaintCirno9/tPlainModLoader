using System;
using System.Collections.Generic;
using System.IO;

namespace TPML.Prepatcher
{
    /// <summary>
    /// Prepatcher 内存程序集字节流缓存中心。<para/>
    /// 用于存放经 Prepatcher IL 重写后的 Mod 程序集字节流，避免重复磁盘 I/O 并确保加载修补后的版本。
    /// </summary>
    public static class PrepatcherStorage
    {
        private static readonly Dictionary<string, byte[]> patchedAssemblies = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 注册已被 Prepatcher 修补过的 Mod 程序集字节流。
        /// </summary>
        /// <param name="assemblyFilePath">Mod DLL 绝对路径</param>
        /// <param name="assemblyBytes">修补后的字节流</param>
        public static void RegisterPatchedBytes(string assemblyFilePath, byte[] assemblyBytes)
        {
            if (string.IsNullOrEmpty(assemblyFilePath) || assemblyBytes == null)
                return;

            string normalizedPath = Path.GetFullPath(assemblyFilePath);
            lock (patchedAssemblies)
            {
                patchedAssemblies[normalizedPath] = assemblyBytes;
            }
        }

        /// <summary>
        /// 尝试获取已被 Prepatcher 修补过的 Mod 程序集字节流。
        /// </summary>
        /// <param name="assemblyFilePath">Mod DLL 绝对路径</param>
        /// <param name="assemblyBytes">修补后的字节流</param>
        /// <returns>若存在已修补的字节流则返回 true</returns>
        public static bool TryGetPatchedBytes(string assemblyFilePath, out byte[] assemblyBytes)
        {
            if (string.IsNullOrEmpty(assemblyFilePath))
            {
                assemblyBytes = null;
                return false;
            }

            string normalizedPath = Path.GetFullPath(assemblyFilePath);
            lock (patchedAssemblies)
            {
                return patchedAssemblies.TryGetValue(normalizedPath, out assemblyBytes);
            }
        }

        /// <summary>
        /// 清理所有缓存的修补字节流。
        /// </summary>
        public static void Clear()
        {
            lock (patchedAssemblies)
            {
                patchedAssemblies.Clear();
            }
        }
    }
}
