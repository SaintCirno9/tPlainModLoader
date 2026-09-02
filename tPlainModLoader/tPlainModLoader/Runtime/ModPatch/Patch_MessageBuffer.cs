#pragma warning disable CS0618
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// MessageBuffer 网络数据分发强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_MessageBuffer : ListCopy<PatchMessageBuffer>
    {
        private static readonly List<PatchMessageBuffer> mod = new List<PatchMessageBuffer>();
        internal static List<PatchMessageBuffer> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_MessageBuffer() : base(mod) { }

        /// <summary>集中注册 MessageBuffer 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_MessageBuffer.GetData += Hook_GetData;

            _hooksInitialized = true;
        }

        private static void Hook_GetData(On_MessageBuffer.orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
        {
            messageType = 0;
            if (!GetDataPrefix(self, start, length, messageType)) return;
            orig(self, start, length, out messageType);
            GetDataPostfix(self, start, length, messageType);
        }

        public static bool GetDataPrefix(MessageBuffer __instance, int start, int length, int messageType)
        {
            messageType = __instance.readBuffer[start];

            if (__instance.reader == null) __instance.ResetReader();

            __instance.reader.BaseStream.Position = start + 1;
            GetDataPrefix_14(__instance, start, length, messageType);

            bool ok = true;
            mod.ForTry(item =>
            {
                __instance.reader.BaseStream.Position = start + 1;
                ok &= item.CanGetData(__instance, start, length, messageType);
            });

            mod.ForTry(item =>
            {
                __instance.reader.BaseStream.Position = start + 1;
                item.GetDataPrefix(__instance, start, length, messageType);
            });

            __instance.reader.BaseStream.Position = start + 1;

            if (ok == false)
            {
                if (__instance.whoAmI < Netplay.MaxConnections)
                {
                    Netplay.Clients[__instance.whoAmI].TimeOutTimer = 0;
                }
                else
                {
                    Netplay.Connection.TimeOutTimer = 0;
                }
                return false;
            }

            return true;
        }

        private static void GetDataPrefix_14(MessageBuffer __instance, int start, int length, int messageType)
        {
            if (messageType != MessageID.PlayerActive) return;
            if (Main.netMode != 1) return;

            int whoAmI = __instance.reader.ReadByte();
            if (Main.player?.IndexInRange(whoAmI) != true) return;

            bool activeNew = __instance.reader.ReadByte() == 1;

            if (activeNew == Main.player[whoAmI].active) return;

            if (activeNew)
            {
                mod.ForTry(item => item.OnPlayerConnect(whoAmI));
            }
            else
            {
                mod.ForTry(item => item.OnPlayerDisconnect(whoAmI));
            }
        }

        public static void GetDataPostfix(MessageBuffer __instance, int start, int length, int messageType)
        {
            messageType = __instance.readBuffer[start];

            mod.ForTry(item =>
            {
                __instance.reader.BaseStream.Position = start + 1;
                item.GetDataPostfix(__instance, start, length, messageType);
            });

            __instance.reader.BaseStream.Position = start + 1;
        }
    }
}
