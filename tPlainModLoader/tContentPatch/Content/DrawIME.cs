using Microsoft.Xna.Framework;
using System;
using Terraria;
using TPML.Content.Engine;

namespace tContentPatch.Content
{
    /// <summary>
    /// 绘制输入法
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

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // Main.DoDraw(GameTime)（实例，postfix）
            HookRegistry.Add(MethodLookup.Instance(typeof(Main), "DoDraw", typeof(GameTime)),
                (Action<Action<Main, GameTime>, Main, GameTime>)((orig, self, gameTime) =>
                {
                    orig(self, gameTime);
                    Postfix(gameTime);
                }));
        }

        internal static void Postfix(GameTime gameTime)
        {
            if (NeedIME == false) return;

            NeedIME = false;

            Main.instance.SetIMEPanelAnchor(IME_P, 0f);
            Main.instance.DrawIMEPanel();//输入法
        }
    }
}
