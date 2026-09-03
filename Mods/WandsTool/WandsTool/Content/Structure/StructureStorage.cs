using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Terraria;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 建筑蓝图持久化存储与剪贴板管理器
    /// </summary>
    public static class StructureStorage
    {
        /// <summary>
        /// 内存剪贴板（即时复制与粘贴，零文件残留）
        /// </summary>
        public static StructureData Clipboard = null;

        /// <summary>
        /// 蓝图文件默认存储目录
        /// </summary>
        public static string BlueprintDirectory
        {
            get
            {
                string baseDir = Main.SavePath;
                if (string.IsNullOrEmpty(baseDir))
                {
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
                }
                string dir = Path.Combine(baseDir, "tPlainModLoader", "Config", "WandsTool", "Blueprints");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
        }

        public class BlueprintJsonModel
        {
            public string Name { get; set; }
            public string BuildTime { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int OriginX { get; set; }
            public int OriginY { get; set; }
            public List<string> SignTexts { get; set; }
            public List<TileSnapshot> FlatTiles { get; set; }
        }

        /// <summary>
        /// 将结构数据保存到本地蓝图文件
        /// </summary>
        public static bool Save(StructureData data, string filename = null)
        {
            if (data == null || data.Tiles == null) return false;

            try
            {
                if (string.IsNullOrWhiteSpace(filename))
                {
                    filename = $"{data.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.wstruct";
                }
                if (!filename.EndsWith(".wstruct") && !filename.EndsWith(".json"))
                {
                    filename += ".wstruct";
                }

                string fullPath = Path.Combine(BlueprintDirectory, filename);

                BlueprintJsonModel model = new BlueprintJsonModel
                {
                    Name = data.Name,
                    BuildTime = data.BuildTime,
                    Width = data.Width,
                    Height = data.Height,
                    OriginX = data.OriginX,
                    OriginY = data.OriginY,
                    SignTexts = data.SignTexts ?? new List<string>(),
                    FlatTiles = new List<TileSnapshot>(data.Width * data.Height)
                };

                for (int x = 0; x < data.Width; x++)
                {
                    for (int y = 0; y < data.Height; y++)
                    {
                        model.FlatTiles.Add(data.Tiles[x, y]);
                    }
                }

                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(fullPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 蓝图保存失败: {ex.Message}", 255, 60, 60);
                return false;
            }
        }

        /// <summary>
        /// 从本地文件加载蓝图结构
        /// </summary>
        public static StructureData Load(string filepath)
        {
            if (!File.Exists(filepath)) return null;

            try
            {
                string json = File.ReadAllText(filepath);
                BlueprintJsonModel model = JsonConvert.DeserializeObject<BlueprintJsonModel>(json);
                if (model == null || model.Width <= 0 || model.Height <= 0 || model.FlatTiles == null) return null;

                StructureData data = new StructureData(model.Width, model.Height, model.Name)
                {
                    BuildTime = model.BuildTime,
                    OriginX = model.OriginX,
                    OriginY = model.OriginY,
                    SignTexts = model.SignTexts ?? new List<string>()
                };

                int index = 0;
                for (int x = 0; x < data.Width; x++)
                {
                    for (int y = 0; y < data.Height; y++)
                    {
                        if (index < model.FlatTiles.Count)
                        {
                            data.Tiles[x, y] = model.FlatTiles[index++];
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 蓝图读取失败: {ex.Message}", 255, 60, 60);
                return null;
            }
        }

        /// <summary>
        /// 获取所有已保存的蓝图文件路径列表
        /// </summary>
        public static List<string> GetSavedBlueprintFiles()
        {
            List<string> list = new List<string>();
            try
            {
                string dir = BlueprintDirectory;
                if (Directory.Exists(dir))
                {
                    list.AddRange(Directory.GetFiles(dir, "*.wstruct"));
                    list.AddRange(Directory.GetFiles(dir, "*.json"));
                }
            }
            catch { }
            return list;
        }

        /// <summary>
        /// 重命名已存在的本地蓝图文件及其内部元数据
        /// </summary>
        public static bool Rename(string oldFilePath, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldFilePath) || !File.Exists(oldFilePath))
            {
                Main.NewText("[魔杖] 目标蓝图文件不存在！", 255, 60, 60);
                return false;
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                Main.NewText("[魔杖] 蓝图名称不能为空！", 255, 60, 60);
                return false;
            }

            // 清洗文件名中的非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                newName = newName.Replace(c, '_');
            }
            newName = newName.Trim();

            try
            {
                StructureData data = Load(oldFilePath);
                if (data == null) return false;

                string oldName = data.Name;
                data.Name = newName;

                // 生成新的文件名（保持原扩展名）
                string ext = Path.GetExtension(oldFilePath);
                if (string.IsNullOrEmpty(ext)) ext = ".wstruct";
                string newFileName = $"{newName}{ext}";
                string newFilePath = Path.Combine(BlueprintDirectory, newFileName);

                // 保存新文件
                bool ok = Save(data, newFileName);
                if (ok)
                {
                    // 若新旧路径不同，删除旧文件
                    if (!string.Equals(Path.GetFullPath(oldFilePath), Path.GetFullPath(newFilePath), StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Delete(oldFilePath);
                        }
                        catch { }
                    }

                    // 若剪贴板正在使用此蓝图，同步更新剪贴板名字
                    if (Clipboard != null && (Clipboard.Name == oldName || Clipboard == data))
                    {
                        Clipboard.Name = newName;
                    }

                    Main.NewText($"[魔杖] 蓝图已重命名为: {newName}", 100, 255, 100);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 重命名失败: {ex.Message}", 255, 60, 60);
                return false;
            }
        }

        /// <summary>
        /// 在 Windows 资源管理器中打开蓝图目录
        /// </summary>
        public static void OpenInExplorer()
        {
            try
            {
                string dir = BlueprintDirectory;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{dir}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Main.NewText($"[魔杖] 无法打开文件夹: {ex.Message}", 255, 60, 60);
            }
        }
    }
}
