using Microsoft.Xna.Framework;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace ReduceMouseLag
{
    /// <summary>
    /// 减少鼠标延迟 模组入口
    /// </summary>
    public class ReduceMouseLag : PatchMain
    {
        public override void DoDrawPrefix(GameTime gameTime)
        {
            // 在整个渲染阶段开始前即时更新一次鼠标坐标
            MouseLagFixEngine.UpdateMousePosition();
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            // 在绘制光标前插入即时鼠标刷新层，确保光标像素以最精准的硬件微秒位置呈现
            int cursorIndex = gameInterfaceLayers.FindIndex(layer => layer.Name == "Vanilla: Cursor");
            if (cursorIndex != -1)
            {
                gameInterfaceLayers.Insert(cursorIndex, new LegacyGameInterfaceLayer(
                    "ReduceMouseLag: InstantMouseUpdate",
                    () =>
                    {
                        MouseLagFixEngine.UpdateMousePosition();
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }
    }
}
