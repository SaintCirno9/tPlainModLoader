using Microsoft.Xna.Framework.Input;
using Terraria.GameInput;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生热键定义。状态读取走原版 PlayerInput（由 tContentPatch.Input.KeybindLoader 注入 Trigger）。
    /// 作者: SaintCirno9
    /// </summary>
    public class ModKeybind
    {
        public Mod Mod { get; }
        public string Name { get; }
        public string DefaultBinding { get; }
        public string FullName => Mod != null ? $"{Mod.Name}/{Name}" : Name;

        public ModKeybind(Mod mod, string name, string defaultBinding)
        {
            Mod = mod;
            Name = name;
            DefaultBinding = defaultBinding;
        }

        public bool Current => KeybindLoader.GetState(FullName);
        public bool JustPressed => KeybindLoader.JustPressed(FullName);
        public bool JustReleased => KeybindLoader.JustReleased(FullName);
    }

    public static class KeybindLoader
    {
        public static ModKeybind RegisterKeybind(Mod mod, string name, string defaultBinding)
        {
            return new ModKeybind(mod, name, defaultBinding);
        }

        public static ModKeybind RegisterKeybind(Mod mod, string name, Keys defaultKey)
        {
            return new ModKeybind(mod, name, defaultKey.ToString());
        }

        public static bool GetState(string name) => ReadTrigger(name, set => set?.Current);
        public static bool JustPressed(string name) => ReadTrigger(name, set => set?.JustPressed);
        public static bool JustReleased(string name) => ReadTrigger(name, set => set?.JustReleased);
        public static void Update() { }

        private static bool ReadTrigger(string name, System.Func<TriggersPack, TriggersSet> selector)
        {
            if (string.IsNullOrEmpty(name) || PlayerInput.Triggers == null) return false;
            TriggersSet set = selector(PlayerInput.Triggers);
            if (set?.KeyStatus == null) return false;
            if (set.KeyStatus.TryGetValue(name, out bool exact) && exact) return true;
            foreach (var kvp in set.KeyStatus)
            {
                if (kvp.Value && (kvp.Key.Equals(name, System.StringComparison.OrdinalIgnoreCase)
                    || kvp.Key.EndsWith("/" + name, System.StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
