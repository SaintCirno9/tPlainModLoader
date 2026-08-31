using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace TPML.Core.Logging.Sinks
{
    /// <summary>
    /// 高性能异步批量落盘文件日志输出端。同目录多进程追加同一文件；写盘异常后尝试重建 writer，不永久退出。
    /// 作者: SaintCirno9
    /// </summary>
    public class AsyncFileLogSink : ILogSink
    {
        private const int MaxQueueSize = 20000;
        private readonly string _logPath;
        private readonly BlockingCollection<LogEntry> _queue = new BlockingCollection<LogEntry>(new ConcurrentQueue<LogEntry>(), MaxQueueSize);
        private readonly Thread _workerThread;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _syncLock = new object();
        private StreamWriter _writer;
        private volatile bool _disposed;
        private int _ioFailCount;

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

            RollOldLog(_logPath);
            OpenWriter();

            _workerThread = new Thread(ProcessQueue)
            {
                Name = "TPML Async File Logger",
                IsBackground = true
            };
            _workerThread.Start();
        }

        private void OpenWriter()
        {
            try
            {
                FileStream fs = new FileStream(
                    _logPath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite | FileShare.Delete);
                _writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = false };
                _writer.WriteLine($"=== TPML Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} pid={Process.GetCurrentProcess().Id} ===");
                _writer.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TPML.Core] 无法创建日志文件 ({_logPath}): {ex.Message}");
            }
        }

        private static void RollOldLog(string currentLogPath)
        {
            try
            {
                if (!File.Exists(currentLogPath)) return;
                FileInfo info = new FileInfo(currentLogPath);
                if (info.Length < 4 * 1024 * 1024) return;

                string dir = Path.GetDirectoryName(currentLogPath);
                string name = Path.GetFileNameWithoutExtension(currentLogPath);
                string ext = Path.GetExtension(currentLogPath);
                string oldLogPath = Path.Combine(dir ?? "", name + "_old" + ext);
                if (File.Exists(oldLogPath))
                {
                    try { File.Delete(oldLogPath); } catch { }
                }
                File.Move(currentLogPath, oldLogPath);
            }
            catch
            {
            }
        }

        public void Emit(LogEntry entry)
        {
            if (_disposed || entry == null) return;

            try
            {
                if (!_queue.IsAddingCompleted)
                {
                    _queue.TryAdd(entry, 0);
                }
            }
            catch { }
        }

        public void Flush()
        {
            lock (_syncLock)
            {
                if (_writer == null) return;

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
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (!_queue.TryTake(out LogEntry entry, 100, _cts.Token)) continue;

                    lock (_syncLock)
                    {
                        if (_writer == null)
                        {
                            OpenWriter();
                            if (_writer == null) continue;
                        }

                        try
                        {
                            _writer.WriteLine(entry.Format());

                            int batchCount = 0;
                            while (_queue.TryTake(out LogEntry batchEntry) && batchCount < 100)
                            {
                                _writer.WriteLine(batchEntry.Format());
                                batchCount++;
                            }

                            _writer.Flush();
                            _ioFailCount = 0;
                        }
                        catch (Exception writeEx)
                        {
                            _ioFailCount++;
                            Console.WriteLine($"[TPML.Core] 异步日志写入异常 (#{_ioFailCount}): {writeEx.Message}");
                            try { _writer?.Dispose(); } catch { }
                            _writer = null;
                            if (_ioFailCount < 8)
                            {
                                OpenWriter();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TPML.Core] 异步日志循环异常: {ex.Message}");
                    Thread.Sleep(200);
                }
            }

            Flush();
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
