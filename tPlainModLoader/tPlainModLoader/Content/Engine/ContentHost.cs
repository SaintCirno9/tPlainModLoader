using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TPML.Content.Engine;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 内容模组统一生命周期宿主。
    /// 旧 tContentPatch 加载器负责调用 Host，内容模组不再需要手工拼接
    /// ContentHookDispatcher / ModContent / RecipeLoader 流程。
    /// </summary>
    public static class ContentHost
    {
        private static readonly ILogger Logger = LogManager.GetLogger("ContentHost");
        private static readonly List<Mod> _mods = new List<Mod>();
        private static readonly HashSet<string> _knownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;

        /// <summary>
        /// 幂等初始化内容引擎。允许在旧模组 Load 之前调用。
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            MonoModHooks.Initialize();
            ContentHookDispatcher.Initialize();
            PlayerLoader.InitializeHooks();
            ItemLoader.InitializeHooks();
            NPCLoader.InitializeHooks();
            ProjectileLoader.InitializeHooks();
            _initialized = true;
        }

        /// <summary>
        /// 注册一个内容模组，并立即触发其 Load。
        /// 同程序集、同名内容模组不会重复注册。
        /// </summary>
        public static Mod Register(Mod mod)
        {
            if (mod == null) return null;

            Initialize();

            string key = BuildKey(mod.Name);
            if (_knownNames.Contains(key) || (!(mod is AssemblyWrapperMod) && _mods.Any(existing => existing.GetType() == mod.GetType())))
            {
                return _mods.FirstOrDefault(existing => string.Equals(existing.Name, mod.Name, StringComparison.OrdinalIgnoreCase)) ??
                    _mods.FirstOrDefault(existing => !(existing is AssemblyWrapperMod) && existing.GetType() == mod.GetType()) ?? mod;
            }

            _knownNames.Add(key);
            _mods.Add(mod);
            ModContent.RegisterMod(mod);

            try
            {
                mod.Load();
            }
            catch (Exception ex)
            {
                Logger.Error($"内容模组 Load 失败: {mod.Name}", ex);
            }

            // 自动扫描程序集中的所有 ILoadable 内容并注册（如 ModPlayer, ModSystem, ModItem）
            if (mod.Code != null)
            {
                Type[] types = null;
                try
                {
                    types = mod.Code.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"模组 [{mod.Name}] 扫描内容类型异常: {ex.Message}");
                }

                if (types != null)
                {
                    foreach (var type in types)
                    {
                        if (type.IsClass && !type.IsAbstract && typeof(ILoadable).IsAssignableFrom(type) && !typeof(Mod).IsAssignableFrom(type))
                        {
                            try
                            {
                                var loadable = (ILoadable)Activator.CreateInstance(type, true);
                                mod.AddContent(loadable);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"模组 [{mod.Name}] 注册内容实例 [{type.FullName}] 失败", ex);
                            }
                        }
                    }
                }
            }

            return mod;
        }

        /// <summary>
        /// 扫描程序集，注册其中所有非抽象 TPML.Content.Mod 内容模组。
        /// 等价于旧引擎每模组入口的手工双注册胶水。
        /// </summary>
        public static List<Mod> RegisterFromAssembly(Assembly assembly)
        {
            var registered = new List<Mod>();
            if (assembly == null) return registered;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logger.Warn($"读取程序集类型不完整: {assembly.FullName}");
                types = ex.Types.Where(t => t != null).ToArray();
            }
            catch (Exception ex)
            {
                Logger.Warn($"读取程序集类型失败: {assembly.FullName}: {ex.Message}");
                return registered;
            }

            foreach (Type type in types)
            {
                if (type.IsClass == false || type.IsAbstract) continue;
                if (typeof(Mod).IsAssignableFrom(type) == false) continue;
                if (_mods.Any(existing => existing.GetType() == type)) continue;

                try
                {
                    Mod mod = (Mod)Activator.CreateInstance(type, true);
                    registered.Add(Register(mod));
                }
                catch (Exception ex)
                {
                    Logger.Error($"内容模组实例化失败: {type.FullName}", ex);
                }
            }

            // 若程序集内未显式继承 TPML.Content.Mod，但包含 ILoadable（如 ModPlayer/ModSystem/ModItem），自动创建承载 Mod
            if (registered.Count == 0)
            {
                bool hasLoadables = false;
                foreach (Type type in types)
                {
                    if (type.IsClass && !type.IsAbstract && typeof(ILoadable).IsAssignableFrom(type) && !typeof(Mod).IsAssignableFrom(type))
                    {
                        hasLoadables = true;
                        break;
                    }
                }

                if (hasLoadables)
                {
                    string modName = assembly.GetName().Name;
                    var autoMod = new AssemblyWrapperMod(modName, assembly);
                    registered.Add(Register(autoMod));
                    Logger.Info($"[ContentHost] 成功为程序集 [{modName}] 注册自动包装模组并扫描内容");
                }
            }

            return registered;
        }

        /// <summary>
        /// 自动包装程序集为轻量 Mod 容器
        /// </summary>
        internal sealed class AssemblyWrapperMod : Mod
        {
            public AssemblyWrapperMod(string name, Assembly assembly)
            {
                Name = name;
                DisplayName = name;
                Logger = LogManager.GetLogger(name);
                Code = assembly;
                Assets = new Assets.ModAssetRepository(this);
            }
        }

        /// <summary>
        /// 查找已注册的内容模组实例。
        /// </summary>
        public static T Find<T>() where T : Mod
        {
            return _mods.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// 注册指定程序集并返回首个内容模组，供旧入口保持静态快捷访问。
        /// </summary>
        public static Mod RegisterFirstFromAssembly(Assembly assembly)
        {
            return RegisterFromAssembly(assembly).FirstOrDefault();
        }

        /// <summary>
        /// 所有内容加载完成后统一构建并注入配方。
        /// 调用方应在所有旧模组 Load 阶段完成后调用。
        /// </summary>
        public static void CompleteLoading()
        {
            try
            {
                RecipeLoader.SetupRecipes();
            }
            catch (Exception ex)
            {
                Logger.Error("配方统一构建失败", ex);
            }
        }

        /// <summary>
        /// 逆序卸载全部内容模组，并清理内容引擎静态注册。
        /// 供旧 tContentPatch 卸载流程调用。
        /// </summary>
        public static void UnloadAll()
        {
            for (int i = _mods.Count - 1; i >= 0; i--)
            {
                Mod mod = _mods[i];
                try
                {
                    mod.Unload();
                }
                catch (Exception ex)
                {
                    Logger.Error($"内容模组卸载失败: {mod?.Name}", ex);
                }
            }

            _mods.Clear();
            _knownNames.Clear();

            try
            {
                MonoModHooks.Clear();
                ContentHookDispatcher.Clear();
                RecipeLoader.Clear();
                ModPlayerExtensions.ClearInstances();
                ItemLoader.Clear();
                NPCLoader.Clear();
                ProjectileLoader.Clear();
                BuffLoader.Clear();
                KeybindLoader.Clear();
                TileLoader.Clear();
                TileEntityLoader.Clear();
                ModContent.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error("内容引擎清理失败", ex);
            }

            _initialized = false;
        }

        private static string BuildKey(string name)
        {
            return string.IsNullOrEmpty(name) ? Guid.NewGuid().ToString("N") : name;
        }
    }
}
