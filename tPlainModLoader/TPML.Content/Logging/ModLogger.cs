using System;
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

        public void Info(object message) => ModLoader.Log($"[{Name}][INFO] {message}");
        public void Warn(object message) => ModLoader.Log($"[{Name}][WARN] {message}");
        public void Error(object message) => ModLoader.Log($"[{Name}][ERROR] {message}");
        public void Error(object message, Exception exception) => ModLoader.Log($"[{Name}][ERROR] {message}\n{exception}");
        public void Debug(object message) => ModLoader.Log($"[{Name}][DEBUG] {message}");
    }
}
