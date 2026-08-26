using System;
using System.Text;

namespace TPML.Core.Logging
{
    /// <summary>
    /// 单条结构化日志实体
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; }
        public LogLevel Level { get; }
        public string LoggerName { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public int ThreadId { get; }

        public LogEntry(LogLevel level, string loggerName, string message, Exception exception = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            LoggerName = loggerName ?? "TPML";
            Message = message ?? string.Empty;
            Exception = exception;
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public string Format(bool includeThread = false)
        {
            string levelStr = Level switch
            {
                LogLevel.Trace => "TRACE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Info => "INFO",
                LogLevel.Warn => "WARN",
                LogLevel.Error => "ERROR",
                LogLevel.Fatal => "FATAL",
                _ => Level.ToString().ToUpperInvariant()
            };

            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(Timestamp.ToString("HH:mm:ss.fff"));
            sb.Append("] [");
            sb.Append(levelStr);
            sb.Append("] ");
            if (includeThread)
            {
                sb.Append("[T:");
                sb.Append(ThreadId);
                sb.Append("] ");
            }
            sb.Append('[');
            sb.Append(LoggerName);
            sb.Append("] ");
            sb.Append(Message);

            if (Exception != null)
            {
                sb.AppendLine();
                sb.Append(Exception);
            }

            return sb.ToString();
        }
    }
}
