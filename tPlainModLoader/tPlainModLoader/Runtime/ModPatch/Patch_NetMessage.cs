using System.Collections.Generic;
using tContentPatch.Content.Network;
using Terraria;
using Terraria.Localization;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// NetMessage 网络同步强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_NetMessage : ListCopy<PatchNetMessage>
    {
        private static readonly List<PatchNetMessage> mod = new List<PatchNetMessage>();
        internal static List<PatchNetMessage> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_NetMessage() : base(mod) { }

        /// <summary>集中注册 NetMessage 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_NetMessage.SendData += Hook_SendData;
            On_NetMessage.SyncConnectedPlayer += Hook_SyncConnectedPlayer;
            On_NetMessage.SyncDisconnectedPlayer += Hook_SyncDisconnectedPlayer;
            On_NetMessage.SyncOnePlayer += Hook_SyncOnePlayer;

            _hooksInitialized = true;
        }

        private static void Hook_SendData(On_NetMessage.orig_SendData orig,
            int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            SendDataPrefix(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
            orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
            SendDataPostfix(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }

        private static void Hook_SyncConnectedPlayer(On_NetMessage.orig_SyncConnectedPlayer orig, int plr)
        {
            SyncConnectedPlayerPrefix(plr);
            orig(plr);
            SyncConnectedPlayerPostfix(plr);
        }

        private static void Hook_SyncDisconnectedPlayer(On_NetMessage.orig_SyncDisconnectedPlayer orig, int plr)
        {
            SyncDisconnectedPlayerPrefix(plr);
            orig(plr);
            SyncDisconnectedPlayerPostfix(plr);
        }

        private static void Hook_SyncOnePlayer(On_NetMessage.orig_SyncOnePlayer orig, int plr, int toWho, int fromWho)
        {
            SyncOnePlayerPrefix(plr, toWho, fromWho);
            orig(plr, toWho, fromWho);
            SyncOnePlayerPostfix(plr, toWho, fromWho);
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

        public static void SyncDisconnectedPlayerPrefix(int plr)
        {
            if (Main.dedServ == false) return;

            mod.ForTry(item => item.SyncDisconnectedPlayerPrefix(plr));
        }

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
