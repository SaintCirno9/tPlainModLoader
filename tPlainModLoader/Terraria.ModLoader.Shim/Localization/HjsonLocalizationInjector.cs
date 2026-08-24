using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Terraria.Localization;

namespace Terraria.ModLoader.Localization
{
    /// <summary>
    /// 轻量 HJSON / JSON 本地化词条解析与注入器
    /// </summary>
    public static class HjsonLocalizationInjector
    {
        public static void InjectHjson(string modName, string hjsonText)
        {
            if (string.IsNullOrWhiteSpace(hjsonText)) return;

            try
            {
                var dict = ParseFlatEntries(hjsonText);
                var lm = LanguageManager.Instance;
                if (lm == null || lm._localizedTexts == null) return;

                int count = 0;
                foreach (var kvp in dict)
                {
                    string key = kvp.Key;
                    if (!key.StartsWith("Mods.", StringComparison.OrdinalIgnoreCase))
                    {
                        key = $"Mods.{modName}.{key}";
                    }

                    if (lm._localizedTexts.TryGetValue(key, out var localizedText))
                    {
                        localizedText.SetValue(kvp.Value);
                    }
                    else
                    {
                        lm._localizedTexts[key] = new LocalizedText(key, kvp.Value);
                    }
                    count++;
                }

                Console.WriteLine($"[HjsonInjector] 为模组 [{modName}] 成功注入 {count} 条本地化词条。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HjsonInjector] 注入本地化词条异常: {ex.Message}");
            }
        }

        public static Dictionary<string, string> ParseFlatEntries(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var stack = new List<string>();

            bool inMultiline = false;
            string multilineKey = null;
            var multilineSb = new StringBuilder();

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex].Trim();

                if (inMultiline)
                {
                    if (line.EndsWith("'''"))
                    {
                        inMultiline = false;
                        string chunk = line.Substring(0, line.Length - 3).Trim();
                        if (chunk.Length > 0) multilineSb.AppendLine(chunk);
                        string fullKey = BuildKey(stack, multilineKey);
                        result[fullKey] = multilineSb.ToString().TrimEnd('\r', '\n');
                        multilineSb.Clear();
                    }
                    else
                    {
                        multilineSb.AppendLine(lines[lineIndex]);
                    }
                    continue;
                }

                // 移除单行注释
                if (line.StartsWith("#") || line.StartsWith("//")) continue;
                int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                if (commentIndex > 0) line = line.Substring(0, commentIndex).Trim();

                if (string.IsNullOrWhiteSpace(line)) continue;

                // 检查闭合大括号
                if (line == "}" || line.StartsWith("},"))
                {
                    if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
                    continue;
                }

                // 多行字符串开始
                if (line.Contains(": '''") || line.Contains(":'''"))
                {
                    int colon = line.IndexOf(':');
                    multilineKey = line.Substring(0, colon).Trim(' ', '"', '\'');
                    inMultiline = true;
                    multilineSb.Clear();
                    int afterQuotes = line.IndexOf("'''", colon) + 3;
                    if (afterQuotes < line.Length)
                    {
                        string remain = line.Substring(afterQuotes);
                        if (remain.EndsWith("'''"))
                        {
                            inMultiline = false;
                            remain = remain.Substring(0, remain.Length - 3);
                            string fullKey = BuildKey(stack, multilineKey);
                            result[fullKey] = remain.Trim();
                        }
                        else
                        {
                            multilineSb.AppendLine(remain);
                        }
                    }
                    continue;
                }

                // 检查对象开启：Key: { 或 Key: {
                if (line.EndsWith("{"))
                {
                    int colon = line.IndexOf(':');
                    string keyName = (colon >= 0 ? line.Substring(0, colon) : line.Substring(0, line.Length - 1)).Trim(' ', '"', '\'');
                    if (!string.IsNullOrEmpty(keyName))
                    {
                        stack.Add(keyName);
                    }
                    continue;
                }

                // 标准键值对 Key: Value
                int colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    string key = line.Substring(0, colonIdx).Trim(' ', '"', '\'');
                    string value = line.Substring(colonIdx + 1).Trim();

                    // 去除行末逗号
                    if (value.EndsWith(",")) value = value.Substring(0, value.Length - 1).Trim();

                    // 去除包裹引号
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        if (value.Length >= 2) value = value.Substring(1, value.Length - 2);
                    }

                    // 转义符处理
                    value = Regex.Unescape(value);

                    string fullKey = BuildKey(stack, key);
                    result[fullKey] = value;
                }
            }

            return result;
        }

        private static string BuildKey(List<string> stack, string leaf)
        {
            if (stack == null || stack.Count == 0) return leaf;
            return string.Join(".", stack) + "." + leaf;
        }
    }
}
