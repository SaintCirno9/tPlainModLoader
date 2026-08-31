using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Net;
using TPML.Content.Engine;

namespace tContentPatch.Content.Network
{
    /// <summary>
    /// 网络模块注册（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class RegisterNetModule
    {
        public static bool Loaded = false;

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // Main.Update(GameTime)（实例，postfix）
            HookRegistry.Add(MethodLookup.Instance(typeof(Main), "Update", typeof(GameTime)),
                (Action<Action<Main, GameTime>, Main, GameTime>)((orig, self, gameTime) =>
                {
                    orig(self, gameTime);
                    Postfix(gameTime);
                }));
        }

        internal static void Postfix(GameTime gameTime)
        {
            if (Loaded) return;
            if (NetManager.Instance.GetModule<UnbreakableWallScan.NetModule>() == null) return;//如果原版最后一个还没注册

            Loaded = true;
            NetManager.Instance.Register<NetTPMLModule>();
        }
    }
}
