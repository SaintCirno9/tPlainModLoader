using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using System.Diagnostics;
using tContentPatch;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.Function1
{
    internal partial class Function_buff : TPML.Content.ModSystem
    {
        public static GetSetReset<bool> clearBuff = new GetSetReset<bool>();
        public static GetSetReset<bool> addBuff_Invisibility = new GetSetReset<bool>();

        public override void DoUpdateInWorldPrefix()
        {
            Player player = Main.LocalPlayer;

            if (clearBuff.val)
            {
                for (int i = 0; i < Player.maxBuffs; ++i)
                {
                    player.buffType[i] = 0;
                    player.buffTime[i] = 0;
                }
            }
            if (addBuff_Invisibility.val)
            {
                player.AddBuff(BuffID.Invisibility, 10);
            }
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("clearBuff", clearBuff),
                CommandBuild.get2("addBuff_Invisibility", addBuff_Invisibility),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get2(clearBuff, null, "Images/Buff", "清空buff"),
                UIBuild.get2(addBuff_Invisibility, null, "Images/Buff_10", "隐身"),
            };

            return uis;
        }
    }
}
