using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 旗帜与图鉴规则门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：
    /// 1. 旗帜杀敌数倍率：获得旗帜所需击杀数 = 原版需求 × 倍率（0.1 = 10%，10 = 1000%）；
    /// 2. 图鉴一次击杀全解锁：击杀一次即显示该条目的全部信息（掉落/简介等）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class BannerAndBestiaryHooks
    {
        public static GetSetReset<bool> EnableBannerRequirement = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> BannerRequirementMultiplier = new GetSetReset<float>(1f, 1f, v => v < 0.05f ? 0.05f : (v > 20f ? 20f : v));
        public static GetSetReset<bool> EnableBestiaryQuickUnlock = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_NPC.CountKillForBannersAndDropThem += Hook_CountKillForBannersAndDropThem;
            On_NPCKillsTracker.RegisterKill += Hook_RegisterKill;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_NPC.CountKillForBannersAndDropThem -= Hook_CountKillForBannersAndDropThem;
            On_NPCKillsTracker.RegisterKill -= Hook_RegisterKill;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get1("bannerRequirement", EnableBannerRequirement, BannerRequirementMultiplier),
                CommandBuild.get2("bestiaryQuickUnlock", EnableBestiaryQuickUnlock)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get1(EnableBannerRequirement, BannerRequirementMultiplier, float.Parse, "获得旗帜所需击杀数倍率：0.1=10%（5 只蓝史莱姆即可），1=原版，10=1000%<float>", "Images/Item_2990", "旗帜杀敌数倍率"),
                UIBuild.get2(EnableBestiaryQuickUnlock, "生物图鉴条目只需击杀 1 次即显示全部信息（掉落/简介等）", "Images/Item_2843", "图鉴一次击杀全解锁")
            };
        }

        private static void Hook_CountKillForBannersAndDropThem(On_NPC.orig_CountKillForBannersAndDropThem orig, NPC self)
        {
            int modifiedBannerItem = -1;
            int originalKills = 0;

            if (EnableBannerRequirement.val)
            {
                int bannerItem = BannerSystem.BannerToItem(BannerSystem.NPCtoBanner(self.BannerID()));
                if (bannerItem > 0)
                {
                    modifiedBannerItem = bannerItem;
                    originalKills = ItemID.Sets.KillsToBanner[bannerItem];
                    int newKills = (int)(originalKills * BannerRequirementMultiplier.val);
                    ItemID.Sets.KillsToBanner[bannerItem] = newKills < 1 ? 1 : newKills;
                }
            }

            try
            {
                orig(self);
            }
            finally
            {
                if (modifiedBannerItem >= 0)
                {
                    ItemID.Sets.KillsToBanner[modifiedBannerItem] = originalKills;
                }
            }
        }

        private static void Hook_RegisterKill(On_NPCKillsTracker.orig_RegisterKill orig, NPCKillsTracker self, NPC npc)
        {
            orig(self, npc);

            if (!EnableBestiaryQuickUnlock.val || npc == null) return;
            string id = npc.GetBestiaryCreditId();
            int needed = CommonEnemyUICollectionInfoProvider.GetKillCountNeeded(id);
            Main.BestiaryTracker.Kills.SetKillCountDirectly(id, needed);
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class BannerAndBestiary
    {
        public static GetSetReset<bool> EnableBannerRequirement => BannerAndBestiaryHooks.EnableBannerRequirement;
        public static GetSetReset<float> BannerRequirementMultiplier => BannerAndBestiaryHooks.BannerRequirementMultiplier;
        public static GetSetReset<bool> EnableBestiaryQuickUnlock => BannerAndBestiaryHooks.EnableBestiaryQuickUnlock;

        public static List<CommandObject> GetCO() => BannerAndBestiaryHooks.GetCO();
        public static List<UIElement> GetUI() => BannerAndBestiaryHooks.GetUI();
    }
}
