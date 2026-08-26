using CommandHelp;
using System;
using System.Collections.Generic;
using tContentPatch.Content.UI.ModSet;
using Terraria.UI;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋配置中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class AccessoryBagConfig
    {
        public static GetSetReset<bool> EnablePassive = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> PreventBagDuplicates = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> PreventPlayerBagDuplicates = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableMaxDuplicateAccessory = new GetSetReset<bool>(false, false);
        public static GetSetReset<int> MaxDuplicateAccessory = new GetSetReset<int>(2, 2);
        public static GetSetReset<bool> EnableEffectiveSlotsLimit = new GetSetReset<bool>(false, false);
        public static GetSetReset<int> EffectiveSlots = new GetSetReset<int>(10, 10);
        public static GetSetReset<int> TotalSlots = new GetSetReset<int>(40, 40);
        public static GetSetReset<int> SlotsPerRow = new GetSetReset<int>(10, 10);
        public static GetSetReset<bool> ApplyBaseStats = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableAccessoryEffects = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> AllowPrefixRoll = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableArmorSetBonuses = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> HighlightActiveSetBonusTooltips = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("accessoryBagPassive", EnablePassive),
                CommandBuild.get2("accessoryBagArmorSets", EnableArmorSetBonuses),
                CommandBuild.get2("accessoryBagHighlightSets", HighlightActiveSetBonusTooltips),
                CommandBuild.get2("accessoryBagPreventBagDup", PreventBagDuplicates),
                CommandBuild.get2("accessoryBagPreventPlayerDup", PreventPlayerBagDuplicates),
                CommandBuild.get2("accessoryBagEnableMaxDup", EnableMaxDuplicateAccessory),
                CommandBuild.get1("accessoryBagMaxDup", EnableMaxDuplicateAccessory, MaxDuplicateAccessory, new CommandInt()),
                CommandBuild.get2("accessoryBagEnableEffectiveLimit", EnableEffectiveSlotsLimit),
                CommandBuild.get1("accessoryBagEffectiveSlots", EnableEffectiveSlotsLimit, EffectiveSlots, new CommandInt()),
                CommandBuild.get1("accessoryBagTotalSlots", new GetSetReset<bool>(true, true), TotalSlots, new CommandInt()),
                CommandBuild.get1("accessoryBagSlotsPerRow", new GetSetReset<bool>(true, true), SlotsPerRow, new CommandInt())
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnablePassive, "饰品袋属性生效：袋内饰品与装备属性无需穿在身上直接生效", "Images/Item_158", "饰品袋属性生效"),
                UIBuild.get2(EnableArmorSetBonuses, "装备套装加成：当饰品袋中凑齐整套防具时，自动激活套装奖励（支持多套装共存）", "Images/Item_2763", "装备套装加成"),
                UIBuild.get2(HighlightActiveSetBonusTooltips, "套装提示点亮：当饰品袋满足套装时，防具提示信息自动点亮为原版已激活样式", "Images/Item_2764", "套装提示点亮"),
                UIBuild.get2(PreventBagDuplicates, "包内防重复：禁止在同一个饰品袋中放入同种饰品或装备", "Images/UI/InfoIcon_0", "包内防重复"),
                UIBuild.get2(PreventPlayerBagDuplicates, "角色互斥防重：禁止放入角色身上已装备的同种物品", "Images/UI/InfoIcon_1", "角色互斥防重"),
                UIBuild.get1(EnableEffectiveSlotsLimit, EffectiveSlots, int.Parse, "限制饰品袋前 N 个槽位的物品属性生效（其余槽位仅作收纳）", "Images/UI/InfoIcon_5", "生效槽位限制"),
                UIBuild.get1(new GetSetReset<bool>(true, true), TotalSlots, int.Parse, "饰品袋总槽位数（默认 40，支持 10~150 格）", "Images/Item_3813", "饰品袋总容量")
            };
        }
    }
}
