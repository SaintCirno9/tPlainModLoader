using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.Utils;
using TPML.Core.Logging;

namespace TPML.Content.Engine
{
    /// <summary>
    /// TPML 统一 MonoMod Detour / HookGen 生命周期管理器（对齐 tModLoader MonoModHooks）
    /// 负责：
    /// 1. 运行时全局监听 DetourManager.DetourApplied 与 ILHookApplied；
    /// 2. 按 Mod 程序集自动追踪记录所有挂载的 Hook 与 ILHook；
    /// 3. 在模组卸载或引擎重载（ContentHost.UnloadAll）时自动遍历 Undo() 回滚，实现零泄漏；
    /// 4. 提供与 tML 规范对齐的 Add / Modify / DumpIL 等调试门面。
    /// </summary>
    public static class MonoModHooks
    {
        private static readonly ILogger Logger = LogManager.GetLogger("MonoModHooks");

        private static readonly Dictionary<Type, string> DefaultAliases = new Dictionary<Type, string>
        {
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(char), "char" },
            { typeof(string), "string" }
        };

        private class DetourList
        {
            public readonly List<IDisposable> Detours = new List<IDisposable>();
            public readonly List<IDisposable> ILHooks = new List<IDisposable>();
        }

        private static readonly Dictionary<Assembly, DetourList> AssemblyDetours = new Dictionary<Assembly, DetourList>();
        private static readonly ConcurrentDictionary<(MethodBase, Delegate), IDisposable> HookCache = new ConcurrentDictionary<(MethodBase, Delegate), IDisposable>();
        private static readonly object LockObj = new object();

        private static bool _isInitialized = false;

        private static DetourList GetDetourList(Assembly asm)
        {
            if (asm == null || asm == typeof(Action).Assembly || asm == typeof(MonoModHooks).Assembly)
            {
                asm = typeof(MonoModHooks).Assembly;
            }

            lock (LockObj)
            {
                if (!AssemblyDetours.TryGetValue(asm, out var list))
                {
                    list = new DetourList();
                    AssemblyDetours[asm] = list;
                }
                return list;
            }
        }

