using Microsoft.Xna.Framework;
using Terraria;
using tContentPatch;
using TPMLBridge.GABP;

namespace TPMLBridge
{
    public class TPMLBridgeMod : Mod
    {
        public override void Load()
        {
            // 启动 GABP TCP Server
            GABPServer.StartFromEnvironment();
        }

        public override void Unload()
        {
            GABPServer.Instance?.Stop();
        }
    }

    public class TPMLBridgePatchMain : PatchMain
    {
        public override void UpdatePrefix(GameTime gameTime)
        {
            // 如果存在长按/通道武器持续按住的任务
            if (TerrariaTools.PendingHoldUseFrames > 0 && !Main.gameMenu && Main.LocalPlayer != null)
            {
                TerrariaTools.PendingHoldUseFrames--;
                Main.LocalPlayer.controlUseItem = true;
                if (TerrariaTools.PendingHoldAlt)
                {
                    Main.LocalPlayer.altFunctionUse = 1;
                }
            }

            // 每帧分发执行主线程队列中的任务
            MainThreadQueue.Update();
        }
    }
}
