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

        public static string GetWorldSavePath()
        {
            string name = CleanFileName(Main.worldName ?? "unknown");
            int id = Main.worldID;
            return Path.Combine(SaveDirectory, $"World_{name}_{id}.tpml_data");
        }
    }
}
