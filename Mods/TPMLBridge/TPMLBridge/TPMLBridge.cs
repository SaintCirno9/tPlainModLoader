using Microsoft.Xna.Framework;
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
            // 每帧分发执行主线程队列中的任务
            MainThreadQueue.Update();
        }
    }
}
