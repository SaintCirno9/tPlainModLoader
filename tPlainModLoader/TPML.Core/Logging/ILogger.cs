using System;

namespace TPML.Core.Logging
{
    /// <summary>
    /// 强类型通用日志接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 日志源名称（通常为模组名或组件名）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 当前 Logger 独立的最低日志等级过滤
        /// </summary>
        LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// 判定指定日志级别是否启用
        /// </summary>
        bool IsEnabled(LogLevel level);

        void Trace(object message);
        void Trace(string message);
        void Trace(string format, params object[] args);

        void Debug(object message);
        void Debug(string message);
        void Debug(string format, params object[] args);

        void Info(object message);
        void Info(string message);
        void Info(string format, params object[] args);

        void Warn(object message);
        void Warn(string message);
        void Warn(string format, params object[] args);

        void Error(object message);
        void Error(string message, Exception exception = null);
        void Error(string format, params object[] args);

        void Fatal(object message);
        void Fatal(string message, Exception exception = null);
        void Fatal(string format, params object[] args);

        void Log(LogLevel level, string message, Exception exception = null);
    }
}
