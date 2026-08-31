using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TPML.Core.Logging;

namespace TPML.Core.Diagnostics
{
    /// <summary>
    /// TPML 原生高性能性能剖析与耗时诊断引擎
    /// </summary>
    public static class PerformanceProfiler
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Profiler");
        private static readonly ConcurrentDictionary<string, MetricEntry> _metrics = new ConcurrentDictionary<string, MetricEntry>(StringComparer.OrdinalIgnoreCase);

        private static Stopwatch _sessionStopwatch;
        private static float _durationSeconds = 0f;
        private static float _remainingSeconds = 0f;
        private static long _totalFrames = 0;
        private static readonly object _stateLock = new object();

        /// <summary>
        /// Profiler 是否处于活动采样状态。关闭状态下具有零开销快速路径。
        /// </summary>
        public static bool IsEnabled { get; private set; } = false;

        /// <summary>
        /// 当前采样会话已运行的秒数
        /// </summary>
        public static float ElapsedSeconds => _sessionStopwatch != null ? (float)_sessionStopwatch.Elapsed.TotalSeconds : 0f;

        /// <summary>
        /// 当前采样设定的总秒数（0 表示手动停止）
        /// </summary>
        public static float TargetDurationSeconds => _durationSeconds;

        /// <summary>
        /// 当前会话采样的总帧数
        /// </summary>
        public static long TotalFrames => _totalFrames;

        /// <summary>
        /// 状态变化事件：(bool isEnabled, float durationSeconds)
        /// </summary>
        public static event Action<bool, float> OnStateChanged;

        /// <summary>
        /// 报告生成事件：(string reportText)
        /// </summary>
        public static event Action<string> OnReportGenerated;

        /// <summary>
        /// 启动性能采样会话
        /// </summary>
        /// <param name="durationSeconds">采样持续秒数。0 或负数表示手动调用 StopAndReport 停止</param>
        public static void Start(float durationSeconds = 0f)
        {
            lock (_stateLock)
            {
                _metrics.Clear();
                _totalFrames = 0;
                _durationSeconds = Math.Max(0f, durationSeconds);
                _remainingSeconds = _durationSeconds;
                _sessionStopwatch = Stopwatch.StartNew();
                IsEnabled = true;
            }

            string modeDesc = durationSeconds > 0 ? $"{durationSeconds:0.##} 秒（自动停止）" : "持续运行（需手动停止）";
            Logger.Info($"[PerformanceProfiler] 性能采样已启动，计划时长: {modeDesc}");
            OnStateChanged?.Invoke(true, durationSeconds);
        }

        /// <summary>
        /// 停止采样并输出格式化性能报告
        /// </summary>
        /// <returns>生成的性能报告内容</returns>
        public static string StopAndReport()
        {
            string report;
            lock (_stateLock)
            {
                if (!IsEnabled && _sessionStopwatch == null)
                {
                    return "当前没有正在运行的性能采样会话。";
                }

                IsEnabled = false;
                _sessionStopwatch?.Stop();
                report = GenerateReportInternal();
                _sessionStopwatch = null;
            }

            Logger.Info("\n" + report);
            OnStateChanged?.Invoke(false, 0f);
            OnReportGenerated?.Invoke(report);
            return report;
        }

        /// <summary>
        /// 仅停止采样而不打印报告
        /// </summary>
        public static void Stop()
        {
            lock (_stateLock)
            {
                if (!IsEnabled) return;
                IsEnabled = false;
                _sessionStopwatch?.Stop();
            }
            Logger.Info("[PerformanceProfiler] 性能采样已停止。");
            OnStateChanged?.Invoke(false, 0f);
        }

        /// <summary>
        /// 由游戏主循环（如 Main.Update）每帧调用，更新计时器并在超时时自动生成报告
        /// </summary>
        /// <param name="deltaSeconds">当前帧间隔秒数</param>
        public static void Update(float deltaSeconds)
        {
            if (!IsEnabled) return;

            _totalFrames++;

            if (_durationSeconds > 0f)
            {
                _remainingSeconds -= deltaSeconds;
                if (_remainingSeconds <= 0f)
                {
                    StopAndReport();
                }
            }
        }

