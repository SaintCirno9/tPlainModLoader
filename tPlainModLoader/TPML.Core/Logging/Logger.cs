using System;

namespace TPML.Core.Logging
{
    /// <summary>
    /// 标准 ILogger 实现类
    /// </summary>
    public class Logger : ILogger
    {
        public string Name { get; }
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        public Logger(string name)
        {
            Name = string.IsNullOrEmpty(name) ? "TPML" : name;
        }

        public bool IsEnabled(LogLevel level)
        {
            if (level < LogManager.GlobalMinimumLevel)
                return false;

            if (level < MinimumLevel)
                return false;

            return true;
        }

        public void Trace(object message) => Log(LogLevel.Trace, message?.ToString());
        public void Trace(string message) => Log(LogLevel.Trace, message);
        public void Trace(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Trace))
                Log(LogLevel.Trace, string.Format(format, args));
        }

        public void Debug(object message) => Log(LogLevel.Debug, message?.ToString());
        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Debug(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Debug))
                Log(LogLevel.Debug, string.Format(format, args));
        }

        public void Info(object message) => Log(LogLevel.Info, message?.ToString());
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Info(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Info))
                Log(LogLevel.Info, string.Format(format, args));
        }

        public void Warn(object message) => Log(LogLevel.Warn, message?.ToString());
        public void Warn(string message) => Log(LogLevel.Warn, message);
        public void Warn(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Warn))
                Log(LogLevel.Warn, string.Format(format, args));
        }

        public void Error(object message) => Log(LogLevel.Error, message?.ToString());
        public void Error(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public void Error(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Error))
                Log(LogLevel.Error, string.Format(format, args));
        }

        public void Fatal(object message) => Log(LogLevel.Fatal, message?.ToString());
        public void Fatal(string message, Exception exception = null) => Log(LogLevel.Fatal, message, exception);
        public void Fatal(string format, params object[] args)
        {
            if (IsEnabled(LogLevel.Fatal))
                Log(LogLevel.Fatal, string.Format(format, args));
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (!IsEnabled(level))
                return;

            LogManager.Emit(new LogEntry(level, Name, message, exception));
        }
    }
}
