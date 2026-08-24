using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader.Container;
using Terraria.ModLoader.Localization;

namespace Terraria.ModLoader.Engine
{
    /// <summary>
    /// tModLoader 模组核心加载引擎
    /// </summary>
    public static class TModShimEngine
    {
        private static readonly List<Mod> _loadedMods = new List<Mod>();
        private static readonly Dictionary<string, Assembly> _dependencyAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        public static Action<string> LogCallback { get; set; } = msg => Console.WriteLine(msg);

        public static void Log(string message) => LogCallback?.Invoke(message);

        [ThreadStatic]
        private static bool _isResolving;

        static TModShimEngine()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (_isResolving) return null;
            _isResolving = true;
            try
            {
                string name = new AssemblyName(args.Name).Name;

                // 1. Terraria / TerrariaServer
                if (name == "Terraria" || name == "TerrariaServer")
                    return typeof(Main).Assembly;

                // 2. Terraria.ModLoader / tModLoader / TerrariaHooks
                if (name == "Terraria.ModLoader" || name == "tModLoader" || name == "TerrariaHooks")
                    return typeof(Mod).Assembly;

                // 3. FNA -> 映射至原版 Microsoft.Xna.Framework
                if (name == "FNA")
                    return typeof(Microsoft.Xna.Framework.Vector2).Assembly;

                // 4. .tmod 内部携带的依赖库 (如 lib/*.dll)
                if (_dependencyAssemblies.TryGetValue(name, out var asm))
                    return asm;

                // 5. .NET 8 / CoreCLR / netstandard 基础类型映射
                if (name == "System.Runtime" || name == "System.Private.CoreLib" || name == "netstandard" ||
                    name == "mscorlib" || name == "System.IO" || name == "System.Threading" ||
                    name == "System.Collections" || name == "System.Runtime.Extensions")
                {
                    return typeof(object).Assembly;
                }

                if (name == "System.Linq" || name == "System.Core")
                {
                    return typeof(System.Linq.Enumerable).Assembly;
                }

                return null;
            }
            finally
            {
                _isResolving = false;
            }
        }

        public static IReadOnlyList<Mod> LoadedMods => _loadedMods;

