using Microsoft.Xna.Framework;
using PipetteTool.Core;
using PipetteTool.Input;
using tContentPatch;

namespace PipetteTool
{
    /// <summary>
    /// 吸管工具模组主入口
    /// </summary>
    public class PipetteToolMod : PatchMain
    {
        public override void Initialize()
        {
            // 初始化快捷键注册
            PipetteKeyHandler.Initialize();
            // 初始化图格与背景墙的反向物品映射表
            TileToItemResolver.Initialize();
        }

        public override void UpdatePostfix(GameTime gameTime)
        {
            // 监听键盘按键并触发吸管调度（在原版 PlayerInput 输入更新完毕后判定）
            PipetteKeyHandler.UpdateInput(PipetteEngine.PerformPipette);
        }
    }
}
