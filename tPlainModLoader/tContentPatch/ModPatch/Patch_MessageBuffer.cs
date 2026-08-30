using System;
using System.Collections.Generic;
using tContentPatch.Content.Network;
using Terraria;
using Terraria.ID;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// MessageBuffer 网络数据分发补丁（M2 迁移：Harmony → MonoMod，prefix 返回 false 跳过原方法）
    /// </summary>
    internal class Patch_MessageBuffer : ListCopy<PatchMessageBuffer>
    {
        private static List<PatchMessageBuffer> mod = new List<PatchMessageBuffer>();

        public Patch_MessageBuffer() : base(mod) { }

        // 原方法第三参为 out int，需自定义 byref 委托
        private delegate void Orig_GetData(MessageBuffer self, int start, int length, out int messageType);
        private delegate void Hook_GetData(Orig_GetData orig, MessageBuffer self, int start, int length, out int messageType);

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // MessageBuffer.GetData(int, int, out int)（实例，返回 void，第三参 out）
            HookRegistry.Add(MethodLookup.Instance(typeof(MessageBuffer), "GetData", typeof(int), typeof(int), typeof(int).MakeByRefType()),
                (Hook_GetData)GetDataHook);
        }

        private static void GetDataHook(Orig_GetData orig, MessageBuffer self, int start, int length, out int messageType)
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

            //

            __instance.reader.BaseStream.Position = start + 1;
            GetDataPrefix_14(__instance, start, length, messageType);

            //

            bool ok = true;
            mod.ForTry(item =>
            {
                __instance.reader.BaseStream.Position = start + 1;

                ok &= item.CanGetData(__instance, start, length, messageType);
            });

            //

            mod.ForTry(item =>
            {
                __instance.reader.BaseStream.Position = start + 1;

                item.GetDataPrefix(__instance, start, length, messageType);
            });

            __instance.reader.BaseStream.Position = start + 1;

            //

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
