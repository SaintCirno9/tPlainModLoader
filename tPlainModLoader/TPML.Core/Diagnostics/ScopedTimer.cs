using System;
using System.Diagnostics;
using TPML.Core.Logging;

namespace TPML.Core.Diagnostics
{
    /// <summary>
    /// 基于 using 语法的轻量级作用域耗时剖析器
    /// </summary>
    public sealed class ScopedTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly ILogger _logger;
        private readonly LogLevel _level;
        private readonly Stopwatch _stopwatch;

        public ScopedTimer(string operationName, ILogger logger = null, LogLevel level = LogLevel.Debug)
        {
            _operationName = operationName;
            _logger = logger ?? LogManager.CoreLogger;
            _level = level;
            _stopwatch = Stopwatch.StartNew();
        }

        public static ScopedTimer Profile(ILogger logger, string operationName, float warnThresholdMs = 0f)
        {
            return new ScopedTimer(operationName, logger, LogLevel.Debug);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            long ticks = _stopwatch.ElapsedTicks;
            if (PerformanceProfiler.IsEnabled)
            {
                PerformanceProfiler.Record("ScopedTimer", _operationName, ticks);
            }
            _logger.Log(_level, $"{_operationName} 完成，耗时: {_stopwatch.ElapsedMilliseconds}ms ({ticks} ticks)");
        }
    }
}
