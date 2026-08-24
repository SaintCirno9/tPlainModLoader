using CommandHelp;
using System.Collections.Generic;
using Terraria;

namespace OptimizeAndTool.Content.Cheat
{
    internal class getPlayers
    {
        public static List<CommandObject> GetCO()
        {
            CommandMethod cm = new CommandMethod("getPlayers");

            cm.Runing += _ =>
            {
                for (int i = 0; i < Main.player?.Length; ++i)
                {
                    Player player = Main.player[i];
                    if (player == null) continue;
                    if (player.active == false) continue;

                    string s = $"{i},{player.whoAmI}[{player.name}]";

                    tContentPatch.ContentPatch.PrintTry(s);
                }
            };

            return new List<CommandObject>() { cm };
        }
    }
}
