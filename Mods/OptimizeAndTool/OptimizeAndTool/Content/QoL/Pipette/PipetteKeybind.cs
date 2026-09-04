using TPML.Content;

namespace OptimizeAndTool.Content.QoL.Pipette
{
    /// <summary>
    /// 吸管快捷键原生注册与按键响应
    /// 作者: SaintCirno9
    /// </summary>
    public static class PipetteKeybind
    {
        public static ModKeybind Keybind { get; private set; }

        public static void Register()
        {
            if (Keybind == null)
            {
                Keybind = KeybindLoader.RegisterKeybind("OptimizeAndTool", "PipettePickBlock", "Q", "吸管选取物块 (Pick Block)");
            }
        }

        public static void Update()
        {
            if (Keybind != null && Keybind.JustPressed)
            {
                PipetteEngine.PerformPipette();
            }
        }
    }
}
