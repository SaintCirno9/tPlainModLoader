using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using TPML;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.Function1
{
    /// <summary>
    /// 玩家能力作弊功能集合（无敌不死、法力满溢、地图传送、飞行等）
    /// 作者: SaintCirno9
    /// </summary>
    internal class PlayerCheatFunctions : TPML.Content.ModPlayer
    {
        public static GetSetReset<bool> noDead = new GetSetReset<bool>();
        public static GetSetReset<bool> manaMax = new GetSetReset<bool>();
        
        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer) return;

            //回复最大血量
            if (noDead.val) This.statLife = This.statLifeMax2;
            //回复最大魔力
            if (manaMax.val) This.statMana = This.statManaMax2;
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>();
            cos.Add(CommandBuild.get2("noDead", noDead));
            if (!ContentPatch.NoPublic) cos.AddRange(Function_noDead2.GetCO());
            cos.AddRange(Function_mapTeleport.GetCO());
            cos.Add(CommandBuild.get2("manaMax", manaMax));
            cos.AddRange(Function_displayPlay.GetCO());
            cos.AddRange(Function_fly.GetCO());
            if (!ContentPatch.NoPublic) cos.AddRange(Function_fly2.GetCO());
            cos.AddRange(Function_lifeRecoverProj.GetCO());
            cos.AddRange(Function_noTeleport.GetCO());
            cos.AddRange(Function_buff.GetCO());
            cos.AddRange(Function_skin.GetCO());

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>();
            uis.Add(UIBuild.get2(noDead, null, "Images/Buff_48", "不死"));
            if (!ContentPatch.NoPublic) uis.AddRange(Function_noDead2.GetUI());
            uis.AddRange(Function_mapTeleport.GetUI());
            uis.Add(UIBuild.get2(manaMax, null, "Images/Buff_6", "回复最大魔力"));
            uis.AddRange(Function_displayPlay.GetUI());
            uis.AddRange(Function_fly.GetUI());
            if (!ContentPatch.NoPublic) uis.AddRange(Function_fly2.GetUI());
            uis.AddRange(Function_lifeRecoverProj.GetUI());
            uis.AddRange(Function_noTeleport.GetUI());
            uis.AddRange(Function_buff.GetUI());
            uis.AddRange(Function_skin.GetUI());

            return uis;
        }
    }

    /// <summary>
    /// 旧版同名类兼容垫片
    /// </summary>
    [System.Obsolete("请改用 PlayerCheatFunctions")]
    internal class Function : PlayerCheatFunctions
    {
        public override void UpdatePrefix(Player This, int playerI) { }
    }
}