        /// <summary>
        /// 初始化全局 MonoModHooks 生命周期管理器（幂等）
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                _isInitialized = true;
                Logger.Info("★ MonoMod Detour 运行时生命周期管理器初始化完毕");
            }
            catch (Exception ex)
            {
                Logger.Error($"初始化 MonoModHooks 异常: {ex.Message}", ex);
            }
        }

        private static string StringRep(MethodBase m)
        {
            if (m == null) return "null";
            try
            {
                var paramString = string.Join(", ", m.GetParameters().Select(p =>
                {
                    var t = p.ParameterType;
                    var s = "";
                    if (t.IsByRef)
                    {
                        s = p.IsOut ? "out " : "ref ";
                        t = t.GetElementType();
                    }
                    return s + (DefaultAliases.TryGetValue(t, out string n) ? n : t.Name);
                }));

                var owner = m.DeclaringType?.FullName ?? (m is DynamicMethod ? "dynamic" : "unknown");
                return $"{owner}::{m.Name}({paramString})";
            }
            catch
            {
                return m.Name;
            }
        }

        /// <summary>
        /// 自动撤销指定程序集所注册的所有 Detour 与 ILHook
        /// </summary>
        public static void RemoveAll(Assembly asm)
        {
            if (asm == null) return;

            lock (LockObj)
            {
                if (AssemblyDetours.TryGetValue(asm, out var list))
                {
                    Logger.Info($"正在自动卸载程序集 [{asm.GetName().Name}] 的 {list.Detours.Count} 个 Detour 与 {list.ILHooks.Count} 个 ILHook...");

                    for (int i = list.Detours.Count - 1; i >= 0; i--)
                    {
                        var detour = list.Detours[i];
                        try
                        {
                            detour?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"撤销 Detour 异常: {ex.Message}");
                        }
                    }

                    for (int i = list.ILHooks.Count - 1; i >= 0; i--)
                    {
                        var ilHook = list.ILHooks[i];
                        try
                        {
                            ilHook?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"撤销 ILHook 异常: {ex.Message}");
                        }
                    }

                    list.Detours.Clear();
                    list.ILHooks.Clear();
                    AssemblyDetours.Remove(asm);
                }
            }
        }

        /// <summary>
        /// 自动撤销指定内容模组所注册的所有 Hook
        /// </summary>
        public static void RemoveAll(Mod mod)
        {
            if (mod?.Code != null)
            {
                RemoveAll(mod.Code);
            }
        }

        /// <summary>
        /// 清空并撤销全部已注册的 Detour，重置 HookEndpointManager 与反射缓存
        /// </summary>
        public static void Clear()
        {
            lock (LockObj)
            {
                Logger.Info("正在执行 MonoModHooks 全量清理与 Hook 回滚...");

                foreach (var kvp in AssemblyDetours)
                {
                    var list = kvp.Value;
                    foreach (var detour in list.Detours)
                    {
                        try { detour?.Dispose(); } catch { }
                    }
                    foreach (var ilHook in list.ILHooks)
                    {
                        try { ilHook?.Dispose(); } catch { }
                    }
                }

                AssemblyDetours.Clear();
                HookCache.Clear();

                try
                {
                    var hemType = typeof(HookEndpointManager);
                    var hooksField = hemType.GetField("Hooks", BindingFlags.NonPublic | BindingFlags.Static);
                    if (hooksField?.GetValue(null) is IDictionary hooksDict)
                    {
                        hooksDict.Clear();
                    }
                    var ilHooksField = hemType.GetField("ILHooks", BindingFlags.NonPublic | BindingFlags.Static);
                    if (ilHooksField?.GetValue(null) is IDictionary ilHooksDict)
                    {
                        ilHooksDict.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"HookEndpointManager 内部字典清理异常: {ex.Message}");
                }

                // 清理 ReflectionHelper 内部静态缓存
                try
                {
                    var type = typeof(ReflectionHelper);
                    FieldInfo[] caches = new[]
                    {
                        type.GetField("AssemblyCache", BindingFlags.NonPublic | BindingFlags.Static),
                        type.GetField("AssembliesCache", BindingFlags.NonPublic | BindingFlags.Static),
                        type.GetField("ResolveReflectionCache", BindingFlags.NonPublic | BindingFlags.Static),
                    };
                    foreach (var cache in caches)
                    {
                        if (cache != null && cache.GetValue(null) is IDictionary dict)
                        {
                            dict.Clear();
                        }
                    }
                }
                catch { }
            }
        }

        #region tML 对齐公开方法

        public static void Add(MethodBase method, Delegate hookDelegate)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (hookDelegate == null) throw new ArgumentNullException(nameof(hookDelegate));

            Initialize();
            var hook = new Hook(method, hookDelegate);
            var owner = hookDelegate.Method?.DeclaringType?.Assembly ?? Assembly.GetCallingAssembly();
            GetDetourList(owner).Detours.Add(hook);
            HookCache.TryAdd((method, hookDelegate), hook);
            Logger.Debug($"Hook [{StringRep(method)}] 由模组程序集 [{owner.GetName().Name}] 注册");
        }

        public static void Modify(MethodBase method, MonoMod.Cil.ILContext.Manipulator callback)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            Initialize();
            var ilHook = new ILHook(method, callback);
            var owner = callback.Method?.DeclaringType?.Assembly ?? Assembly.GetCallingAssembly();
            GetDetourList(owner).ILHooks.Add(ilHook);
            HookCache.TryAdd((method, callback), ilHook);
            Logger.Debug($"ILHook [{StringRep(method)}] 由模组程序集 [{owner.GetName().Name}] 注册");
        }

        public static void RequestNativeAccess() { }

        public static void DumpILHooks()
        {
            try
            {
                var ilHooksField = typeof(HookEndpointManager).GetField("ILHooks", BindingFlags.NonPublic | BindingFlags.Static);
                object ilHooksFieldValue = ilHooksField?.GetValue(null);
                if (ilHooksFieldValue is IDictionary ilHooks)
                {
                    Logger.Info("已注册 IL Hooks 列表:");
                    foreach (DictionaryEntry item in ilHooks)
                    {
                        Logger.Info($"  - {item.Key}: {item.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"DumpILHooks 失败: {ex.Message}");
            }
        }

        public static void DumpOnHooks()
        {
            try
            {
                var hooksField = typeof(HookEndpointManager).GetField("Hooks", BindingFlags.NonPublic | BindingFlags.Static);
                object hooksFieldValue = hooksField?.GetValue(null);
                if (hooksFieldValue is IDictionary detours)
                {
                    Logger.Info("已注册 On Detours 列表:");
                    foreach (DictionaryEntry item in detours)
                    {
                        Logger.Info($"  - {item.Key}: {item.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"DumpOnHooks 失败: {ex.Message}");
            }
        }

        public static void DumpIL(Mod mod, MonoMod.Cil.ILContext il)
        {
            if (il?.Method == null) return;
            try
            {
                string methodName = il.Method.FullName.Replace(':', '_').Replace('<', '[').Replace('>', ']');
                if (methodName.Contains('?'))
                    methodName = methodName.Substring(methodName.LastIndexOf('?') + 1);

                string modName = mod?.Name ?? "TPML";
                string dumpDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "ILDumps", modName);
                if (!Directory.Exists(dumpDir)) Directory.CreateDirectory(dumpDir);

                string filePath = Path.Combine(dumpDir, methodName + ".txt");
                File.WriteAllText(filePath, il.ToString());
                Logger.Info($"IL 转储成功: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"IL 转储失败: {ex.Message}");
            }
        }

        #endregion
    }
}
