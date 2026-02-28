using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.Net;

namespace tContentPatch.Content.Network
{
    [HarmonyPatch(typeof(Main), "Update")]
    internal class RegisterNetModule
    {
        public static bool Loaded = false;

        internal static void Postfix(GameTime gameTime)
        {
            if (Loaded) return;
            if (NetManager.Instance.GetModule<UnbreakableWallScan.NetModule>() == null) return;//如果原版最后一个还没注册

            Loaded = true;
            NetManager.Instance.Register<NetTPMLModule>();
        }
    }
}
