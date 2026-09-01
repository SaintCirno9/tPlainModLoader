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
        private static readonly Dictionary<Type, int> _tileEntityTypes = new Dictionary<Type, int>();
        private static readonly Dictionary<Type, int> _wallTypes = new Dictionary<Type, int>();

        public static IReadOnlyCollection<Mod> Mods => _mods.Values;

        public static void RegisterMod(Mod mod)
        {
            if (mod == null) return;
            if (_mods.TryGetValue(mod.Name, out Mod existing) && !ReferenceEquals(existing, mod))
                return;

            _mods[mod.Name] = mod;
            _instances[mod.GetType()] = mod;
            Localization.LocalizationLoader.LoadModLocalization(mod);
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

        public static void RegisterNPCType(Type type, int id)
        {
            if (type != null) _npcTypes[type] = id;
        }

        public static void RegisterProjectileType(Type type, int id)
        {
            if (type != null) _projTypes[type] = id;
        }

        public static void RegisterBuffType(Type type, int id)
        {
            if (type != null) _buffTypes[type] = id;
        }

        public static void RegisterTileType(Type type, int id)
        {
            if (type != null) _tileTypes[type] = id;
        }

        public static void RegisterTileEntityType(Type type, int id)
        {
            if (type != null) _tileEntityTypes[type] = id;
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
            _tileEntityTypes.Clear();
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
                if (item is T match || item.GetType().FullName == typeof(T).FullName)
                {
                    _instances[typeof(T)] = item;
                    return item as T;
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
                if ((item is T || item.GetType().FullName == typeof(T).FullName) && item is ModType mt)
                {
                    if (string.Equals(mt.Mod?.Name, modName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(mt.Name, contentName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = item as T;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryFind<T>(string modName, string name, out T value) where T : class
        {
            return TryFind<T>($"{modName}/{name}", out value);
        }

        public static T Find<T>(string fullName) where T : class
        {
            if (TryFind<T>(fullName, out T val)) return val;
            throw new KeyNotFoundException($"未找到内容: {fullName}");
        }

        public static T Find<T>(string modName, string name) where T : class
        {
            return Find<T>($"{modName}/{name}");
        }

        public static int ItemType<T>() where T : ModItem
        {
            if (_itemTypes.TryGetValue(typeof(T), out int id))
                return id;

            foreach (var kvp in _itemTypes)
            {
                if (kvp.Key.FullName == typeof(T).FullName || kvp.Key.Name == typeof(T).Name)
                {
                    _itemTypes[typeof(T)] = kvp.Value;
                    return kvp.Value;
                }
            }

            var instance = GetInstance<T>();
            if (instance != null)
            {
                _itemTypes[typeof(T)] = instance.Type;
                return instance.Type;
            }

            return 0;
        }

        public static int ItemType(string modName, string itemName) => ItemLoader.ItemType(modName, itemName);
        public static int ItemType(string fullName) => ItemLoader.ItemType(fullName);
        public static ModItem GetModItem(int type) => ItemLoader.GetModItem(type);

        public static int NPCType<T>() where T : ModNPC
        {
            if (_npcTypes.TryGetValue(typeof(T), out int id))
                return id;

            foreach (var kvp in _npcTypes)
            {
                if (kvp.Key.FullName == typeof(T).FullName || kvp.Key.Name == typeof(T).Name)
                {
                    _npcTypes[typeof(T)] = kvp.Value;
                    return kvp.Value;
                }
            }

            var instance = GetInstance<T>();
            if (instance != null)
            {
                _npcTypes[typeof(T)] = instance.Type;
                return instance.Type;
            }

            return 0;
        }

        public static int NPCType(string modName, string npcName) => NPCLoader.NPCType(modName, npcName);
        public static int NPCType(string fullName) => NPCLoader.NPCType(fullName);
        public static ModNPC GetModNPC(int type) => NPCLoader.GetModNPC(type);

        public static int ProjectileType<T>() where T : ModProjectile
        {
            if (_projTypes.TryGetValue(typeof(T), out int id))
                return id;

            foreach (var kvp in _projTypes)
            {
                if (kvp.Key.FullName == typeof(T).FullName || kvp.Key.Name == typeof(T).Name)
                {
                    _projTypes[typeof(T)] = kvp.Value;
                    return kvp.Value;
                }
            }

            var instance = GetInstance<T>();
            if (instance != null)
            {
                _projTypes[typeof(T)] = instance.Type;
                return instance.Type;
            }

            return 0;
        }

        public static int ProjectileType(string modName, string projName) => ProjectileLoader.ProjectileType(modName, projName);
        public static int ProjectileType(string fullName) => ProjectileLoader.ProjectileType(fullName);
        public static ModProjectile GetModProjectile(int type) => ProjectileLoader.GetModProjectile(type);

        public static int BuffType<T>() where T : ModBuff
        {
            if (_buffTypes.TryGetValue(typeof(T), out int id))
                return id;

            foreach (var kvp in _buffTypes)
            {
                if (kvp.Key.FullName == typeof(T).FullName || kvp.Key.Name == typeof(T).Name)
                {
                    _buffTypes[typeof(T)] = kvp.Value;
                    return kvp.Value;
                }
            }

            var instance = GetInstance<T>();
            if (instance != null)
            {
                _buffTypes[typeof(T)] = instance.Type;
                return instance.Type;
            }

            return 0;
        }

        public static int BuffType(string modName, string buffName) => BuffLoader.BuffType(modName, buffName);
        public static int BuffType(string fullName) => BuffLoader.BuffType(fullName);
        public static ModBuff GetModBuff(int type) => BuffLoader.GetBuff(type);

        public static int GetModHeadSlot(string headTexture) => NPCLoader.GetHeadSlot(headTexture);

        public static int TileType<T>() where T : ModTile
        {
            if (_tileTypes.TryGetValue(typeof(T), out int id))
                return id;

            foreach (var kvp in _tileTypes)
            {
                if (kvp.Key.FullName == typeof(T).FullName || kvp.Key.Name == typeof(T).Name)
                {
                    _tileTypes[typeof(T)] = kvp.Value;
                    return kvp.Value;
                }
            }

            var instance = GetInstance<T>();
            if (instance != null)
            {
                _tileTypes[typeof(T)] = instance.Type;
                return instance.Type;
            }

            return 0;
        }

        public static int TileType(string modName, string tileName) => TileLoader.TileType(modName, tileName);
        public static int TileType(string fullName) => TileLoader.TileType(fullName);
        public static ModTile GetModTile(int type) => TileLoader.GetTile(type);

        public static int TileEntityType<T>() where T : ModTileEntity
        {
            if (_tileEntityTypes.TryGetValue(typeof(T), out int id))
                return id;

            foreach (var kvp in _tileEntityTypes)
            {
                if (kvp.Key.FullName == typeof(T).FullName || kvp.Key.Name == typeof(T).Name)
                {
                    _tileEntityTypes[typeof(T)] = kvp.Value;
                    return kvp.Value;
                }
            }

            var instance = GetInstance<T>();
            if (instance != null)
            {
                _tileEntityTypes[typeof(T)] = instance.Type;
                return instance.Type;
            }

            return 0;
        }

        public static int TileEntityType(string fullName) => TileEntityLoader.TileEntityType(fullName);
        public static ModTileEntity GetModTileEntity(int type) => TileEntityLoader.GetEntity(type);

        public static IEnumerable<T> GetContent<T>() where T : class
        {
            return _allContent.OfType<T>();
        }

        public static bool TryGetMod(string name, out Mod mod)
        {
            mod = null;
            if (string.IsNullOrEmpty(name)) return false;
            return _mods.TryGetValue(name, out mod);
        }

        public static Mod GetMod(string name)
        {
            if (TryGetMod(name, out Mod mod)) return mod;
            return null;
        }

        public static void PostSetupContent()
        {
            foreach (var mod in _mods.Values)
            {
                try
                {
                    mod.PostSetupContent();
                }
                catch (Exception ex)
                {
                    ModLoader.Log($"[ModContent] 模组 {mod.Name} PostSetupContent 异常: {ex.Message}");
                }
            }

            foreach (var content in _allContent)
            {
                if (content is ModType mt)
                {
                    try
                    {
                        mt.PostSetupContent();
                    }
                    catch (Exception ex)
                    {
                        ModLoader.Log($"[ModContent] 内容 {mt.FullName} PostSetupContent 异常: {ex.Message}");
                    }
                }
            }
        }

        public static Asset<T> Request<T>(string path, AssetRequestMode mode = AssetRequestMode.ImmediateLoad) where T : class
        {
            if (string.IsNullOrEmpty(path)) return Asset<T>.Empty;

            string cleanPath = path.Replace('\\', '/');
            int slashIdx = cleanPath.IndexOf('/');
            if (slashIdx > 0)
            {
                string modName = cleanPath.Substring(0, slashIdx);
                string assetName = cleanPath.Substring(slashIdx + 1);
                if (_mods.TryGetValue(modName, out Mod mod) && mod.Assets != null)
                {
                    return mod.Assets.Request<T>(assetName, mode);
                }
            }
            return Asset<T>.Empty;
        }
    }
}
