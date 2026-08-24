using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Terraria.ModLoader.Container
{
    /// <summary>
    /// 解析后的 .tmod 虚拟容器数据结构
    /// </summary>
    public class TModFileContainer
    {
        public string FilePath { get; set; }
        public string ModName { get; set; }
        public string ModVersion { get; set; }
        public string TmlVersion { get; set; }
        public Dictionary<string, byte[]> Files { get; } = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public byte[] MainAssemblyBytes
        {
            get
            {
                if (Files.TryGetValue($"{ModName}.dll", out var bytes))
                    return bytes;
                // 搜索任意 dll
                foreach (var kvp in Files)
                {
                    if (kvp.Key.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        return kvp.Value;
                }
                return null;
            }
        }
    }

    /// <summary>
    /// .tmod 文件流式解析器
    /// </summary>
    public static class TModContainerReader
    {
        public static string Read7BitEncodedString(BinaryReader reader)
        {
            int length = 0;
            int shift = 0;
            while (true)
            {
                byte b = reader.ReadByte();
                length |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    break;
                shift += 7;
            }
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        public static TModFileContainer Read(string tmodPath)
        {
            if (!File.Exists(tmodPath))
                throw new FileNotFoundException($"未找到 .tmod 文件: {tmodPath}");

            using (var fileStream = File.OpenRead(tmodPath))
            using (var reader = new BinaryReader(fileStream))
            {
                byte[] magic = reader.ReadBytes(4);
                if (Encoding.ASCII.GetString(magic) != "TMOD")
                    throw new InvalidDataException($"无效的 .tmod 文件头魔数: {Encoding.ASCII.GetString(magic)}");

                string tmlVersion = Read7BitEncodedString(reader);
                byte[] hash = reader.ReadBytes(20);
                byte[] sig = reader.ReadBytes(256);
                int dataLen = reader.ReadInt32();

                byte[] payload = reader.ReadBytes(dataLen);
                using (var sha1 = SHA1.Create())
                {
                    byte[] computed = sha1.ComputeHash(payload);
                    bool match = true;
                    for (int i = 0; i < 20; i++)
                    {
                        if (hash[i] != computed[i]) { match = false; break; }
                    }
                    if (!match)
                    {
                        Console.WriteLine($"[TModContainerReader] 警告: SHA1 校验不一致 ({Path.GetFileName(tmodPath)})");
                    }
                }

                using (var ms = new MemoryStream(payload))
                using (var pReader = new BinaryReader(ms))
                {
                    string modName = Read7BitEncodedString(pReader);
                    string modVersion = Read7BitEncodedString(pReader);
                    int fileCount = pReader.ReadInt32();

                    var entries = new List<(string Name, int Size, int CSize)>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        string name = Read7BitEncodedString(pReader);
                        int size = pReader.ReadInt32();
                        int csize = pReader.ReadInt32();
                        entries.Add((name, size, csize));
                    }

                    var container = new TModFileContainer
                    {
                        FilePath = tmodPath,
                        ModName = modName,
                        ModVersion = modVersion,
                        TmlVersion = tmlVersion
                    };

                    foreach (var entry in entries)
                    {
                        byte[] cdata = pReader.ReadBytes(entry.CSize);
                        byte[] data;
                        if (entry.Size != entry.CSize)
                        {
                            using (var cstream = new MemoryStream(cdata))
                            using (var deflate = new DeflateStream(cstream, CompressionMode.Decompress))
                            using (var outStream = new MemoryStream(entry.Size))
                            {
                                deflate.CopyTo(outStream);
                                data = outStream.ToArray();
                            }
                        }
                        else
                        {
                            data = cdata;
                        }

                        container.Files[entry.Name] = data;
                    }

                    Console.WriteLine($"[TModContainerReader] 成功解析 {modName} v{modVersion} (包含 {fileCount} 个文件)");
                    return container;
                }
            }
        }
    }
}
