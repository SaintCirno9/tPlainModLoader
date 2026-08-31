using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Creative;
using TPML.Content.Engine;
using TPML.Core.Pinyin;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// 原版制作系统、向导配方查询与旅程模式物品搜索的拼音多模匹配补丁
    /// 作者: SaintCirno9
    /// M2 迁移说明：原 Harmony 前缀经 ____字段 注入读取实例私有字段；
    /// MonoMod 无字段注入机制，改为经 Publicizer 直连字段（强类型，零反射）。
    /// </summary>
    internal static class Patch_CreativeAndCraftingSearch
    {
        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // ItemFilters.BySearch.FitsFilter(Item)（实例，返回 bool，原方法被完全替换）
            HookRegistry.Add(MethodLookup.Instance(typeof(ItemFilters.BySearch), "FitsFilter", typeof(Item)),
                (Func<Func<ItemFilters.BySearch, Item, bool>, ItemFilters.BySearch, Item, bool>)((orig, self, entry) =>
                {
                    return FitsFilterPrefix(self, entry);
                }));

            // Filters.BySearch.FitsFilter(BestiaryEntry)（实例，返回 bool，原方法被完全替换）
            HookRegistry.Add(MethodLookup.Instance(typeof(Filters.BySearch), "FitsFilter", typeof(BestiaryEntry)),
                (Func<Func<Filters.BySearch, BestiaryEntry, bool>, Filters.BySearch, BestiaryEntry, bool>)((orig, self, entry) =>
                {
                    return BestiaryFitsFilterPrefix(self, entry);
                }));
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
