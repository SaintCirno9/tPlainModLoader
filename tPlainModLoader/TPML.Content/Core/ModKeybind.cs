using Microsoft.Xna.Framework.Input;
using Terraria.GameInput;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生热键定义
    /// </summary>
    public class ModKeybind
    {
        public Mod Mod { get; }
        public string Name { get; }
        public string DefaultBinding { get; }

        public ModKeybind(Mod mod, string name, string defaultBinding)
        {
            Mod = mod;
            Name = name;
            DefaultBinding = defaultBinding;
        }

        public bool Current => KeybindLoader.GetState(Name);
        public bool JustPressed => KeybindLoader.JustPressed(Name);
        public bool JustReleased => KeybindLoader.JustReleased(Name);
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

        public static bool GetState(string name) => false;
        public static bool JustPressed(string name) => false;
        public static bool JustReleased(string name) => false;
        public static void Update() { }
    }
}
