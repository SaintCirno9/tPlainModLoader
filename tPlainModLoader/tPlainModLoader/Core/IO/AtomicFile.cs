using System;
using System.IO;
using System.Text;

namespace TPML.Core.IO
{
    /// <summary>
    /// 原子写盘与损坏文件备份原语：先写临时文件再替换目标，避免崩溃截断后被空数据覆盖。
    /// 作者: SaintCirno9
    /// </summary>
    public static class AtomicFile
    {
        /// <summary>
        /// 将文本原子写入目标路径（临时文件 + Replace/Move）。
        /// </summary>
        public static void WriteAllText(string path, string contents, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            encoding = encoding ?? new UTF8Encoding(false);

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tmpPath = path + ".tmp";
            File.WriteAllText(tmpPath, contents ?? string.Empty, encoding);
            Replace(tmpPath, path);
        }

        /// <summary>
        /// 用临时文件原子替换目标文件。
        /// </summary>
        public static void Replace(string sourceTempPath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourceTempPath)) throw new ArgumentNullException(nameof(sourceTempPath));
            if (string.IsNullOrEmpty(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

            if (File.Exists(destinationPath))
            {
                try
                {
                    File.Replace(sourceTempPath, destinationPath, destinationPath + ".bak", true);
                    TryDelete(destinationPath + ".bak");
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (IOException)
                {
                }

                TryDelete(destinationPath);
            }

            File.Move(sourceTempPath, destinationPath);
        }

        /// <summary>
        /// 将损坏的目标文件移走为 <c>*.corrupt</c>（同名已存在则追加序号），绝不原地覆盖。
        /// </summary>
        public static string BackupCorrupt(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

            string backup = path + ".corrupt";
            int n = 0;
            while (File.Exists(backup))
            {
                n++;
                backup = path + ".corrupt." + n;
            }

            try
            {
                File.Move(path, backup);
                return backup;
            }
            catch
            {
                try
                {
                    File.Copy(path, backup);
                    return backup;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
