using System;
using TPML.Core.Logging;

namespace TPML.Content.Logging
{
    /// <summary>
    /// 兼容层 ModLogger，封装并转发至 TPML.Core.Logging.ILogger
    /// </summary>
    public class ModLogger : ILogger
    {
        private readonly ILogger _innerLogger;

        public string Name => _innerLogger.Name;
        public LogLevel MinimumLevel
        {
            get => _innerLogger.MinimumLevel;
            set => _innerLogger.MinimumLevel = value;
        }

        public ModLogger(string name)
        {
            _innerLogger = LogManager.GetLogger(name);
        }

        public bool IsEnabled(LogLevel level) => _innerLogger.IsEnabled(level);

        public void Trace(object message) => _innerLogger.Trace(message);
        public void Trace(string message) => _innerLogger.Trace(message);
        public void Trace(string format, params object[] args) => _innerLogger.Trace(format, args);

        public void Debug(object message) => _innerLogger.Debug(message);
        public void Debug(string message) => _innerLogger.Debug(message);
        public void Debug(string format, params object[] args) => _innerLogger.Debug(format, args);

        public void Info(object message) => _innerLogger.Info(message);
        public void Info(string message) => _innerLogger.Info(message);
        public void Info(string format, params object[] args) => _innerLogger.Info(format, args);

        public void Warn(object message) => _innerLogger.Warn(message);
        public void Warn(string message) => _innerLogger.Warn(message);
        public void Warn(string format, params object[] args) => _innerLogger.Warn(format, args);

        public void Error(object message) => _innerLogger.Error(message);
        public void Error(string message, Exception exception = null) => _innerLogger.Error(message, exception);
        public void Error(string format, params object[] args) => _innerLogger.Error(format, args);

        public void Fatal(object message) => _innerLogger.Fatal(message);
        public void Fatal(string message, Exception exception = null) => _innerLogger.Fatal(message, exception);
        public void Fatal(string format, params object[] args) => _innerLogger.Fatal(format, args);

        public void Log(LogLevel level, string message, Exception exception = null) => _innerLogger.Log(level, message, exception);
    }
}