        public static Mod LoadTModContainer(TModFileContainer container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            Log($"[TModShimEngine] 开始载入模组: {container.ModName} v{container.ModVersion}");

            // 1. 本地化词条预注入
            foreach (var kvp in container.Files)
            {
                if (kvp.Key.StartsWith("Localization/", StringComparison.OrdinalIgnoreCase) &&
                    (kvp.Key.EndsWith(".hjson", StringComparison.OrdinalIgnoreCase) || kvp.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
                {
                    string text = System.Text.Encoding.UTF8.GetString(kvp.Value);
                    HjsonLocalizationInjector.InjectHjson(container.ModName, text);
                }
            }

            // 2. 预加载容器内携带的所有依赖 DLL (如 lib/*.dll)
            foreach (var kvp in container.Files)
            {
                if (kvp.Key.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    !kvp.Key.Equals($"{container.ModName}.dll", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        byte[] retargetedDep = AssemblyRetargeter.Retarget(kvp.Value, kvp.Key);
                        var depAsm = Assembly.Load(retargetedDep);
                        _dependencyAssemblies[depAsm.GetName().Name] = depAsm;
                    }
                    catch (Exception ex)
                    {
                        Log($"[TModShimEngine] 载入内嵌依赖库 {kvp.Key} 异常: {ex.Message}");
                    }
                }
            }

            // 3. 加载主程序集 (自动执行 IL 元数据重定向)
            byte[] asmBytes = container.MainAssemblyBytes;
            if (asmBytes == null)
            {
                Log($"[TModShimEngine] 模组 {container.ModName} 中未找到主程序集 DLL。");
                return null;
            }

            byte[] retargetedMain = AssemblyRetargeter.Retarget(asmBytes, container.ModName);
            Assembly modAsm = Assembly.Load(retargetedMain);

            // 4. 获取程序集内所有类型（容错处理与详细 LoaderExceptions 日志输出）
            Type[] asmTypes;
            try
            {
                asmTypes = modAsm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log($"[TModShimEngine] 扫描模组 {container.ModName} 类型时发生 ReflectionTypeLoadException:");
                foreach (var loaderEx in ex.LoaderExceptions)
                {
                    Log($"  - [LoaderException] {loaderEx?.Message}");
                }
                asmTypes = ex.Types.Where(t => t != null).ToArray();
            }

            // 5. 寻找 Mod 主入口类与 ILoadable
            Type modType = null;
            var loadableTypes = new List<Type>();

            foreach (Type t in asmTypes)
            {
                try
                {
                    if (t.IsAbstract || t.IsInterface) continue;

                    if (typeof(Mod).IsAssignableFrom(t) && modType == null)
                    {
                        modType = t;
                    }
                    else if (typeof(ILoadable).IsAssignableFrom(t))
                    {
                        loadableTypes.Add(t);
                    }
                }
                catch (Exception ex)
                {
                    Log($"[TModShimEngine] 分析类型 {t?.FullName} 失败: {ex.Message}");
                }
            }

            Mod modInstance;
            if (modType != null)
            {
                modInstance = (Mod)Activator.CreateInstance(modType, true);
            }
            else
            {
                // 自动生成通用 Mod 容器
                modInstance = new GenericMod(container.ModName);
            }

            modInstance.Name = container.ModName;
            modInstance.DisplayName = container.ModName;
            if (Version.TryParse(container.ModVersion, out var ver))
            {
                modInstance.Version = ver;
            }
            modInstance.Code = modAsm;

            // 填充文件数据
            foreach (var kvp in container.Files)
            {
                modInstance._fileData[kvp.Key] = kvp.Value;
            }

            ModContent.RegisterMod(modInstance);

            // 6. 调用 Mod.Load()
            try
            {
                modInstance.Load();
            }
            catch (Exception ex)
            {
                Log($"[TModShimEngine] 执行 {modInstance.Name}.Load() 异常: {ex}");
            }

            // 7. 实例化并加载所有 ILoadable
            var loadedInstances = new List<ILoadable>();
            foreach (Type t in loadableTypes)
            {
                try
                {
                    var loadable = (ILoadable)Activator.CreateInstance(t, true);
                    if (loadable.IsLoadingEnabled(modInstance))
                    {
                        loadable.Load(modInstance);
                        modInstance.AddContent(loadable);
                        loadedInstances.Add(loadable);
                    }
                }
                catch (Exception ex)
                {
                    Log($"[TModShimEngine] 实例化/加载内容类型 {t.FullName} 异常: {ex.Message}");
                }
            }

            // 8. 注册至钩子分发器并应用按需补丁
            TModHookDispatcher.RegisterHookInstances(loadedInstances);

            // 9. 调用 PostSetupContent()
            try
            {
                modInstance.PostSetupContent();
            }
            catch (Exception ex)
            {
                Log($"[TModShimEngine] 执行 {modInstance.Name}.PostSetupContent() 异常: {ex}");
            }

            _loadedMods.Add(modInstance);
            Log($"[TModShimEngine] 模组 [{modInstance.Name}] 加载完成 (注册了 {loadedInstances.Count} 个内容项)");
            return modInstance;
        }

        public static void UnloadAll()
        {
            Log("[TModShimEngine] 卸载所有 tModLoader Shim 模组...");

            for (int i = 0; i < _loadedMods.Count; i++)
            {
                try
                {
                    _loadedMods[i].Unload();
                }
                catch (Exception ex)
                {
                    Log($"[TModShimEngine] 卸载模组 {_loadedMods[i].Name} 异常: {ex}");
                }
            }

            _loadedMods.Clear();
            TModHookDispatcher.Clear();
            ModContent.Clear();
            KeybindLoader.Clear();
        }

        private class GenericMod : Mod
        {
            public GenericMod(string name)
            {
                Name = name;
                DisplayName = name;
            }
        }
    }
}
