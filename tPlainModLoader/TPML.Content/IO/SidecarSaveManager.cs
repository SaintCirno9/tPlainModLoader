using System;
using System.IO;
using Terraria;

namespace TPML.Content.IO
{
    public static class SidecarSaveManager
    {
        public static string SaveDirectory => Path.Combine(Main.SavePath, "TPML_Saves");

        static SidecarSaveManager()
        {
            try
            {
                if (!Directory.Exists(SaveDirectory))
                {
                    Directory.CreateDirectory(SaveDirectory);
                }
            }
            catch { }
        }

        public static string GetPlayerSavePath(Player player)
        {
            string name = player?.name ?? "unknown";
            return Path.Combine(SaveDirectory, $"Player_{name}.tpml");
        }

        public static string GetWorldSavePath()
        {
            string name = Main.worldName ?? "unknown";
            return Path.Combine(SaveDirectory, $"World_{name}.tpml");
        }
    }
}
