using Microsoft.Xna.Framework;
using Terraria;

namespace OptimizeAndTool.Content.Optimize.ReduceMouseLag
{
    /// <summary>
    /// 在主游戏绘制开始前挂载高频采样，消除光标滞后感（基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    public static class ReduceMouseLagHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Main.Draw += Hook_Draw;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Main.Draw -= Hook_Draw;
            _registered = false;
        }

        private static void Hook_Draw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
        {
            MouseLagFixEngine.UpdateMousePosition();
            orig(self, gameTime);
        }
    }
}
