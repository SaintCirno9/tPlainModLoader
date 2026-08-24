using Terraria;

namespace OptimizeAndTool.Content.Creative
{
    public class CreativeInventory
    {
        private static UICreativeInventory ui_ci = null;

        public static bool IsOpen => ui_ci != null && ui_ci.IsOpen;
        public static bool IsHovering => ui_ci != null && ui_ci.IsOpen && ui_ci.ContainsPoint(Main.MouseScreen);
        public static UICreativeInventory UI => ui_ci;

        public static void SwitchOpenOrClose()
        {
            if (ui_ci == null)
            {
                ui_ci = new UICreativeInventory("物品浏览器", 600, 450);
            }

            if (ui_ci.IsOpen) ui_ci.Close();
            else ui_ci.Open(ModifyInterfaceLayers.ui_state);
        }
    }
}
