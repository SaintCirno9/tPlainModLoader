using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using TPML.Core.Pinyin;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 原版制作系统、向导配方查询与旅程模式物品搜索的拼音多模匹配补丁
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_CreativeAndCraftingSearch
    {
        /// <summary>
        /// 拦截原版 ItemFilters.BySearch.FitsFilter，支持制作系统、向导配方和旅程模式物品的拼音与首字母搜索
        /// </summary>
        [HarmonyPatch(typeof(ItemFilters.BySearch), nameof(ItemFilters.BySearch.FitsFilter))]
        [HarmonyPrefix]
        public static bool FitsFilterPrefix(
            ItemFilters.BySearch __instance,
            Item entry,
            ref bool __result,
            string[] ____toolTipLines,
            ref int ____unusedYoyoLogo,
            Color[] ____unusedColor,
            string ____search)
        {
            if (string.IsNullOrWhiteSpace(____search))
            {
                __result = true;
                return false;
            }

            // 1. 优先匹配物品名称与本地化中文名称（支持拼音全拼与首字母缩写）
            string localizedName = Lang.GetItemNameValue(entry.type);
            if (PinyinHelper.Matches(localizedName, ____search) || PinyinHelper.Matches(entry.Name, ____search))
            {
                __result = true;
                return false;
            }

            // 2. 匹配物品详细描述与 Tooltip 文本行
            int numLines = 1;
            float knockBack = entry.knockBack;
            int stack = entry.stack;
            entry.stack = 1;
            Main.MouseText_DrawItemTooltip_GetLinesInfo(entry, ref ____unusedYoyoLogo, knockBack, ref numLines, ____toolTipLines, ____unusedColor);
            entry.stack = stack;

            for (int i = 0; i < numLines; i++)
            {
                if (____toolTipLines[i] != null && PinyinHelper.Matches(____toolTipLines[i], ____search))
                {
                    __result = true;
                    return false;
                }
            }

            __result = false;
            return false;
        }

        /// <summary>
        /// 拦截原版怪物图鉴 Bestiary.Filters.BySearch.FitsFilter，支持怪物图鉴的拼音与首字母搜索
        /// </summary>
        [HarmonyPatch(typeof(Filters.BySearch), nameof(Filters.BySearch.FitsFilter))]
        [HarmonyPrefix]
        public static bool BestiaryFitsFilterPrefix(
            Filters.BySearch __instance,
            BestiaryEntry entry,
            ref bool __result,
            string ____search)
        {
            if (string.IsNullOrWhiteSpace(____search))
            {
                __result = true;
                return false;
            }

            BestiaryUICollectionInfo info = entry.UIInfoProvider.GetEntryUICollectionInfo();
            for (int i = 0; i < entry.Info.Count; i++)
            {
                if (entry.Info[i] is IProvideSearchFilterString provideSearchFilterString)
                {
                    string searchString = provideSearchFilterString.GetSearchString(ref info);
                    if (searchString != null && PinyinHelper.Matches(searchString, ____search))
                    {
                        __result = true;
                        return false;
                    }
                }
            }

            __result = false;
            return false;
        }
    }
}
