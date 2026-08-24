using System;
using System.Collections.Generic;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 模组静态管理器与查找中心
    /// </summary>
    public static class ModLoader
    {
        public static bool TryGetMod(string name, out Mod result)
        {
            result = ModContent.GetMod(name);
            return result != null;
        }

        public static Mod GetMod(string name)
        {
            return ModContent.GetMod(name);
        }

        public static bool HasMod(string name)
        {
            return ModContent.GetMod(name) != null;
        }

        public static IReadOnlyCollection<Mod> Mods => ModContent.Mods;

        public static Action<string> LogCallback { get; set; }

        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (LogCallback != null)
            {
                LogCallback(message);
                return;
            }

            Console.WriteLine(message);
        }
    }
}
