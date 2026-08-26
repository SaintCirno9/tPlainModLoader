using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TPML.Core.Logging.Sinks;

namespace TPML.Core.Logging
{
    /// <summary>
    /// TPML 全局日志中心与管理工厂
    /// </summary>
    public static class LogManager
    {
        private static readonly object SyncLock = new object();
        private static readonly List<ILogSink> Sinks = new List<ILogSink>();
        private static readonly ConcurrentDictionary<string, ILogger> Loggers = new ConcurrentDictionary<string, ILogger>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;

        /// <summary>
        /// 全局最低日志过滤级别（默认 Info）
        /// </summary>
        public static LogLevel GlobalMinimumLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// 核心 Logger 实例
        /// </summary>
        public static ILogger CoreLogger => GetLogger("TPML");

        static LogManager()
        {
            // 默认挂载控制台 Sink，确保在未显式 Initialize 前也能看到控制台输出
            Sinks.Add(new ConsoleLogSink());
        }

        /// <summary>
        /// 初始化日志管理中心，配置文件异步落盘并注册崩溃防护钩子
        /// </summary>
        public static void Initialize(string logDirectory, string logFileName = "tpml.log", LogLevel minimumLevel = LogLevel.Info)
        {
            lock (SyncLock)
            {
                if (_initialized) return;
                _initialized = true;

                GlobalMinimumLevel = minimumLevel;

                try
                {
                    AsyncFileLogSink fileSink = new AsyncFileLogSink(logDirectory, logFileName);
                    Sinks.Add(fileSink);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LogManager] 初始化文件日志 Sink 失败: {ex.Message}");
                }

                // 注册全局异常与退出钩子
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    Exception ex = args.ExceptionObject as Exception;
                    CoreLogger.Fatal($"[AppDomain UnhandledException] 发生未捕获异常，即将崩溃退出: {ex?.Message}", ex);
                    Flush();
                };

                AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
                {
                    Shutdown();
                };
            }
        }

        /// <summary>
        /// 获取指定名称的强类型 Logger
        /// </summary>
        public static ILogger GetLogger(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "TPML";

            return Loggers.GetOrAdd(name, n => new Logger(n));
        }

        /// <summary>
        /// 获取指定类型的 Logger（以类名为 LoggerName）
        /// </summary>
        public static ILogger GetLogger<T>() => GetLogger(typeof(T).Name);

        /// <summary>
        /// 获取指定类型的 Logger（以类名为 LoggerName）
        /// </summary>
        public static ILogger GetLogger(Type type) => GetLogger(type?.Name ?? "Unknown");

        /// <summary>
        /// 添加自定义日志输出端
        /// </summary>
        public static void AddSink(ILogSink sink)
        {
            if (sink == null) return;
            lock (SyncLock)
            {
                if (!Sinks.Contains(sink))
                {
                    Sinks.Add(sink);
                }
            }
        }

        /// <summary>
        /// 分发一条日志至所有已注册的 Sink
        /// </summary>
        internal static void Emit(LogEntry entry)
        {
            if (entry == null) return;

            lock (SyncLock)
            {
                for (int i = 0; i < Sinks.Count; i++)
                {
                    try
                    {
                        Sinks[i].Emit(entry);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 强制所有 Sink 刷盘
        /// </summary>
        public static void Flush()
        {
            lock (SyncLock)
            {
                for (int i = 0; i < Sinks.Count; i++)
                {
                    try
                    {
                        Sinks[i].Flush();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 优雅释放日志中心资源
        /// </summary>
        public static void Shutdown()
        {
            lock (SyncLock)
            {
                Flush();
                for (int i = 0; i < Sinks.Count; i++)
                {
                    try
                    {
                        Sinks[i].Dispose();
                    }
                    catch { }
                }
                Sinks.Clear();
            }
        }
    }
}
