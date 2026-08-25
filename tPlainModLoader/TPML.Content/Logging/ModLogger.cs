using System;

namespace TPML.Content.Logging
{
    public class ModLogger
    {
        public string Name { get; }

        public ModLogger(string name)
        {
            Name = name;
        }

        public void Info(object message) => ModLoader.Log($"[{Name}] [INFO] {message}");
        public void Warn(object message) => ModLoader.Log($"[{Name}] [WARN] {message}");
        public void Error(object message) => ModLoader.Log($"[{Name}] [ERROR] {message}");
        public void Debug(object message) => ModLoader.Log($"[{Name}] [DEBUG] {message}");
    }
}
