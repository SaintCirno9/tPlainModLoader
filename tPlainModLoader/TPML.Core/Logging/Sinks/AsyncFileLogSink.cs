using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace TPML.Core.Logging.Sinks
{
    /// <summary>
    /// 高性能异步批量落盘文件日志输出端
    /// </summary>
    public class AsyncFileLogSink : ILogSink
    {
        private readonly string _logPath;
        private readonly BlockingCollection<LogEntry> _queue = new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>());
        private readonly Thread _workerThread;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _syncLock = new object();
        private StreamWriter _writer;
        private volatile bool _disposed;

        public string LogPath => _logPath;

        public AsyncFileLogSink(string logDirectory, string logFileName = "tpml.log")
        {
            if (string.IsNullOrEmpty(logDirectory))
                logDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists(logDirectory))
            {
                try { Directory.CreateDirectory(logDirectory); } catch { }
            }

            _logPath = Path.Combine(logDirectory, logFileName);

            // 启动时滚动归档历史日志
            RollOldLog(_logPath);

            try
            {
                FileStream fs = new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                _writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = false };
                _writer.WriteLine($"=== TPML Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===");
                _writer.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TPML.Core] 无法创建日志文件 ({_logPath}): {ex.Message}");
            }

            _workerThread = new Thread(ProcessQueue)
            {
                Name = "TPML Async File Logger",
                IsBackground = true
            };
            _workerThread.Start();
        }

        private static void RollOldLog(string currentLogPath)
        {
            try
            {
                if (File.Exists(currentLogPath))
                {
                    string dir = Path.GetDirectoryName(currentLogPath);
                    string oldLogPath = Path.Combine(dir, "tpml_old.log");
                    if (File.Exists(oldLogPath))
                    {
                        File.Delete(oldLogPath);
                    }
                    File.Move(currentLogPath, oldLogPath);
                }
            }
            catch { }
        }

        public void Emit(LogEntry entry)
        {
            if (_disposed || entry == null) return;

            try
            {
                if (!_queue.IsAddingCompleted)
                {
                    _queue.Add(entry);
                }
            }
            catch { }
        }

        public void Flush()
        {
            lock (_syncLock)
            {
                if (_writer == null) return;

                // 尽快排空当前队列中的所有日志
                while (_queue.TryTake(out LogEntry entry))
                {
                    try
                    {
                        _writer.WriteLine(entry.Format());
                    }
                    catch { }
                }

                try
                {
                    _writer.Flush();
                }
                catch { }
            }
        }

        private void ProcessQueue()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_queue.TryTake(out LogEntry entry, 100, _cts.Token))
                    {
                        lock (_syncLock)
                        {
                            if (_writer != null)
                            {
                                _writer.WriteLine(entry.Format());

                                // 批量写完当前累积的条目后统一 Flush
                                int batchCount = 0;
                                while (_queue.TryTake(out LogEntry batchEntry) && batchCount < 100)
                                {
                                    _writer.WriteLine(batchEntry.Format());
                                    batchCount++;
                                }

                                _writer.Flush();
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常退出
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TPML.Core] 异步日志写入异常: {ex.Message}");
            }
            finally
            {
                Flush();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _queue.CompleteAdding();
                _cts.Cancel();
                _workerThread.Join(500);
            }
            catch { }

            lock (_syncLock)
            {
                try
                {
                    Flush();
                    _writer?.Dispose();
                    _writer = null;
                }
                catch { }
            }

            _cts.Dispose();
            _queue.Dispose();
        }
    }
}
