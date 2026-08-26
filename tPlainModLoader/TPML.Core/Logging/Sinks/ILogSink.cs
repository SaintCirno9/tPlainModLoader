using System;

namespace TPML.Core.Logging.Sinks
{
    /// <summary>
    /// 日志输出目标接口
    /// </summary>
    public interface ILogSink : IDisposable
    {
        /// <summary>
        /// 输出单条日志
        /// </summary>
        void Emit(LogEntry entry);

        /// <summary>
        /// 强制刷盘/清空缓冲区
        /// </summary>
        void Flush();
    }
}
