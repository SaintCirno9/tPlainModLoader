using CommandHelp;
using HarmonyLib;
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
    /// 旗帜与图鉴规则（对齐 ImproveGame 语义）：
    /// 1. 旗帜杀敌数倍率：获得旗帜所需击杀数 = 原版需求 × 倍率（0.1 = 10%，10 = 1000%）；
    /// 2. 图鉴一次击杀全解锁：击杀一次即显示该条目的全部信息（掉落/简介等）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class BannerAndBestiary
    {
        public static GetSetReset<bool> EnableBannerRequirement = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> BannerRequirementMultiplier = new GetSetReset<float>(1f, 1f, v => v < 0.05f ? 0.05f : (v > 20f ? 20f : v));
        public static GetSetReset<bool> EnableBestiaryQuickUnlock = new GetSetReset<bool>(false, false);

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
    }

    /// <summary>
    /// 旗帜杀敌数倍率：拦截 NPC.CountKillForBannersAndDropThem（内部调用 BannerSystem.AddNPCKillBy，
    /// BannerSystem.cs:243 读取 ItemID.Sets.KillsToBanner 判定是否掉旗），Prefix 临时将需求 × 倍率、Postfix 恢复。
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.CountKillForBannersAndDropThem))]
    internal static class Patch_BannerRequirement
    {
        private static int modifiedBannerItem = -1;
        private static int originalKills = 0;

        [HarmonyPrefix]
        internal static void Prefix(NPC __instance)
        {
            if (!BannerAndBestiary.EnableBannerRequirement.val) return;
            int bannerItem = BannerSystem.BannerToItem(BannerSystem.NPCtoBanner(__instance.BannerID()));
            if (bannerItem <= 0) return;
            modifiedBannerItem = bannerItem;
            originalKills = ItemID.Sets.KillsToBanner[bannerItem];
            int newKills = (int)(originalKills * BannerAndBestiary.BannerRequirementMultiplier.val);
            ItemID.Sets.KillsToBanner[bannerItem] = newKills < 1 ? 1 : newKills;
        }

        /// <summary>Finalizer：无论原方法正常返回还是抛异常都恢复 KillsToBanner（全局数组，残留即永久污染）</summary>
        [HarmonyFinalizer]
        internal static void Finalizer()
        {
            if (modifiedBannerItem < 0) return;
            ItemID.Sets.KillsToBanner[modifiedBannerItem] = originalKills;
            modifiedBannerItem = -1;
        }
    }

    /// <summary>
    /// 图鉴快速解锁：击杀登记后把该条目击杀数直接拉到"完整解锁"所需值。
    /// 依据 CommonEnemyUICollectionInfoProvider.GetUnlockStateByKillCount：killCount &gt;= fullKillCountNeeded 即 CanShowDropsWithDropRates_4（全信息）。
    /// </summary>
    [HarmonyPatch(typeof(NPCKillsTracker), nameof(NPCKillsTracker.RegisterKill))]
    internal static class Patch_BestiaryQuickUnlock
    {
        [HarmonyPostfix]
        internal static void Postfix(NPC npc)
        {
            if (!BannerAndBestiary.EnableBestiaryQuickUnlock.val || npc == null) return;
            string id = npc.GetBestiaryCreditId();
            int needed = CommonEnemyUICollectionInfoProvider.GetKillCountNeeded(id);
            Main.BestiaryTracker.Kills.SetKillCountDirectly(id, needed);
        }
    }
}
