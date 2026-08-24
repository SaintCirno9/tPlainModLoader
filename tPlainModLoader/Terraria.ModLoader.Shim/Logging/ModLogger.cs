using System;
using System.IO;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 模组日志包装器
    /// </summary>
    public class ModLogger
    {
        public string Name { get; }

        public ModLogger(string name)
        {
            Name = name;
        }

        public void Info(object message) => Console.WriteLine($"[{Name}][INFO] {message}");
        public void Warn(object message) => Console.WriteLine($"[{Name}][WARN] {message}");
        public void Error(object message) => Console.WriteLine($"[{Name}][ERROR] {message}");
        public void Error(object message, Exception exception) => Console.WriteLine($"[{Name}][ERROR] {message}\n{exception}");
        public void Debug(object message) => Console.WriteLine($"[{Name}][DEBUG] {message}");
    }
}
