using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Terraria.GameInput;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 快捷键包装类
    /// </summary>
    public class ModKeybind
    {
        public Mod Mod { get; }
        public string Name { get; }
        public string DefaultBinding { get; }
        public string FullName => $"{Mod?.Name ?? "Terraria"}/{Name}";

        public Keys AssignedKey { get; set; } = Keys.None;

        public bool Current { get; internal set; }
        public bool Old { get; internal set; }
        public bool JustPressed => Current && !Old;
        public bool JustReleased => !Current && Old;

        public ModKeybind(Mod mod, string name, string defaultBinding)
        {
            Mod = mod;
            Name = name;
            DefaultBinding = defaultBinding;

            if (!string.IsNullOrEmpty(defaultBinding) && Enum.TryParse<Keys>(defaultBinding, true, out var key))
            {
                AssignedKey = key;
            }
        }

        internal void Update(KeyboardState currentKb, KeyboardState oldKb)
        {
            Old = Current;
            if (AssignedKey != Keys.None)
            {
                Current = currentKb.IsKeyDown(AssignedKey);
            }
            else
            {
                Current = false;
            }
        }
    }

    /// <summary>
    /// tModLoader 快捷键加载与注册中心
    /// </summary>
    public static class KeybindLoader
    {
        private static readonly List<ModKeybind> _keybinds = new List<ModKeybind>();
        private static KeyboardState _oldKb;

        public static IReadOnlyList<ModKeybind> Keybinds => _keybinds;

        public static ModKeybind RegisterKeybind(Mod mod, string name, string defaultBinding)
        {
            var keybind = new ModKeybind(mod, name, defaultBinding);
            _keybinds.Add(keybind);
            return keybind;
        }

        public static ModKeybind RegisterKeybind(Mod mod, string name, Keys defaultBinding)
        {
            return RegisterKeybind(mod, name, defaultBinding.ToString());
        }

        public static void Update()
        {
            KeyboardState currentKb = Keyboard.GetState();
            for (int i = 0; i < _keybinds.Count; i++)
            {
                _keybinds[i].Update(currentKb, _oldKb);
            }
            _oldKb = currentKb;
        }

        public static void Clear()
        {
            _keybinds.Clear();
        }
    }
}
