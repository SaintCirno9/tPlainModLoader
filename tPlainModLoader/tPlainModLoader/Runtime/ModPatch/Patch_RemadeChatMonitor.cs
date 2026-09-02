#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using tContentPatch.Utils;
using Terraria.GameContent.UI.Chat;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 聊天监视器强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_RemadeChatMonitor : ListCopy<PatchRemadeChatMonitor>
    {
        private static readonly List<PatchRemadeChatMonitor> mod = new List<PatchRemadeChatMonitor>();
        internal static List<PatchRemadeChatMonitor> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_RemadeChatMonitor() : base(mod) { }

        /// <summary>集中注册 RemadeChatMonitor 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_RemadeChatMonitor.DrawChat += Hook_DrawChat;
            On_RemadeChatMonitor.AddNewMessage += Hook_AddNewMessage;

            _hooksInitialized = true;
        }

        private static void Hook_DrawChat(On_RemadeChatMonitor.orig_DrawChat orig, RemadeChatMonitor self, bool drawingPlayerChat)
        {
            DrawChatPrefix(drawingPlayerChat);
            orig(self, drawingPlayerChat);
            DrawChatPostfix(drawingPlayerChat);
        }

        private static void Hook_AddNewMessage(On_RemadeChatMonitor.orig_AddNewMessage orig, RemadeChatMonitor self, string text, Color color, int widthLimitInPixels)
        {
            string textLocal = text;
            AddNewMessagePrefix(ref textLocal, color, widthLimitInPixels);
            orig(self, textLocal, color, widthLimitInPixels);
        }

        public static void DrawChatPrefix(bool drawingPlayerChat)
        {
            mod.ForTry(item => item.DrawChatPrefix(drawingPlayerChat));
        }

        public static void DrawChatPostfix(bool drawingPlayerChat)
        {
            mod.ForTry(item => item.DrawChatPostfix(drawingPlayerChat));
        }

        public static void AddNewMessagePrefix(ref string text, Color color, int widthLimitInPixels = -1)
        {
            try
            {
                foreach (PatchRemadeChatMonitor item in mod) item.AddNewMessagePrefix(ref text, color, widthLimitInPixels);
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }
    }
}
