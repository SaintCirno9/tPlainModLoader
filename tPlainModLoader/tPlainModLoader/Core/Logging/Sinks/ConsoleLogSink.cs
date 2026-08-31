using System;

namespace TPML.Core.Logging.Sinks
{
    /// <summary>
    /// 控制台彩色日志输出端
    /// </summary>
    public class ConsoleLogSink : ILogSink
    {
        private static readonly object ConsoleLock = new object();

        public void Emit(LogEntry entry)
        {
            if (entry == null) return;

            string formatted = entry.Format();

            lock (ConsoleLock)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = GetColorForLevel(entry.Level);
                    Console.WriteLine(formatted);
                }
                catch (System.IO.IOException)
                {
                }
                finally
                {
                    try { Console.ForegroundColor = originalColor; } catch { }
                }
            }
        }

        public void Flush()
        {
            // 控制台无延迟缓冲
        }

        public void Dispose()
        {
        }

        private static ConsoleColor GetColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => ConsoleColor.DarkGray,
                LogLevel.Debug => ConsoleColor.DarkGray,
                LogLevel.Info => ConsoleColor.Gray,
                LogLevel.Warn => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray
            };
        }
    }
}
