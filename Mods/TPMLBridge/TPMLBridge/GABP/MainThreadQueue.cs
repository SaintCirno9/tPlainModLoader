using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TPML.Core.Logging;

namespace TPMLBridge.GABP
{
    /// <summary>
    /// 游戏主线程任务调度器
    /// </summary>
    public static class MainThreadQueue
    {
        private static readonly ILogger Logger = LogManager.GetLogger("MainThreadQueue");
        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public static void Enqueue(Action action)
        {
            if (action == null) return;
            _queue.Enqueue(action);
        }

        public static Task<T> EnqueueAsync<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            _queue.Enqueue(() =>
            {
                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        public static Task EnqueueAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// 每帧在主线程 UpdatePrefix 时调用
        /// </summary>
        public static void Update()
        {
            int maxPerFrame = 50;
            while (maxPerFrame-- > 0 && _queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Logger.Error("执行任务异常", ex);
                }
            }
        }
    }
}
