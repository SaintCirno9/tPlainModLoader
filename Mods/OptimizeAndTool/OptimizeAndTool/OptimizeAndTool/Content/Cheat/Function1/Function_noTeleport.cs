using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.Function1
{
    /// <summary>
    /// //NoPublic
    /// </summary>
    internal class Function_noTeleport : Mod
    {
        public static GetSetReset<bool> noTeleport = new GetSetReset<bool>();

        // M2：弃用 IAddPatch，改用 MonoMod.HookGen 的 On_ 门面（tML 标准做法）
        public override void Load()
        {
            On_Player.Teleport += (orig, self, newPos, Style, extraInfo) =>
            {
                if (self == Main.LocalPlayer && noTeleport.val) return; // 跳过（prefix 返回 false）
                orig(self, newPos, Style, extraInfo);
            };
        }

        public static bool Teleport(Player __instance, Vector2 newPos, int Style = 0, int extraInfo = 0)
        {
            if (__instance != Main.LocalPlayer) return true;
            return noTeleport.val == false;
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("noTeleport", noTeleport),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get2(noTeleport, "重新加载模组后该功能会失控", "Images/Buff_88", "禁用传送"),
            };

            return uis;
        }
    }
}
