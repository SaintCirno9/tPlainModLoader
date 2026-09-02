using Terraria.Chat;
using tContentPatch.Command;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 拦截游戏内聊天输入框的指令输入强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal static class Patch_ChatCommand
    {
        private static bool _hooksInitialized = false;

        /// <summary>集中注册聊天指令强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_ChatCommandProcessor.CreateOutgoingMessage += Hook_CreateOutgoingMessage;

            _hooksInitialized = true;
        }

        private static ChatMessage Hook_CreateOutgoingMessage(On_ChatCommandProcessor.orig_CreateOutgoingMessage orig, ChatCommandProcessor self, string text)
        {
            ChatMessage result = null;
            if (CreateOutgoingMessagePrefix(text, ref result))
            {
                result = orig(self, text);
            }
            return result;
        }

        public static bool CreateOutgoingMessagePrefix(string text, ref ChatMessage __result)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;

            string trimmed = text.Trim();
            string cmd = trimmed;
            if (cmd.StartsWith("/") || cmd.StartsWith("."))
            {
                cmd = cmd.Substring(1).Trim();
            }

            if (ProgramCommand.TryRun(cmd))
            {
                var empty = new ChatMessage("");
                empty.Consume();
                __result = empty;
                return false; // 拦截原版聊天广播，指令已由 TPML 消费执行
            }

            return true;
        }
    }
}
