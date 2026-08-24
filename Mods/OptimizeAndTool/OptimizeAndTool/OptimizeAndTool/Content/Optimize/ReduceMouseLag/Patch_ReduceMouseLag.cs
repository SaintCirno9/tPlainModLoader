using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;

namespace OptimizeAndTool.Content.Optimize.ReduceMouseLag
{
    /// <summary>
    /// 在主游戏绘制开始前挂载高频采样，消除光标滞后感
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Main), "Draw")]
    public class Patch_ReduceMouseLag
    {
        [HarmonyPrefix]
        public static void Prefix(GameTime gameTime)
        {
            MouseLagFixEngine.UpdateMousePosition();
        }
    }
}
