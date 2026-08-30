using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.Function1
{
    internal class Function_noDead2 : Mod
    {
        public static GetSetReset<bool> noDead2 = new GetSetReset<bool>();

        // M2：弃用 IAddPatch，改用 MonoMod.HookGen 的 On_ 门面（tML 标准做法）
        public override void Load()
        {
            On.Terraria.Player.KillMe += (orig, self, damageSource, dmg, hitDirection, pvp) =>
            {
                if (self == Main.LocalPlayer && noDead2.val) return; // 跳过（prefix 返回 false）
                orig(self, damageSource, dmg, hitDirection, pvp);
            };
        }

        public static bool KillMe(Player __instance, Terraria.DataStructures.PlayerDeathReason damageSource, double dmg, int hitDirection, bool pvp = false)
        {
            if (__instance != Main.LocalPlayer) return true;
            return noDead2.val == false;
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("noDead2", noDead2),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get2(noDead2, "重新加载模组后该功能会失控", "Images/Buff_58", "不死2"),
            };

            return uis;
        }
    }
}
