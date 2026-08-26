using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace RecipeBrowser.Common
{
    /// <summary>
    /// RecipeBrowser UI 贴图与材质资源管理器
    /// 作者: SaintCirno9
    /// </summary>
    public static class RBTextures
    {
        private static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static readonly Assembly _assembly = typeof(RBTextures).Assembly;

        // UI 元件通用贴图
        public static Texture2D FavoritedOverlay => GetTexture("Images/FavoritedOverlay");
        public static Texture2D SelectedOverlay => GetTexture("Images/SelectedOverlay");
        public static Texture2D AbleToCraftBackground => GetTexture("Images/CanCraftBackground");
        public static Texture2D AbleToCraftExtendedBackground => GetTexture("Images/CanCraftExtendedBackground");
        public static Texture2D Checkbox => GetTexture("UIElements/checkBox");
        public static Texture2D Checkmark => GetTexture("UIElements/checkMark");
        public static Texture2D MoreLeft => GetTexture("UIElements/MoreLeft");
        public static Texture2D MoreRight => GetTexture("UIElements/MoreRight");
        public static Texture2D MoreUp => GetTexture("UIElements/MoreUp");
        public static Texture2D MoreDown => GetTexture("UIElements/MoreDown");
        public static Texture2D DuplicateOn => GetTexture("Images/duplicateOn");
        public static Texture2D DuplicateOff => GetTexture("Images/duplicateOff");
        public static Texture2D CloseButton => GetTexture("UIElements/closeButton");
        public static Texture2D HistoryBack => GetTexture("UIElements/historyBack");
        public static Texture2D HistoryForward => GetTexture("UIElements/historyForward");
        public static Texture2D FilterMod => GetTexture("Images/filterMod");
        public static Texture2D FilterModColorable => GetTexture("Images/filterModColorable");
        public static Texture2D UniqueTile => GetTexture("Images/uniqueTile");
        public static Texture2D BugNet => GetTexture("Images/bugNet");
        public static Texture2D CategoryArmorSets => GetTexture("Images/categoryArmorSets");
        public static Texture2D TileByHand => GetTexture("Images/tileByHand");

        public static Texture2D GetTexture(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            // 规范化路径 key（去扩展名，使用正斜杠）
            string key = name.Replace('\\', '/');
            if (key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(0, key.Length - 4);
            }
            if (key.StartsWith("RecipeBrowser/Resources/", StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring("RecipeBrowser/Resources/".Length);
            }
            if (key.StartsWith("RecipeBrowser/", StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring("RecipeBrowser/".Length);
            }

            if (_textures.TryGetValue(key, out var tex) && tex != null && !tex.IsDisposed)
            {
                return tex;
            }

            // 1. 尝试从内嵌资源读取
            string resName = $"RecipeBrowser.Resources.{key.Replace('/', '.')}.png";
            using (Stream stream = _assembly.GetManifestResourceStream(resName))
            {
                if (stream != null && Main.graphics?.GraphicsDevice != null)
                {
                    try
                    {
                        tex = Texture2D.FromStream(Main.graphics.GraphicsDevice, stream);
                        _textures[key] = tex;
                        return tex;
                    }
                    catch { }
                }
            }

            // 2. 尝试从本地磁盘目录读取
            string modRoot = Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"))
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "RecipeBrowser")
                : Path.Combine(Directory.GetCurrentDirectory(), "tPlainModLoader", "Mods", "RecipeBrowser");
            string diskPath = Path.Combine(modRoot, "Resources", $"{key.Replace('/', Path.DirectorySeparatorChar)}.png");
            if (File.Exists(diskPath) && Main.graphics?.GraphicsDevice != null)
            {
                try
                {
                    using (FileStream fs = File.OpenRead(diskPath))
                    {
                        tex = Texture2D.FromStream(Main.graphics.GraphicsDevice, fs);
                        _textures[key] = tex;
                        return tex;
                    }
                }
                catch { }
            }

            return null;
        }

        public static void Clear()
        {
            foreach (var kv in _textures)
            {
                try
                {
                    if (kv.Value != null && !kv.Value.IsDisposed) kv.Value.Dispose();
                }
                catch { }
            }
            _textures.Clear();
        }
    }
}
