using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Terraria;
using Terraria.ID;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(MessageBuffer))]
    internal class Patch_MessageBuffer : ListCopy<PatchMessageBuffer>
    {
        private static List<PatchMessageBuffer> mod = new List<PatchMessageBuffer>();

        public Patch_MessageBuffer() : base(mod) { }

        [HarmonyPatch("GetData")]
        [HarmonyPrefix]
        public static void GetDataPrefix(MessageBuffer __instance, int start, int length, int messageType)
        {
            messageType = __instance.readBuffer[start];

            if (__instance.reader == null) __instance.ResetReader();

            //

            __instance.reader.BaseStream.Position = start + 1;
            GetDataPrefix_14(__instance, start, length, messageType);

            mod.ForTry(item =>
            {
                __instance.reader.BaseStream.Position = start + 1;

                item.GetDataPrefix(__instance, start, length, messageType);
            });

            __instance.reader.BaseStream.Position = start + 1;
        }

        private static void GetDataPrefix_14(MessageBuffer __instance, int start, int length, int messageType)
        {
            if (messageType != MessageID.PlayerActive) return;
            if (Main.netMode != 1) return;

            int whoAmI = __instance.reader.ReadByte();
            if (Main.player?.IndexInRange(whoAmI) != true) return;

            int activeNewNum = __instance.reader.ReadByte();
            bool activeNew = activeNewNum == 1;

            bool active = Main.player[whoAmI].active;

            if (activeNew != active)
            {
                if (activeNew)
                {
                    mod.ForTry(item => item.OnPlayerConnect(whoAmI));
                }
                else
                {
                    mod.ForTry(item => item.OnPlayerDisconnect(whoAmI));
                }
            }
        }

        [HarmonyPatch("GetData")]
        [HarmonyPostfix]
        public static void GetDataPostfix(MessageBuffer __instance, int start, int length, int messageType)
        {
            mod.ForTry(item =>
            {
                __instance.reader.BaseStream.Position = start + 1;

                item.GetDataPostfix(__instance, start, length, messageType);
            });

            __instance.reader.BaseStream.Position = start + 1;
        }
    }
}
