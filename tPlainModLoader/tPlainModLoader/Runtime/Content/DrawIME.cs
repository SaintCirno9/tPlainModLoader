using Microsoft.Xna.Framework;
using Terraria;

namespace tContentPatch.Content
{
    /// <summary>
    /// 绘制输入法（由 Patch_Main.Hook_DoDraw 单点调度）
    /// </summary>
    public static class DrawIME
    {
        /// <summary>
        /// 需要绘制输入法
        /// </summary>
        public static bool NeedIME = false;
        /// <summary>
        /// 输入法位置
        /// </summary>
        public static Vector2 IME_P = Vector2.Zero;

        public static void RegisterAll()
        {
            // 已收敛由 Patch_Main 单点调度，无需单独 Detour
        }

        internal static void Postfix(GameTime gameTime)
        {
            if (NeedIME == false) return;

            NeedIME = false;

            Main.instance.SetIMEPanelAnchor(IME_P, 0f);
            Main.instance.DrawIMEPanel();
        }
    }
}
