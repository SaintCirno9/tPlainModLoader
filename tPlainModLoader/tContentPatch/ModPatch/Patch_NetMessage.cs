using HarmonyLib;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(NetMessage))]
    internal class Patch_NetMessage : ListCopy<PatchNetMessage>
    {
        private static List<PatchNetMessage> mod = new List<PatchNetMessage>();

        public Patch_NetMessage() : base(mod) { }

        [HarmonyPatch("SendData")]
        [HarmonyPrefix]
        public static void SendDataPrefix(int msgType, int remoteClient, int ignoreClient, NetworkText text,
            int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            mod.ForTry(item => item.SendDataPrefix(msgType, remoteClient, ignoreClient, text,
                number, number2, number3, number4, number5, number6, number7));
        }

        [HarmonyPatch("SendData")]
        [HarmonyPostfix]
        public static void SendDataPostfix(int msgType, int remoteClient, int ignoreClient, NetworkText text,
            int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            mod.ForTry(item => item.SendDataPostfix(msgType, remoteClient, ignoreClient, text,
                number, number2, number3, number4, number5, number6, number7));
        }
        
        [HarmonyPatch("SyncConnectedPlayer")]
        [HarmonyPrefix]
        public static void SyncConnectedPlayerPrefix(int plr)
        {
            if (Main.netMode != 2) return;

            mod.ForTry(item => item.SyncConnectedPlayerPrefix(plr));
        }

        [HarmonyPatch("SyncConnectedPlayer")]
        [HarmonyPostfix]
        public static void SyncConnectedPlayerPostfix(int plr)
        {
            if (Main.netMode != 2) return;

            mod.ForTry(item => item.SyncConnectedPlayerPostfix(plr));
        }

        [HarmonyPatch("SyncDisconnectedPlayer")]
        [HarmonyPrefix]
        public static void SyncDisconnectedPlayerPrefix(int plr)
        {
            if (Main.netMode != 2) return;

            mod.ForTry(item => item.SyncDisconnectedPlayerPrefix(plr));
        }

        [HarmonyPatch("SyncDisconnectedPlayer")]
        [HarmonyPostfix]
        public static void SyncDisconnectedPlayerPostfix(int plr)
        {
            if (Main.netMode != 2) return;

            mod.ForTry(item => item.SyncDisconnectedPlayerPostfix(plr));
        }
    }
}
