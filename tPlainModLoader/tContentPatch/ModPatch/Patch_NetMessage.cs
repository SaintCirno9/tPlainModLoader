using System;
using System.Collections.Generic;
using System.Reflection;
using tContentPatch.Content.Network;
using Terraria;
using Terraria.Localization;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// NetMessage 网络同步补丁（M2 迁移：Harmony → MonoMod，全部为静态方法）
    /// </summary>
    internal class Patch_NetMessage : ListCopy<PatchNetMessage>
    {
        private static List<PatchNetMessage> mod = new List<PatchNetMessage>();

        public Patch_NetMessage() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var netMessage = typeof(NetMessage);

            // NetMessage.SendData(int, int, int, NetworkText, int, float, float, float, int, int, int)（静态）
            HookRegistry.Add(MethodLookup.Static(netMessage, "SendData", new[]
                {
                    typeof(int), typeof(int), typeof(int), typeof(NetworkText),
                    typeof(int), typeof(float), typeof(float), typeof(float), typeof(int), typeof(int), typeof(int)
                }),
                (Action<Action<int, int, int, NetworkText, int, float, float, float, int, int, int>, int, int, int, NetworkText, int, float, float, float, int, int, int>)((orig, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7) =>
                {
                    SendDataPrefix(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
                    orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
                    SendDataPostfix(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
                }));

            // NetMessage.SyncConnectedPlayer(int)（静态）
            HookRegistry.Add(MethodLookup.Static(netMessage, "SyncConnectedPlayer", typeof(int)),
                (Action<Action<int>, int>)((orig, plr) =>
                {
                    SyncConnectedPlayerPrefix(plr);
                    orig(plr);
                    SyncConnectedPlayerPostfix(plr);
                }));

            // NetMessage.SyncDisconnectedPlayer(int)（静态）
            HookRegistry.Add(MethodLookup.Static(netMessage, "SyncDisconnectedPlayer", typeof(int)),
                (Action<Action<int>, int>)((orig, plr) =>
                {
                    SyncDisconnectedPlayerPrefix(plr);
                    orig(plr);
                    SyncDisconnectedPlayerPostfix(plr);
                }));

            // NetMessage.SyncOnePlayer(int, int, int)（静态）
            HookRegistry.Add(MethodLookup.Static(netMessage, "SyncOnePlayer", typeof(int), typeof(int), typeof(int)),
                (Action<Action<int, int, int>, int, int, int>)((orig, plr, toWho, fromWho) =>
                {
                    SyncOnePlayerPrefix(plr, toWho, fromWho);
                    orig(plr, toWho, fromWho);
                    SyncOnePlayerPostfix(plr, toWho, fromWho);
                }));
        }

        public static void SendDataPrefix(int msgType, int remoteClient, int ignoreClient, NetworkText text,
            int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            mod.ForTry(item => item.SendDataPrefix(msgType, remoteClient, ignoreClient, text,
                number, number2, number3, number4, number5, number6, number7));
        }

        public static void SendDataPostfix(int msgType, int remoteClient, int ignoreClient, NetworkText text,
            int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            mod.ForTry(item => item.SendDataPostfix(msgType, remoteClient, ignoreClient, text,
                number, number2, number3, number4, number5, number6, number7));

            if (msgType == Terraria.ID.MessageID.PlayerSpawn && Main.netMode == 1 && ContentPatch.NoPublic) NetTPMLModule.SendToServer();
        }

        public static void SyncConnectedPlayerPrefix(int plr)
        {
            if (Main.dedServ == false) return;

            mod.ForTry(item => item.SyncConnectedPlayerPrefix(plr));
        }

        public static void SyncConnectedPlayerPostfix(int plr)
        {
            if (Main.dedServ == false) return;

            mod.ForTry(item => item.SyncConnectedPlayerPostfix(plr));
        }

        /// <summary>
        /// 建议使用NetMessage.SyncOnePlayer
        /// <para/><see cref="NetMessage.SyncDisconnectedPlayer(int)"/>的修补有可能失效
        /// <para/>在服务端启动后才修补时这个就没效果了
        /// <para/>服务端是运行在另一个线程上的<see cref="Netplay.StartServer"/>
        /// <para/>这个方法就是由另一个线程调用的, 所以可能是因为这个原因导致的
        /// <para/>所以最好在有线程启动前修补
        /// </summary>
        public static void SyncDisconnectedPlayerPrefix(int plr)
        {
            if (Main.dedServ == false) return;

            mod.ForTry(item => item.SyncDisconnectedPlayerPrefix(plr));
        }

        /// <summary>
        /// 和<see cref="SyncDisconnectedPlayerPrefix(int)"/>一样的问题
        /// </summary>
        public static void SyncDisconnectedPlayerPostfix(int plr)
        {
            if (Main.dedServ == false) return;

            mod.ForTry(item => item.SyncDisconnectedPlayerPostfix(plr));
        }

        public static void SyncOnePlayerPrefix(int plr, int toWho, int fromWho)
        {
            if (Main.dedServ == false) return;

            mod.ForTry(item => item.SyncOnePlayerPrefix(plr, toWho, fromWho));
        }

        public static void SyncOnePlayerPostfix(int plr, int toWho, int fromWho)
        {
            if (Main.dedServ == false) return;

            mod.ForTry(item => item.SyncOnePlayerPostfix(plr, toWho, fromWho));
        }
    }
}
