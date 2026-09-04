using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TPML;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.Function2
{
    internal class Function_mouseToPlayAngle : TPML.Content.ModSystem
    {
        public static GetSetReset<bool> mtpa = new GetSetReset<bool>();
        public static GetSetReset<int> mtpa_play = new GetSetReset<int>(-1, -1);

        public override void Initialize()
        {
            mtpa_play.OnValUpdate += v => Utils.OutputPlayerName(mtpa_play.val);
        }

        public override void DoUpdateInWorldPrefix()
        {
            Player This = Main.LocalPlayer;

            if (mtpa.val == false) return;

            if (mtpa_play.val < 0) return;
            if (mtpa_play.val >= Main.player.Length) return;

            Player target = Main.player[mtpa_play.val];

            Vector2 targetP = WorldCheatFunctions.aimAdvance.val ?
                Utils.aimAdvance(This.Center, WorldCheatFunctions.aimAdvance_val.val, target.Center, target.velocity) :
                target.Center;

            Vector2 mouseP = targetP - Main.screenPosition;

            Main.mouseX = Convert.ToInt32(mouseP.X);
            Main.mouseY = Convert.ToInt32(mouseP.Y);
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get1("mouseToPlayAngle", mtpa, mtpa_play, new CommandInt()),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get1(mtpa, mtpa_play, int.Parse, "<int>", null, "鼠标指向玩家方向"),
            };

            return uis;
        }
    }
}
