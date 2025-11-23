using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace tContentPatch.Content
{
    /// <summary>
    /// 绘制输入法
    /// </summary>
    [HarmonyPatch(typeof(Main), "DoDraw")]
    public static class DrawIME
    {
        public static bool NeedIME = false;
        public static Vector2 IME_P = Vector2.Zero;

        internal static void Postfix(GameTime gameTime)
        {
            if (NeedIME == false) return;

            NeedIME = false;

            //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
            //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            Main.instance.DrawWindowsIMEPanel(IME_P, 0f);//输入法

            Main.spriteBatch.End();
        }
    }
}
