using tContentPatch.Input;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 饰品箱原生按键绑定注册与交互
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBoxKeybind
    {
        public static ModKeybind Keybind { get; private set; }

        public static void Register()
        {
            if (Keybind == null)
            {
                Keybind = KeybindLoader.RegisterKeybind("OptimizeAndTool", "ToggleAccessoryBox", "P", "打开/关闭饰品盒 (AccessoryBox)");
            }
        }

        public static void Update()
        {
            if (Keybind != null && Keybind.JustPressed)
            {
                BoxWindow.Toggle();
            }
        }
    }
}
