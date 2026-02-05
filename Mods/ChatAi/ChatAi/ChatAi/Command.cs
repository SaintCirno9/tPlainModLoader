using ChatAi.Content;
using ChatAi.Utils;
using ChatAi.Utils.quickBuild;
using CommandHelp;
using System.Collections.Generic;
using tContentPatch;

namespace ChatAi
{
    public class Command : Mod
    {
        public override List<CommandObject> GetCommands()
        {
            List<CommandObject> cos = new List<CommandObject>();

            CommandHRA<bool> cmd = new CommandHRA<bool>(nameof(ChatAi), GameChatAi.Enable,
                new CommandTrue(), new CommandFalse());

            cmd
                .CMDBuild("displayText", GameChatAi.DisplayText)
                .CMDBuild("stringKey", GameChatAi.StringKey)
                .CMDBuild("chatHead", GameChatAi.ChatHead)
                .CMDBuild("type", GameChatAi.AiType);

            cos.Add(cmd);

            return cos;
        }
    }
}
