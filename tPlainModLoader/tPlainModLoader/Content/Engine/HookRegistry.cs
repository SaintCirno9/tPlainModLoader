using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace TPML.Content.Engine
{
    public enum HookScope
    {
        Framework,
        Content
    }

    /// <summary>
    /// TPML MonoMod 钩子注册表：集中持有引擎与框架层 detour，支持统一反注册（对应原 Harmony UnpatchAll）。
    /// 规则：钩子必须在目标方法调用方被 JIT 之前应用（M1 spike 结论：JIT 内联会绕过 detour）。
    /// </summary>
    public static class HookRegistry
    {
        private sealed class RegisteredHook
        {
            public IDisposable Hook;
            public HookScope Scope;
        }

        private static readonly List<RegisteredHook> _hooks = new List<RegisteredHook>();

        public static int Count => _hooks.Count;

        /// <summary>
        /// 注册一个框架级 On 风格 detour 并立即生效。detour 委托形状：(orig, 方法参数...)。
        /// </summary>
        public static IDisposable Add(MethodBase target, Delegate detour)
        {
            return Add(target, detour, HookScope.Framework);
        }

        public static IDisposable AddFramework(MethodBase target, Delegate detour)
        {
            return Add(target, detour, HookScope.Framework);
        }

        public static IDisposable AddContent(MethodBase target, Delegate detour)
        {
            return Add(target, detour, HookScope.Content);
        }

        public static IDisposable Add(MethodBase target, Delegate detour, HookScope scope)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (detour == null) throw new ArgumentNullException(nameof(detour));

            try
            {
                var hook = new Hook(target, detour);
                _hooks.Add(new RegisteredHook { Hook = hook, Scope = scope });
                return hook;
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[HookRegistry] 应用 Detour Hook 失败 [{target.DeclaringType?.FullName}.{target.Name}]: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 按作用域逆序反注册钩子（与 detour 栈语义一致）。
        /// </summary>
        public static void Clear(HookScope scope)
        {
            for (int i = _hooks.Count - 1; i >= 0; i--)
            {
                if (_hooks[i].Scope != scope) continue;

                try
                {
                    _hooks[i].Hook.Dispose();
                }
                catch (Exception ex)
                {
                    ModLoader.Log($"[HookRegistry] 反注册异常: {ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    _hooks.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 逆序反注册全部钩子，仅供完整框架退出使用。
        /// </summary>
        public static void ClearAll()
        {
            for (int i = _hooks.Count - 1; i >= 0; i--)
            {
                try
                {
                    _hooks[i].Hook.Dispose();
                }
                catch (Exception ex)
                {
                    ModLoader.Log($"[HookRegistry] 反注册异常: {ex.GetType().Name}: {ex.Message}");
                }
            }
            _hooks.Clear();
        }
    }
}
