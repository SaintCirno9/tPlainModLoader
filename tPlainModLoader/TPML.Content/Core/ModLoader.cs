using System;

namespace TPML.Content
{
    /// <summary>
    /// TPML 模组运行日志与全局状态中心
    /// </summary>
    public static class ModLoader
    {
        public static string Version => "1.4.4.9-TPML-2026.8";
        public static Action<string> LogCallback;

        public static void Log(string message)
        {
            Console.WriteLine($"[TPML] {message}");
            try
            {
                LogCallback?.Invoke(message);
            }
            catch { }
        }
    }
}
