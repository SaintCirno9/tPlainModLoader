using System;
using System.IO;
using Terraria;

namespace TPML.Content.IO
{
    /// <summary>
    /// TPML 伴随存档目录与路径管理器
    /// 作者: SaintCirno9
    /// </summary>
    public static class SidecarSaveManager
    {
        private static string _fallbackSavePath;

        public static string BaseSavePath
        {
            get
            {
                if (!string.IsNullOrEmpty(Main.SavePath)) return Main.SavePath;
                if (_fallbackSavePath == null)
                {
                    _fallbackSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
                }
                return _fallbackSavePath;
            }
        }

        public static string SaveDirectory
        {
            get
            {
                string dir = Path.Combine(BaseSavePath, "TPML_Saves");
                try
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                catch { }
                return dir;
            }
        }

        public static string CleanFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public static string GetPlayerSavePath(Player player)
        {
            string name = CleanFileName(player?.name ?? "unknown");
            return Path.Combine(SaveDirectory, $"Player_{name}.tpml_data");
        }

        public static string GetPlayerSavePath(string playerName)
        {
            string name = CleanFileName(playerName ?? "unknown");
            return Path.Combine(SaveDirectory, $"Player_{name}.tpml_data");
        }

        public static string GetWorldSavePath()
        {
            string name = CleanFileName(Main.worldName ?? "unknown");
            int id = Main.worldID;
            return Path.Combine(SaveDirectory, $"World_{name}_{id}.tpml_data");
        }

        public static string GetWorldSavePath(string worldName, int worldId)
        {
            string name = CleanFileName(worldName ?? "unknown");
            return Path.Combine(SaveDirectory, $"World_{name}_{worldId}.tpml_data");
        }

        /// <summary>
        /// 删除指定角色的伴随存档文件
        /// </summary>
        public static void DeletePlayerSave(string playerName)
        {
            try
            {
                string path = GetPlayerSavePath(playerName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch { }
        }

        /// <summary>
        /// 删除指定世界的伴随存档文件
        /// </summary>
        public static void DeleteWorldSave(string worldName, int worldId)
        {
            try
            {
                string path = GetWorldSavePath(worldName, worldId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                // 兜底清理：若存在匹配 World_*_{worldId}.tpml_data 的文件也一并删除
                if (Directory.Exists(SaveDirectory) && worldId > 0)
                {
                    string pattern = $"World_*_{worldId}.tpml_data";
                    foreach (var file in Directory.GetFiles(SaveDirectory, pattern))
                    {
                        try
                        {
                            if (File.Exists(file)) File.Delete(file);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}
