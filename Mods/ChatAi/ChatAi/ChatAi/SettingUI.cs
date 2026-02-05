using ChatAi.Content;
using ChatAi.Utils.quickBuild;
using tContentPatch;
using Terraria.UI;

namespace ChatAi
{
    internal class SettingUI_player : ModSetting
    {
        public override string Name => "设置";
        public override string Title => "聊天Ai: 设置";

        public override UIElement GetUI()
        {
            return UIBuild.get3(GameChatAi.GetUI());
        }
    }
}
