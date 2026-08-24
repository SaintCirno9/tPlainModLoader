using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 静态内容单例检索与注册中心
    /// </summary>
    public static class ModContent
    {
        private static readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();
        private static readonly Dictionary<string, Mod> _mods = new Dictionary<string, Mod>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ILoadable> _allContent = new List<ILoadable>();

        private static readonly Dictionary<Type, int> _itemTypes = new Dictionary<Type, int>();
        private static readonly Dictionary<Type, int> _projTypes = new Dictionary<Type, int>();
        private static readonly Dictionary<Type, int> _npcTypes = new Dictionary<Type, int>();
        private static readonly Dictionary<Type, int> _buffTypes = new Dictionary<Type, int>();
        private static readonly Dictionary<Type, int> _tileTypes = new Dictionary<Type, int>();
        private static readonly Dictionary<Type, int> _wallTypes = new Dictionary<Type, int>();

        public static IReadOnlyCollection<Mod> Mods => _mods.Values;

        public static void RegisterMod(Mod mod)
        {
            if (mod == null) return;
            if (_mods.TryGetValue(mod.Name, out Mod existing) && !ReferenceEquals(existing, mod))
                return;

            _mods[mod.Name] = mod;
            _instances[mod.GetType()] = mod;
        }

        public static void RegisterContent(ILoadable content)
        {
            if (content == null) return;
            if (_allContent.Any(existing => ReferenceEquals(existing, content) ||
                                             existing.GetType() == content.GetType()))
                return;

            _allContent.Add(content);
            _instances[content.GetType()] = content;
        }

        public static void RegisterItemType(Type type, int id)
        {
            if (type != null) _itemTypes[type] = id;
        }

        public static void Clear()
        {
            _instances.Clear();
            _mods.Clear();
            _allContent.Clear();
            _itemTypes.Clear();
            _projTypes.Clear();
            _npcTypes.Clear();
            _buffTypes.Clear();
            _tileTypes.Clear();
            _wallTypes.Clear();
            KeybindLoader.Clear();
            ModPlayerExtensions.ClearInstances();
        }

        public static Mod GetMod(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            _mods.TryGetValue(name, out var mod);
            return mod;
        }

        public static bool TryFind<T>(string name, out T value) where T : class
        {
            value = null;
            if (string.IsNullOrEmpty(name)) return false;

            string modName = "Terraria";
            string entryName = name;
            int slashIndex = name.IndexOf('/');
            if (slashIndex >= 0)
            {
                modName = name.Substring(0, slashIndex);
                entryName = name.Substring(slashIndex + 1);
            }

            foreach (var obj in _allContent)
            {
                if (obj is T match && obj is ModType modType)
                {
                    if ((modType.Mod?.Name == modName || modName == "Terraria") && modType.Name == entryName)
                    {
                        value = match;
                        return true;
                    }
                }
            }
            return false;
        }

        public static T Find<T>(string name) where T : class
        {
            if (TryFind<T>(name, out var value))
                return value;
            throw new KeyNotFoundException($"未找到类型为 {typeof(T).Name} 且名称为 '{name}' 的模组内容。");
        }

        public static T GetInstance<T>() where T : class
        {
            if (_instances.TryGetValue(typeof(T), out object obj))
                return (T)obj;

            // 派生查找 fallback
            foreach (var kvp in _instances)
            {
                if (typeof(T).IsAssignableFrom(kvp.Key))
                    return (T)kvp.Value;
            }
            return default;
        }

        public static int ItemType<T>() where T : ModItem
        {
            return _itemTypes.TryGetValue(typeof(T), out int type) ? type : 0;
        }

        public static int ProjectileType<T>() where T : class
        {
            return _projTypes.TryGetValue(typeof(T), out int type) ? type : 0;
        }

        public static int NPCType<T>() where T : class
        {
            return _npcTypes.TryGetValue(typeof(T), out int type) ? type : 0;
        }

        public static int BuffType<T>() where T : class
        {
            return _buffTypes.TryGetValue(typeof(T), out int type) ? type : 0;
        }

        public static int TileType<T>() where T : class
        {
            return _tileTypes.TryGetValue(typeof(T), out int type) ? type : 0;
        }

        public static int WallType<T>() where T : class
        {
            return _wallTypes.TryGetValue(typeof(T), out int type) ? type : 0;
        }

        public static Asset<T> Request<T>(string name, AssetRequestMode mode = AssetRequestMode.AsyncLoad) where T : class
        {
            if (string.IsNullOrEmpty(name)) return null;

            string normalized = name.Replace("\\", "/");
            string modName = null;
            string assetPath = normalized;

            int slashIdx = normalized.IndexOf('/');
            if (slashIdx >= 0)
            {
                modName = normalized.Substring(0, slashIdx);
                assetPath = normalized.Substring(slashIdx + 1);
            }

            // 1. 如果指定了 Mod 名称，优先从对应 Mod 的 Assets 加载
            if (!string.IsNullOrEmpty(modName) && _mods.TryGetValue(modName, out var mod))
            {
                var modAsset = mod.Assets?.Request<T>(assetPath, mode);
                if (modAsset != null) return modAsset;
            }

            // 2. 遍历所有已加载 Mod 尝试加载
            foreach (var m in _mods.Values)
            {
                var asset = m.Assets?.Request<T>(normalized, mode) ?? m.Assets?.Request<T>(assetPath, mode);
                if (asset != null) return asset;
            }

            // 3. Fallback 原版 Main.Assets
            try
            {
                if (Main.Assets != null)
                {
                    var vanillaAsset = Main.Assets.Request<T>(name, mode);
                    if (vanillaAsset != null) return vanillaAsset;
                }
            }
            catch { }

            // 4. Texture2D 安全兜底
            if (typeof(T) == typeof(Texture2D) && _mods.Count > 0)
            {
                var firstMod = _mods.Values.FirstOrDefault();
                return firstMod?.Assets?.Request<T>(name, mode);
            }

            return null;
        }

        public static IEnumerable<T> GetContent<T>()
        {
            return _allContent.OfType<T>();
        }
    }
}
