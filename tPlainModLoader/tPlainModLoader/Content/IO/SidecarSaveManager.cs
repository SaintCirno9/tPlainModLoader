using System;
using System.IO;
using Terraria;
using Terraria.IO;
using Terraria.Utilities;

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

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Player, string> _playerFileCache = new System.Collections.Concurrent.ConcurrentDictionary<Player, string>();

        /// <summary>
        /// 显式绑定 Player 实例与其对应的 .plr 文件路径，确保后续所有无上下文调用路径 100% 绝对一致
        /// </summary>
        public static void BindPlayerFilePath(Player player, string playerFilePath)
        {
            if (player == null || string.IsNullOrEmpty(playerFilePath)) return;
            _playerFileCache[player] = playerFilePath;
        }

        /// <summary>
        /// 角色伴随存档路径：优先使用 .plr 文件名（稳定、可区分同名角色），并兼容旧的按显示名命名文件。
        /// </summary>
        public static string GetPlayerSavePath(Player player)
        {
            return GetPlayerSavePath(player, TryGetPlayerFilePath(player));
        }

        public static string GetPlayerSavePath(Player player, string playerFilePath)
        {
            if (player != null && !string.IsNullOrEmpty(playerFilePath))
            {
                _playerFileCache[player] = playerFilePath;
            }

            string nameKey = CleanFileName(player?.name ?? "unknown");
            string namePath = Path.Combine(SaveDirectory, $"Player_{nameKey}.tpml_data");
            string fileKey = GetPlayerFileKey(playerFilePath);
            if (string.IsNullOrEmpty(fileKey) || string.Equals(fileKey, nameKey, StringComparison.OrdinalIgnoreCase))
            {
                return namePath;
            }

            string filePath = Path.Combine(SaveDirectory, $"Player_{CleanFileName(fileKey)}.tpml_data");
            TryMigrateLegacyPlayerFile(namePath, filePath);
            return filePath;
        }

        public static string GetPlayerSavePath(string playerName)
        {
            string name = CleanFileName(playerName ?? "unknown");
            return Path.Combine(SaveDirectory, $"Player_{name}.tpml_data");
        }

        private static string TryGetPlayerFilePath(Player player)
        {
            if (player == null) return null;
            if (_playerFileCache.TryGetValue(player, out string cachedPath) && !string.IsNullOrEmpty(cachedPath))
            {
                return cachedPath;
            }

            try
            {
                PlayerFileData active = Main.ActivePlayerFileData;
                if (active?.Player == player && !string.IsNullOrEmpty(active.Path))
                {
                    _playerFileCache[player] = active.Path;
                    return active.Path;
                }
            }
            catch
            {
            }
            return null;
        }

        private static string GetPlayerFileKey(string playerFilePath)
        {
            if (string.IsNullOrEmpty(playerFilePath)) return null;
            try
            {
                return FileUtilities.GetFileName(playerFilePath, includeExtension: false);
            }
            catch
            {
                return Path.GetFileNameWithoutExtension(playerFilePath);
            }
        }

        private static void TryMigrateLegacyPlayerFile(string namePath, string filePath)
        {
            try
            {
                if (File.Exists(filePath) || !File.Exists(namePath)) return;
                File.Copy(namePath, filePath, overwrite: false);
            }
            catch
            {
            }
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
