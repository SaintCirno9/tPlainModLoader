using System.Collections.Generic;
using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.UI;
using Terraria.Utilities;
using Terraria.GameContent.Drawing;
using TPML.Content;

namespace OptimizeAndTool.Content.Cheat.Function2
{
    /// <summary>
    /// 透视照明功能（基于 TPML ModSystem 与 HookGen 强类型门面）
    /// 作者: SaintCirno9
    /// </summary>
    internal class Function_lightHack : TPML.Content.ModSystem
    {
        public static GetSetReset<bool> lightHack = new GetSetReset<bool>();

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("lightHack", lightHack),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get2(lightHack, text: "透视照明"),
            };

            return uis;
        }

        public override void Load()
        {
            On_TileLightScanner.ApplyTileLight += delegate(On_TileLightScanner.orig_ApplyTileLight orig, TileLightScanner self, Tile tile, int x, int y, ref FastRandom localRandom, ref Vector3 lightColor)
            {
                if (lightHack.val)
                {
                    lightColor = Vector3.One;
                }
                orig(self, tile, x, y, ref localRandom, ref lightColor);
            };
        }
    }
}
