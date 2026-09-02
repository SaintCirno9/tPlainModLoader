using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using TPML.Core.Pinyin;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 原版制作系统、向导配方查询与旅程模式物品搜索的拼音多模匹配强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal static class Patch_CreativeAndCraftingSearch
    {
        private static bool _hooksInitialized = false;

        /// <summary>集中注册搜索过滤器强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_ItemFilters.BySearch.FitsFilter += Hook_ItemSearchFitsFilter;
            On_Filters.BySearch.FitsFilter += Hook_BestiarySearchFitsFilter;

            _hooksInitialized = true;
        }

        private static bool Hook_ItemSearchFitsFilter(On_ItemFilters.BySearch.orig_FitsFilter orig, ItemFilters.BySearch self, Item entry)
        {
            return FitsFilterPrefix(self, entry);
        }

        private static bool Hook_BestiarySearchFitsFilter(On_Filters.BySearch.orig_FitsFilter orig, Filters.BySearch self, BestiaryEntry entry)
        {
            return BestiaryFitsFilterPrefix(self, entry);
        }

        /// <summary>
        /// 拦截原版 ItemFilters.BySearch.FitsFilter，支持制作系统、向导配方和旅程模式物品的拼音与首字母搜索
        /// </summary>
        public static bool FitsFilterPrefix(ItemFilters.BySearch __instance, Item entry)
        {
            string search = __instance._search;
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            // 1. 优先匹配物品名称与本地化中文名称（支持拼音全拼与首字母缩写）
            string localizedName = Lang.GetItemNameValue(entry.type);
            if (PinyinHelper.Matches(localizedName, search) || PinyinHelper.Matches(entry.Name, search))
            {
                return true;
            }

            // 2. 匹配物品详细描述与 Tooltip 文本行
            int numLines = 1;
            float knockBack = entry.knockBack;
            int stack = entry.stack;
            entry.stack = 1;
            Main.MouseText_DrawItemTooltip_GetLinesInfo(entry, ref __instance._unusedYoyoLogo, knockBack, ref numLines, __instance._toolTipLines, __instance._unusedColor);
            entry.stack = stack;

            for (int i = 0; i < numLines; i++)
            {
                if (__instance._toolTipLines[i] != null && PinyinHelper.Matches(__instance._toolTipLines[i], search))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 拦截原版怪物图鉴 Bestiary.Filters.BySearch.FitsFilter，支持怪物图鉴的拼音与首字母搜索
        /// </summary>
        public static bool BestiaryFitsFilterPrefix(Filters.BySearch __instance, BestiaryEntry entry)
        {
            string search = __instance._search;
            if (string.IsNullOrWhiteSpace(search))
            {
                return true;
            }

            BestiaryUICollectionInfo info = entry.UIInfoProvider.GetEntryUICollectionInfo();
            for (int i = 0; i < entry.Info.Count; i++)
            {
                if (entry.Info[i] is IProvideSearchFilterString provideSearchFilterString)
                {
                    string searchString = provideSearchFilterString.GetSearchString(ref info);
                    if (searchString != null && PinyinHelper.Matches(searchString, search))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
