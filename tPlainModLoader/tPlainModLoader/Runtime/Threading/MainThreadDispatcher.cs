using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TPML.Core.Logging;

namespace TPML.Threading
{
    /// <summary>
    /// 游戏主线程任务调度器。后台回调必须经此回主线程后再读写 UI / 游戏状态。
    /// 作者: SaintCirno9
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static readonly ILogger Logger = LogManager.GetLogger("MainThreadDispatcher");
        private static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();
        private static int _mainThreadId = -1;

        /// <summary>当前是否已捕获主线程且调用方就在主线程上。</summary>
        public static bool IsMainThread
        {
            get
            {
                int id = Volatile.Read(ref _mainThreadId);
                return id >= 0 && Thread.CurrentThread.ManagedThreadId == id;
            }
        }

        /// <summary>由主循环首帧捕获游戏线程 ID。</summary>
        public static void CaptureMainThread()
        {
            Volatile.Write(ref _mainThreadId, Thread.CurrentThread.ManagedThreadId);
        }

        /// <summary>投递到主线程；已在主线程则立即执行。</summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            if (IsMainThread)
            {
                InvokeSafe(action);
                return;
            }
            Queue.Enqueue(action);
        }

        /// <summary>投递并等待主线程执行完毕。续体异步调度，避免内联回主线程死锁。</summary>
        public static Task EnqueueAsync(Action action)
        {
            if (action == null) return Task.CompletedTask;
            if (IsMainThread)
            {
                InvokeSafe(action);
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Queue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>投递函数并返回结果。续体异步调度。</summary>
        public static Task<T> EnqueueAsync<T>(Func<T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));
            if (IsMainThread)
            {
                try
                {
                    return Task.FromResult(func());
                }
                catch (Exception ex)
                {
                    var tcsFail = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
                    tcsFail.TrySetException(ex);
                    return tcsFail.Task;
                }
            }

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Queue.Enqueue(() =>
            {
                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>每帧在主循环中排空队列。</summary>
        public static void Pump(int maxPerFrame = 64)
        {
            int remain = maxPerFrame;
            while (remain-- > 0 && Queue.TryDequeue(out Action action))
            {
                InvokeSafe(action);
            }
        }

        private static void InvokeSafe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.Error("主线程调度任务异常", ex);
            }
        }
    }
}
