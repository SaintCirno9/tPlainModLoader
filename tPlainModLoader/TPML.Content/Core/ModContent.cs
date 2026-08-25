using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace TPML.Content
{
    /// <summary>
    /// TPML 静态内容单例检索与注册中心
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
        }

        public static T GetInstance<T>() where T : class
        {
            if (_instances.TryGetValue(typeof(T), out object val))
            {
                return (T)val;
            }

            foreach (var item in _allContent)
            {
                if (item is T match)
                {
                    _instances[typeof(T)] = match;
                    return match;
                }
            }

            return null;
        }

        public static bool TryFind<T>(string fullName, out T value) where T : class
        {
            value = null;
            if (string.IsNullOrEmpty(fullName)) return false;

            string[] parts = fullName.Split('/');
            if (parts.Length != 2) return false;

            string modName = parts[0];
            string contentName = parts[1];

            foreach (var item in _allContent)
            {
                if (item is T match && item is ModType mt)
                {
                    if (string.Equals(mt.Mod?.Name, modName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(mt.Name, contentName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = match;
                        return true;
                    }
                }
            }

            return false;
        }

        public static T Find<T>(string fullName) where T : class
        {
            if (TryFind<T>(fullName, out T val)) return val;
            throw new KeyNotFoundException($"未找到内容: {fullName}");
        }

        public static int ItemType<T>() where T : ModItem
        {
            if (_itemTypes.TryGetValue(typeof(T), out int id))
                return id;

            var instance = GetInstance<T>();
            if (instance != null)
            {
                _itemTypes[typeof(T)] = instance.Type;
                return instance.Type;
            }

            return 0;
        }

        public static IEnumerable<T> GetContent<T>() where T : class
        {
            return _allContent.OfType<T>();
        }

        public static Asset<Texture2D> Request<T>(string name, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) where T : class
        {
            return Asset<Texture2D>.Empty;
        }
    }
}
