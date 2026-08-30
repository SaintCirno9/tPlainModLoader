using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace TPML.Content.Engine
{
    /// <summary>
    /// TPML MonoMod 钩子注册表：集中持有引擎与框架层 detour，支持统一反注册（对应原 Harmony UnpatchAll）。
    /// 规则：钩子必须在目标方法调用方被 JIT 之前应用（M1 spike 结论：JIT 内联会绕过 detour）。
    /// </summary>
    public static class HookRegistry
    {
        private static readonly List<IDisposable> _hooks = new List<IDisposable>();

        public static int Count => _hooks.Count;

        /// <summary>
        /// 注册一个 On 风格 detour 并立即生效。detour 委托形状：(orig, 方法参数...)。
        /// </summary>
        public static IDisposable Add(MethodBase target, Delegate detour)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (detour == null) throw new ArgumentNullException(nameof(detour));

            var hook = new Hook(target, detour);
            _hooks.Add(hook);
            return hook;
        }

        /// <summary>
        /// 逆序反注册全部钩子（与 detour 栈语义一致）。
        /// </summary>
        public static void ClearAll()
        {
            for (int i = _hooks.Count - 1; i >= 0; i--)
            {
                try
                {
                    _hooks[i].Dispose();
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