        /// <summary>
        /// 轻量级作用域计时测量。在 Profiler 关闭时为零开销（返回默认空结构体）。
        /// </summary>
        /// <param name="category">模块或模组名称</param>
        /// <param name="name">具体功能或方法标识</param>
        /// <returns>支持 using 语法的 ProfilerScope 结构体</returns>
        public static ProfilerScope Measure(string category, string name)
        {
            if (!IsEnabled)
                return default(ProfilerScope);

            return new ProfilerScope(category, name, Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// 记录单次耗时采样数据（纳秒/Ticks 级）
        /// </summary>
        public static void Record(string category, string name, long elapsedTicks)
        {
            if (!IsEnabled || elapsedTicks < 0) return;

            string key = category + "::" + name;
            MetricEntry entry = _metrics.GetOrAdd(key, k => new MetricEntry(category, name));
            entry.AddSample(elapsedTicks);
        }

        /// <summary>
        /// 生成当前已收集数据的性能报告快照
        /// </summary>
        public static string GenerateReport()
        {
            lock (_stateLock)
            {
                return GenerateReportInternal();
            }
        }

        private static string GenerateReportInternal()
        {
            double totalSessionMs = _sessionStopwatch != null ? _sessionStopwatch.Elapsed.TotalMilliseconds : 0.0;
            if (totalSessionMs <= 0.001) totalSessionMs = 0.001;

            double totalSessionSeconds = totalSessionMs / 1000.0;
            double avgFps = _totalFrames > 0 && totalSessionSeconds > 0 ? _totalFrames / totalSessionSeconds : 0.0;

            var list = _metrics.Values.ToList();
            list.Sort((a, b) => b.TotalTicks.CompareTo(a.TotalTicks));

            var sb = new StringBuilder();
            sb.AppendLine("======================================== [TPML 性能剖析报告] ========================================");
            sb.AppendLine($"采样时长: {totalSessionSeconds:F2}s | 采样总帧数: {_totalFrames} 帧 | 平均帧率: {avgFps:F1} FPS | 监控项数量: {list.Count}");
            sb.AppendLine("------------------------------------------------------------------------------------------------------");
            sb.AppendLine(string.Format("{0,-18} {1,-36} {2,10} {3,8} {4,10} {5,10} {6,9}",
                "模块/类别", "功能/方法名", "总耗时(ms)", "调用次数", "平均(ms)", "峰值(ms)", "帧占比(%)"));
            sb.AppendLine("------------------------------------------------------------------------------------------------------");

            if (list.Count == 0)
            {
                sb.AppendLine("  (在采样周期内未捕获到任何性能数据)");
            }
            else
            {
                double tickFrequency = Stopwatch.Frequency;
                foreach (var entry in list)
                {
                    double totalMs = (entry.TotalTicks * 1000.0) / tickFrequency;
                    double maxMs = (entry.MaxTicks * 1000.0) / tickFrequency;
                    long calls = entry.CallCount;
                    double avgMs = calls > 0 ? totalMs / calls : 0.0;
                    double percentage = (totalMs / totalSessionMs) * 100.0;

                    string cat = Truncate(entry.Category, 18);
                    string name = Truncate(entry.Name, 36);

                    sb.AppendLine(string.Format("{0,-18} {1,-36} {2,10:F2} {3,8} {4,10:F3} {5,10:F2} {6,8:F2}%",
                        cat, name, totalMs, calls, avgMs, maxMs, percentage));
                }
            }

            sb.AppendLine("======================================================================================================");
            return sb.ToString();
        }

        private static string Truncate(string str, int maxLen)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            if (str.Length <= maxLen) return str;
            return str.Substring(0, maxLen - 3) + "...";
        }
    }

    /// <summary>
    /// 轻量级无装箱零堆分配的作用域耗时结构体
    /// </summary>
    public readonly struct ProfilerScope : IDisposable
    {
        private readonly string _category;
        private readonly string _name;
        private readonly long _startTicks;
        private readonly bool _active;

        public ProfilerScope(string category, string name, long startTicks)
        {
            _category = category;
            _name = name;
            _startTicks = startTicks;
            _active = true;
        }

        public void Dispose()
        {
            if (!_active) return;
            long elapsed = Stopwatch.GetTimestamp() - _startTicks;
            PerformanceProfiler.Record(_category, _name, elapsed);
        }
    }

    /// <summary>
    /// 线程安全的指标聚合项
    /// </summary>
    public sealed class MetricEntry
    {
        public string Category { get; }
        public string Name { get; }

        private long _callCount;
        private long _totalTicks;
        private long _maxTicks;
        private long _minTicks = long.MaxValue;
        private readonly object _syncRoot = new object();

        public long CallCount => _callCount;
        public long TotalTicks => _totalTicks;
        public long MaxTicks => _maxTicks;
        public long MinTicks => _minTicks == long.MaxValue ? 0 : _minTicks;

        public MetricEntry(string category, string name)
        {
            Category = category;
            Name = name;
        }

        public void AddSample(long ticks)
        {
            lock (_syncRoot)
            {
                _callCount++;
                _totalTicks += ticks;
                if (ticks > _maxTicks) _maxTicks = ticks;
                if (ticks < _minTicks) _minTicks = ticks;
            }
        }
    }
}
