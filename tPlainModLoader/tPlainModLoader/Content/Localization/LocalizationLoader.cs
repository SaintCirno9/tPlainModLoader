using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.Localization;

namespace TPML.Content.Localization
{
    /// <summary>
    /// TPML 引擎级 HJSON 本地化加载器
    /// 自动从模组程序集中解析 HJSON 并注入原版 LanguageManager
    /// </summary>
    public static class LocalizationLoader
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void LoadModLocalization(Mod mod)
        {
            if (mod == null || mod.Code == null) return;

            try
            {
                var names = mod.Code.GetManifestResourceNames();
                foreach (var name in names)
                {
                    if (name.EndsWith(".hjson", StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream stream = mod.Code.GetManifestResourceStream(name))
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
                }

                InjectToVanillaLanguage();
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[LocalizationLoader] 载入模组 [{mod.Name}] 本地化异常: {ex.Message}");
            }
        }

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

                if (line.StartsWith("'''") && !string.IsNullOrEmpty(multilineKey))
                {
                    inMultilineString = true;
                    multilineLines.Clear();
                    if (line.Length > 3) multilineLines.Add(line.Substring(3));
                    i++;
                    continue;
                }

                if (line.EndsWith("{"))
                {
                    string header = line.Substring(0, line.Length - 1).Trim();
                    string[] segs = header.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in segs)
                    {
                        string clean = s.Trim().Trim('"', '\'');
                        if (!string.IsNullOrEmpty(clean)) pathStack.Add(clean);
                    }
                    i++;
                    continue;
                }

                if (line == "}")
                {
                    if (pathStack.Count > 0) pathStack.RemoveAt(pathStack.Count - 1);
                    i++;
                    continue;
                }

                int colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    string key = line.Substring(0, colonIdx).Trim().Trim('"', '\'');
                    string val = line.Substring(colonIdx + 1).Trim();

                    if (val.StartsWith("'''"))
                    {
                        string currentPath = pathStack.Count > 0 ? string.Join(".", pathStack) + "." + key : key;
                        multilineKey = currentPath;
                        inMultilineString = true;
                        multilineLines.Clear();
                        string rest = val.Substring(3);
                        if (rest.EndsWith("'''") && rest.Length >= 3)
                        {
                            target[currentPath] = rest.Substring(0, rest.Length - 3).Trim();
                            inMultilineString = false;
                        }
                        else if (!string.IsNullOrEmpty(rest))
                        {
                            multilineLines.Add(rest);
                        }
                        i++;
                        continue;
                    }

                    if (val.EndsWith("{"))
                    {
                        pathStack.Add(key);
                        i++;
                        continue;
                    }

                    if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
                    {
                        val = val.Substring(1, val.Length - 2);
                    }

                    string fullKey = pathStack.Count > 0 ? string.Join(".", pathStack) + "." + key : key;
                    target[fullKey] = val;
                }

                i++;
            }
        }

        public static void InjectToVanillaLanguage()
        {
            try
            {
                var lm = LanguageManager.Instance;
                if (lm == null) return;

                var categoryField = typeof(LanguageManager).GetField("_categoryGroupedTranslations", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var localizedTextsField = typeof(LanguageManager).GetField("_localizedTexts", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                var categoryDict = categoryField?.GetValue(lm) as Dictionary<string, List<string>>;
                var localizedDict = localizedTextsField?.GetValue(lm) as Dictionary<string, LocalizedText>;

                foreach (var kvp in _translations)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;

                    if (localizedDict != null)
                    {
                        if (localizedDict.TryGetValue(key, out LocalizedText existing))
                        {
                            typeof(LocalizedText).GetField("Value", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.SetValue(existing, value);
                            typeof(LocalizedText).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.SetValue(existing, value);
                        }
                        else
                        {
                            LocalizedText newText = (LocalizedText)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(LocalizedText));
                            typeof(LocalizedText).GetField("Key", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.SetValue(newText, key);
                            typeof(LocalizedText).GetField("<Key>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.SetValue(newText, key);
                            typeof(LocalizedText).GetField("Value", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.SetValue(newText, value);
                            typeof(LocalizedText).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.SetValue(newText, value);
                            localizedDict[key] = newText;

                            if (categoryDict != null)
                            {
                                int dotIdx = key.IndexOf('.');
                                string category = dotIdx > 0 ? key.Substring(0, dotIdx) : "Mods";
                                if (!categoryDict.TryGetValue(category, out List<string> keys))
                                {
                                    keys = new List<string>();
                                    categoryDict[category] = keys;
                                }
                                if (!keys.Contains(key)) keys.Add(key);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[LocalizationLoader] 注入原版语言系统异常: {ex.Message}");
            }
        }
    }
}
