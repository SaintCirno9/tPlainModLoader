using System;
using System.Collections.Concurrent;

namespace TPML.Core.Pinyin
{
    /// <summary>
    /// 拼音搜索与多模匹配辅助类（带高并发内存缓存）
    /// 作者: SaintCirno9
    /// </summary>
    public static class PinyinHelper
    {
        private class PinyinCacheEntry
        {
            public string CleanRaw;
            public string PinyinFirst;
            public string PinyinFull;
            public string AutoComplete;
        }

        private static readonly ConcurrentDictionary<string, PinyinCacheEntry> _cache =
            new ConcurrentDictionary<string, PinyinCacheEntry>(StringComparer.Ordinal);

        /// <summary>
        /// 清理空格与常用分隔符并转换为小写
        /// </summary>
        public static string CleanString(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace(" ", "").Replace("\t", "").Replace("-", "").Replace("'", "").ToLowerInvariant();
        }

        private static PinyinCacheEntry GetOrCreateEntry(string sourceText)
        {
            if (sourceText == null) sourceText = string.Empty;
            if (_cache.TryGetValue(sourceText, out var entry))
            {
                return entry;
            }

            string cleanRaw = CleanString(sourceText);
            string pinyinRaw = PinyinConvert.TranslateToPinyin(sourceText);
            string[] parts = pinyinRaw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            var firstLetters = new System.Text.StringBuilder(parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    firstLetters.Append(parts[i][0]);
                }
            }

            string pinyinFirst = CleanString(firstLetters.ToString());
            string pinyinFull = CleanString(pinyinRaw.Replace("|", ""));
            string autoComplete = CleanString($"{firstLetters} {pinyinFull} {sourceText}");

            entry = new PinyinCacheEntry
            {
                CleanRaw = cleanRaw,
                PinyinFirst = pinyinFirst,
                PinyinFull = pinyinFull,
                AutoComplete = autoComplete
            };

            _cache.TryAdd(sourceText, entry);
            return entry;
        }

        /// <summary>
        /// 判断源字符串是否包含搜索词（支持直接包含、全拼包含、首字母缩写包含）
        /// </summary>
        /// <param name="sourceText">源文本（如物品名称、NPC名称或描述）</param>
        /// <param name="searchText">搜索关键词（如 "zsg"、"zuanshi"、"钻石"）</param>
        /// <returns>若匹配成功返回 true</returns>
        public static bool Matches(string sourceText, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)) return true;
            if (string.IsNullOrEmpty(sourceText)) return false;

            // 1. 快速路径：源文本直接包含搜索词（忽略大小写）
            if (sourceText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string cleanSearch = CleanString(searchText);
            if (cleanSearch.Length == 0) return true;

            // 2. 获取源文本的拼音缓存元数据
            var entry = GetOrCreateEntry(sourceText);

            // 3. 原文清洗后包含（忽略标点和空格差异）
            if (entry.CleanRaw.IndexOf(cleanSearch, StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            // 4. 首字母缩写包含（如 "zsg" 匹配 "钻石镐"）
            if (entry.PinyinFirst.IndexOf(cleanSearch, StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            // 5. 全拼连写包含（如 "zuanshi" 或 "shigao" 匹配 "钻石镐"）
            if (entry.PinyinFull.IndexOf(cleanSearch, StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            // 6. 自动补全组合串包含
            if (entry.AutoComplete.IndexOf(cleanSearch, StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 兼容签名：带有已计算的 originalComparison 索引
        /// </summary>
        public static bool Matches(int originalComparison, string sourceText, string searchText)
        {
            if (originalComparison != -1) return true;
            return Matches(sourceText, searchText);
        }

        /// <summary>
        /// 扩展方法：字符串快速拼音匹配
        /// </summary>
        public static bool MatchesPinyin(this string source, string query)
        {
            return Matches(source, query);
        }
    }
}
