using System;
using System.Collections.Generic;
using System.Reflection;
using TPML.Core.Logging;

namespace OptimizeAndTool.Utils
{
    /// <summary>
    /// 单个钩子门控生命周期注册元数据
    /// 作者: SaintCirno9
    /// </summary>
    public sealed class HookRegistration
    {
        /// <summary>门控标识名称</summary>
        public string Name { get; }

        /// <summary>门控目标类型（若是通过反射类型注册）</summary>
        public Type TargetType { get; }

        /// <summary>注册/激活委托</summary>
        public Action RegisterAction { get; }

        /// <summary>注销/卸载委托</summary>
        public Action UnregisterAction { get; }

        /// <summary>当前门控是否处于激活状态</summary>
        public bool IsActive { get; internal set; }

        public HookRegistration(string name, Action registerAction, Action unregisterAction, Type targetType = null)
        {
            Name = name ?? targetType?.Name ?? "UnknownHook";
            RegisterAction = registerAction ?? throw new ArgumentNullException(nameof(registerAction));
            UnregisterAction = unregisterAction ?? throw new ArgumentNullException(nameof(unregisterAction));
            TargetType = targetType;
        }
    }

    /// <summary>
    /// 统一 Hook 生命周期门控管理器
    /// 负责集中声明、按序激活 (FIFO) 与严格逆序注销 (LIFO) 所有 MonoMod / HookGen 钩子门控，
    /// 并提供异常隔离保护与日志记录，消除双向硬编码维护的遗漏与脆弱性。
    /// 作者: SaintCirno9
    /// </summary>
    public static class HookLifecycleRegistry
    {
        private static readonly ILogger _logger = LogManager.GetLogger("OptimizeAndTool.HookLifecycle");
        private static readonly List<HookRegistration> _registrations = new List<HookRegistration>();
        private static readonly object _lock = new object();
        private static bool _isRegistered = false;

        /// <summary>已注册门控数量</summary>
        public static int Count
        {
            get
            {
                lock (_lock) return _registrations.Count;
            }
        }

        /// <summary>全局门控是否已被激活</summary>
        public static bool IsRegistered
        {
            get
            {
                lock (_lock) return _isRegistered;
            }
        }

        /// <summary>
        /// 通过类型自省注册 Hook 门控（查找静态无参 RegisterAll 与 UnregisterAll 方法并转为委托缓存）
        /// </summary>
        /// <param name="type">包含静态 RegisterAll 与 UnregisterAll 的门控类型</param>
        public static void RegisterType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            lock (_lock)
            {
                for (int i = 0; i < _registrations.Count; i++)
                {
                    if (_registrations[i].TargetType == type)
                    {
                        return; // 幂等保护，已注册则跳过
                    }
                }

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                var regMethod = type.GetMethod("RegisterAll", flags, null, Type.EmptyTypes, null);
                var unregMethod = type.GetMethod("UnregisterAll", flags, null, Type.EmptyTypes, null);

                if (regMethod == null)
                {
                    throw new InvalidOperationException($"[HookLifecycle] 门控类型 {type.FullName} 缺少静态 RegisterAll() 方法");
                }
                if (unregMethod == null)
                {
                    throw new InvalidOperationException($"[HookLifecycle] 门控类型 {type.FullName} 缺少静态 UnregisterAll() 方法");
                }

                Action regAction = (Action)Delegate.CreateDelegate(typeof(Action), regMethod);
                Action unregAction = (Action)Delegate.CreateDelegate(typeof(Action), unregMethod);

                _registrations.Add(new HookRegistration(type.Name, regAction, unregAction, type));
            }
        }

        /// <summary>
        /// 批量通过类型注册 Hook 门控
        /// </summary>
        public static void RegisterTypes(params Type[] types)
        {
            if (types == null) return;
            foreach (var type in types)
            {
                if (type != null)
                {
                    RegisterType(type);
                }
            }
        }

        /// <summary>
        /// 泛型类型注册（仅适用于非静态类型）
        /// </summary>
        public static void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }

        /// <summary>
        /// 显式注册自定义委托对
        /// </summary>
        /// <param name="register">注册/激活回调</param>
        /// <param name="unregister">注销/卸载回调</param>
        /// <param name="name">门控名称描述</param>
        public static void Register(Action register, Action unregister, string name = null)
        {
            if (register == null) throw new ArgumentNullException(nameof(register));
            if (unregister == null) throw new ArgumentNullException(nameof(unregister));

            lock (_lock)
            {
                _registrations.Add(new HookRegistration(name, register, unregister));
            }
        }

        /// <summary>
        /// 正序（FIFO）激活并注册全部 Hook 门控
        /// 单个钩子注册异常进行独立捕获与日志记录，隔离故障不阻断其他钩子
        /// </summary>
        public static void RegisterAll()
        {
            List<HookRegistration> snapshot;
            lock (_lock)
            {
                if (_isRegistered)
                {
                    _logger.Warn("[HookLifecycle] RegisterAll() 已经被调用，跳过重复注册");
                    return;
                }
                snapshot = new List<HookRegistration>(_registrations);
                _isRegistered = true;
            }

            _logger.Info($"[HookLifecycle] 开始正序激活全部钩子门控 (共 {snapshot.Count} 个)...");
            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < snapshot.Count; i++)
            {
                var hook = snapshot[i];
                try
                {
                    hook.RegisterAction();
                    hook.IsActive = true;
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.Error($"[HookLifecycle] 激活钩子门控 [{hook.Name}] 发生异常", ex);
                }
            }

            _logger.Info($"[HookLifecycle] 全部钩子门控激活完成: 成功 {successCount} 个, 失败 {failCount} 个");
        }

        /// <summary>
        /// 逆序（LIFO）反注册并卸载全部 Hook 门控
        /// 严格保证依赖拓扑反向卸载，单个异常独立捕获，确保所有门控都能执行卸载
        /// </summary>
        public static void UnregisterAll()
        {
            List<HookRegistration> snapshot;
            lock (_lock)
            {
                if (!_isRegistered && _registrations.Count == 0)
                {
                    return;
                }
                snapshot = new List<HookRegistration>(_registrations);
                _isRegistered = false;
            }

            _logger.Info($"[HookLifecycle] 开始逆序(LIFO)注销全部钩子门控 (共 {snapshot.Count} 个)...");
            int successCount = 0;
            int failCount = 0;

            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                var hook = snapshot[i];
                try
                {
                    hook.UnregisterAction();
                    hook.IsActive = false;
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.Error($"[HookLifecycle] 注销钩子门控 [{hook.Name}] 发生异常", ex);
                }
            }

            _logger.Info($"[HookLifecycle] 全部钩子门控卸载完成: 成功 {successCount} 个, 失败 {failCount} 个");
        }

        /// <summary>
        /// 清空所有注册元数据（通常仅用于重载或重置）
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _registrations.Clear();
                _isRegistered = false;
            }
        }
    }
}
