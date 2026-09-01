using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.Localization;

namespace RecipeBrowser.Common
{
    /// <summary>
    /// RecipeBrowser 本地化词条管理器
    /// 自动解析 zh-Hans.hjson 并注入原版 LanguageManager
    /// 作者: SaintCirno9
    /// </summary>
    public static class RBLanguage
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            _translations.Clear();

            // 1. 优先载入 en-US 作为 base，然后用 zh-Hans 覆盖
            LoadHjsonFromResource("RecipeBrowser.Resources.Localization.en-US.hjson");
            LoadHjsonFromResource("RecipeBrowser.Resources.Localization.zh-Hans.hjson");

            // 2. 注入原版 LanguageManager
            InjectToVanillaLanguage();
        }

        private static void LoadHjsonFromResource(string resName)
        {
            try
            {
                using (Stream stream = typeof(RBLanguage).Assembly.GetManifestResourceStream(resName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
                        {
                            string text = reader.ReadToEnd();
                            ParseHjson(text, _translations);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 极简层级 HJSON 解析器，提取所有键值对并展开为 dot-path (如 Mods.RecipeBrowser.Keybinds.Toggle.DisplayName)
        /// </summary>
        public static void ParseHjson(string text, Dictionary<string, string> target)
        {
            if (string.IsNullOrEmpty(text)) return;

            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            List<string> pathStack = new List<string>();

            bool inMultilineString = false;
            string multilineKey = null;
            List<string> multilineLines = new List<string>();

            int i = 0;
            while (i < lines.Length)
            {
                string rawLine = lines[i];
                string line = rawLine.Trim();

                if (inMultilineString)
                {
                    if (line.EndsWith("'''"))
                    {
                        int endIdx = rawLine.LastIndexOf("'''", StringComparison.Ordinal);
                        if (endIdx >= 0) multilineLines.Add(rawLine.Substring(0, endIdx));
                        string fullValue = string.Join("\n", multilineLines).Trim();
                        if (!string.IsNullOrEmpty(multilineKey))
                        {
                            target[multilineKey] = fullValue;
                            string strippedKey = StripPrefix(multilineKey);
                            if (!string.IsNullOrEmpty(strippedKey)) target[strippedKey] = fullValue;
                        }
                        inMultilineString = false;
                        multilineKey = null;
                        multilineLines.Clear();
                    }
                    else
                    {
                        multilineLines.Add(rawLine);
                    }
                    i++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                {
                    i++;
                    continue;
                }

                // 统计花括号闭合
                int closeCount = 0;
                while (line.EndsWith("}"))
                {
                    closeCount++;
                    line = line.Substring(0, line.Length - 1).Trim();
                }

                if (string.IsNullOrEmpty(line))
                {
                    for (int c = 0; c < closeCount; c++)
                    {
                        if (pathStack.Count > 0) pathStack.RemoveAt(pathStack.Count - 1);
                    }
                    i++;
                    continue;
                }

                if (line.Contains(":"))
                {
                    int colonIdx = line.IndexOf(':');
                    string k = line.Substring(0, colonIdx).Trim().Trim('"', '\'');
                    string v = line.Substring(colonIdx + 1).Trim();

                    if (v.StartsWith("'''"))
                    {
                        string keyPath = BuildPath(pathStack, k);
                        if (v.Length > 3 && v.EndsWith("'''"))
                        {
                            string content = v.Substring(3, v.Length - 6);
                            target[keyPath] = content;
                            string stripped = StripPrefix(keyPath);
                            if (!string.IsNullOrEmpty(stripped)) target[stripped] = content;
                        }
                        else
                        {
                            inMultilineString = true;
                            multilineKey = keyPath;
                            multilineLines.Clear();
                            if (v.Length > 3) multilineLines.Add(v.Substring(3));
                        }
                    }
                    else if (v == "{")
                    {
                        pathStack.Add(k);
                    }
                    else if (v == "")
                    {
                        // 检查下一有效行是否为多行三引号或左花括号
                        int nextI = i + 1;
                        while (nextI < lines.Length)
                        {
                            string nextL = lines[nextI].Trim();
                            if (!string.IsNullOrWhiteSpace(nextL) && !nextL.StartsWith("#") && !nextL.StartsWith("//"))
                                break;
                            nextI++;
                        }
                        if (nextI < lines.Length && lines[nextI].Trim().StartsWith("'''"))
                        {
                            inMultilineString = true;
                            multilineKey = BuildPath(pathStack, k);
                            multilineLines.Clear();
                            string firstLine = lines[nextI].Trim();
                            if (firstLine.Length > 3) multilineLines.Add(firstLine.Substring(3));
                            i = nextI;
                        }
                        else if (nextI < lines.Length && lines[nextI].Trim() == "{")
                        {
                            pathStack.Add(k);
                            i = nextI;
                        }
                        else
                        {
                            pathStack.Add(k);
                        }
                    }
                    else
                    {
                        v = v.Trim('"', '\'');
                        string keyPath = BuildPath(pathStack, k);
                        target[keyPath] = v;
                        string stripped = StripPrefix(keyPath);
                        if (!string.IsNullOrEmpty(stripped)) target[stripped] = v;
                    }
                }
                else if (line.EndsWith("{"))
                {
                    string k = line.Substring(0, line.Length - 1).Trim().Trim('"', '\'');
                    if (!string.IsNullOrEmpty(k))
                    {
                        pathStack.Add(k);
                    }
                }

                for (int c = 0; c < closeCount; c++)
                {
                    if (pathStack.Count > 0)
                        pathStack.RemoveAt(pathStack.Count - 1);
                }
                i++;
            }
        }

        private static string StripPrefix(string key)
        {
            if (key.StartsWith("Mods.RecipeBrowser.", StringComparison.OrdinalIgnoreCase))
            {
                return key.Substring("Mods.RecipeBrowser.".Length);
            }
            if (key.StartsWith("RecipeBrowser.", StringComparison.OrdinalIgnoreCase))
            {
                return key.Substring("RecipeBrowser.".Length);
            }
            return key;
        }

        private static string BuildPath(List<string> stack, string key)
        {
            if (stack.Count == 0) return key;
            return string.Join(".", stack) + "." + key;
        }

        private static void InjectToVanillaLanguage()
        {
            try
            {
                var lm = LanguageManager.Instance;
                if (lm == null) return;

                foreach (var kv in _translations)
                {
                    string key = kv.Key;
                    string val = kv.Value;

                    // 确保同时有 Mods.RecipeBrowser. 前缀或原样路径
                    if (!key.StartsWith("Mods.", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "Mods.RecipeBrowser." + key;
                    }

                    if (lm._localizedTexts != null)
                    {
                        if (lm._localizedTexts.TryGetValue(key, out var lt))
                        {
                            lt.SetValue(val);
                        }
                        else
                        {
                            lt = new LocalizedText(key, val);
                            lm._localizedTexts[key] = lt;
                        }
                    }
                }
            }
            catch { }
        }

        public static string GetText(string category, string key, params object[] args)
        {
            string fullKey = $"Mods.RecipeBrowser.{category}.{key}";
            string shortKey = $"{category}.{key}";

            if (_translations.TryGetValue(fullKey, out string val) ||
                _translations.TryGetValue(shortKey, out val) ||
                _translations.TryGetValue(key, out val))
            {
                if (args != null && args.Length > 0)
                {
                    try { return string.Format(val, args); } catch { return val; }
                }
                return val;
            }

            // 尝试直接从原版 Language 获取
            try
            {
                if (Language.Exists(fullKey))
                {
                    if (args != null && args.Length > 0)
                    {
                        return Language.GetTextValue(fullKey, args);
                    }
                    return Language.GetTextValue(fullKey);
                }
            }
            catch { }

            return key;
        }

        public static LocalizedText GetLocalizedText(string category, string key)
        {
            string fullKey = $"Mods.RecipeBrowser.{category}.{key}";
            return Language.GetText(fullKey);
        }
    }
}
