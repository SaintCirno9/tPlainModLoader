using Terraria.Chat;
using tContentPatch.Command;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 拦截游戏内聊天输入框的指令输入（支持以 / 或 . 开头或直接输入指令名），
    /// 并在本地作为 tContentPatch 指令分发执行，拦截原版公屏广播。
    /// </summary>
    internal static class Patch_ChatCommand
    {
        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // ChatCommandProcessor.CreateOutgoingMessage(string)（实例，返回 ChatMessage）
            HookRegistry.Add(MethodLookup.Instance(typeof(ChatCommandProcessor), "CreateOutgoingMessage", typeof(string)),
                (System.Func<System.Func<ChatCommandProcessor, string, ChatMessage>, ChatCommandProcessor, string, ChatMessage>)((orig, self, text) =>
                {
                    ChatMessage result = null;
                    if (CreateOutgoingMessagePrefix(text, ref result)) result = orig(self, text);
                    return result;
                }));
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
