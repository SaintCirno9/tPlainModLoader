using Microsoft.Xna.Framework;
using Terraria.GameContent;
using Terraria.Net;

namespace tContentPatch.Content.Network
{
    /// <summary>
    /// 网络模块注册（由 Patch_Main.Hook_Update 单点调度）
    /// </summary>
    internal static class RegisterNetModule
    {
        public static bool Loaded = false;

        public static void RegisterAll()
        {
            // 已收敛由 Patch_Main 单点调度，无需单独 Detour
        }

        internal static void Postfix(GameTime gameTime)
        {
            if (Loaded) return;
            if (NetManager.Instance.GetModule<UnbreakableWallScan.NetModule>() == null) return;

            Loaded = true;
            NetManager.Instance.Register<NetTPMLModule>();
        }
    }
}
