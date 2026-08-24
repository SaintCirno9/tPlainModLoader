using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.PlayerModify
{
    internal class ValSet : PatchPlayer
    {
        public static GetSetReset<bool> damage = new GetSetReset<bool>();
        public static GetSetReset<float> damage_val = new GetSetReset<float>();
        public static GetSetReset<bool> armorPenetration = new GetSetReset<bool>();
        public static GetSetReset<int> armorPenetration_val = new GetSetReset<int>();
        public static GetSetReset<bool> maxMinions = new GetSetReset<bool>();
        public static GetSetReset<int> maxMinions_val = new GetSetReset<int>();
        public static GetSetReset<bool> endurance = new GetSetReset<bool>();
        public static GetSetReset<float> endurance_val = new GetSetReset<float>();
        public static GetSetReset<bool> grabRange = new GetSetReset<bool>();
        public static GetSetReset<int> grabRange_val = new GetSetReset<int>(20, 20, v => v < 0 ? 0 : v);

        public override void UpdateArmorSetsPostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer) return;

            if (damage.val)
            {
                This.magicDamage += damage_val.val;
                This.meleeDamage += damage_val.val;
                This.rangedDamage += damage_val.val;
                This.minionDamage += damage_val.val;
            }

            if (armorPenetration.val)
            {
                This.armorPenetration = armorPenetration_val.val;
            }

            if (maxMinions.val)
            {
                This.maxMinions = maxMinions_val.val;
                This.maxTurrets = maxMinions_val.val;
            }

            if (endurance.val)
            {
                This.endurance = endurance_val.val;
            }

            if (grabRange.val)
            {
                Player.defaultItemGrabRange = grabRange_val.val * 16;
            }
            else
            {
                Player.defaultItemGrabRange = 42;
            }
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get1("damage", damage, damage_val, new CommandFloat()),
                CommandBuild.get1("armorPenetration", armorPenetration, armorPenetration_val, new CommandInt()),
                CommandBuild.get1("maxMinions", maxMinions, maxMinions_val, new CommandInt()),
                CommandBuild.get1("endurance", endurance, endurance_val, new CommandFloat()),
                CommandBuild.get1("grabRange", grabRange, grabRange_val, new CommandInt()),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get1(damage, damage_val, float.Parse, "<float>", "Images/Buff_180", "伤害倍率"),
                UIBuild.get1(armorPenetration, armorPenetration_val, int.Parse, "<float>", "Images/Buff_159", "穿甲"),
                UIBuild.get1(maxMinions, maxMinions_val, int.Parse, "<int>", "Images/Buff_150", "召唤物上限"),
                UIBuild.get1(endurance, endurance_val, float.Parse, "<float>", "Images/Buff_114", "减伤"),
                UIBuild.get1(grabRange, grabRange_val, int.Parse, "掉落物自动吸附拾取范围(格)<int>", "Images/Item_5010", "拾取范围"),
            };

            return uis;
        }
    }
}
