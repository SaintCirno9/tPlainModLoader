using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using tContentPatch.Utils;
using Terraria.GameContent.UI.Chat;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 聊天监视器补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_RemadeChatMonitor : ListCopy<PatchRemadeChatMonitor>
    {
        private static List<PatchRemadeChatMonitor> mod = new List<PatchRemadeChatMonitor>();

        public Patch_RemadeChatMonitor() : base(mod) { }

        // AddNewMessage 原版签名 text 为非 ref；前缀声明 ref 是 Harmony 惯例（本地副本传参）
        private delegate void Orig_AddNewMessage(RemadeChatMonitor self, string text, Color color, int widthLimitInPixels);
        private delegate void Hook_AddNewMessage(Orig_AddNewMessage orig, RemadeChatMonitor self, string text, Color color, int widthLimitInPixels);

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var monitor = typeof(RemadeChatMonitor);

            // RemadeChatMonitor.DrawChat(bool)（实例）
            HookRegistry.Add(MethodLookup.Instance(monitor, "DrawChat", typeof(bool)),
                (Action<Action<RemadeChatMonitor, bool>, RemadeChatMonitor, bool>)((orig, self, drawingPlayerChat) =>
                {
                    DrawChatPrefix(drawingPlayerChat);
                    orig(self, drawingPlayerChat);
                    DrawChatPostfix(drawingPlayerChat);
                }));

            // RemadeChatMonitor.AddNewMessage(string, Color, int)（实例，ref 前缀用本地副本）
            HookRegistry.Add(MethodLookup.Instance(monitor, "AddNewMessage", typeof(string), typeof(Color), typeof(int)),
                (Hook_AddNewMessage)AddNewMessageHook);
        }

        private static void AddNewMessageHook(Orig_AddNewMessage orig, RemadeChatMonitor self, string text, Color color, int widthLimitInPixels)
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
