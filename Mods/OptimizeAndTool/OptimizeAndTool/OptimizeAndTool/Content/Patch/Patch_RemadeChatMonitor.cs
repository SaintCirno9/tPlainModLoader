using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using tContentPatch;
using tContentPatch.Patch;
using Terraria.UI.Chat;
using Terraria.GameContent.UI.Chat;

namespace OptimizeAndTool.Content.Patch
{
    internal class Patch_RemadeChatMonitor : Mod
    {
        public static List<ChatMessageContainer> _messages
        {
            get
            {
                if (Terraria.Main.chatMonitor is RemadeChatMonitor monitor)
                {
                    return monitor._messages;
                }
                return null;
            }
        }

        public static int _showCount
        { 
            get
            {
                if (Terraria.Main.chatMonitor is RemadeChatMonitor monitor)
                {
                    return monitor._showCount;
                }
                return 0;
            }
        }

        public static int _startChatLine
        {
            get
            {
                if (Terraria.Main.chatMonitor is RemadeChatMonitor monitor)
                {
                    return monitor._startChatLine;
                }
                return 0;
            }
        }

        public override void AddPatch(IAddPatch addPatch)
        {
            addPatch.AddPrefix(typeof(RemadeChatMonitor).GetMethod(nameof(RemadeChatMonitor.AddNewMessage)),
                typeof(Patch_RemadeChatMonitor).GetMethod(nameof(AddNewMessagePrefix)));
        }

        public static void AddNewMessagePrefix(ref string text, Color color, int widthLimitInPixels = -1)
        {
            try
            {
                CleanRepeatChat.OnAddNewMessage(ref text, color, widthLimitInPixels);
            }
            catch (Exception ex)
            {
                Terraria.Main.NewText($"{nameof(Patch_RemadeChatMonitor)}.{nameof(AddNewMessagePrefix)}:{ex.Message}");
            }
        }
    }
}
