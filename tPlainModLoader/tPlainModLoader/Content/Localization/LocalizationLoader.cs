using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

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
                string activeCulture = Language.ActiveCulture?.Name ?? "zh-Hans";

                string enUsRes = null;
                string activeCultureRes = null;

                foreach (var name in names)
                {
                    if (name.EndsWith(".hjson", StringComparison.OrdinalIgnoreCase))
                    {
                        if (name.IndexOf("en-US", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            enUsRes = name;
                        }
                        if (name.IndexOf(activeCulture, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (activeCulture == "zh-Hans" && (name.IndexOf("zh-Hans", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("zh-CN", StringComparison.OrdinalIgnoreCase) >= 0)))
                        {
                            activeCultureRes = name;
                        }
                    }
                }

                // 1. 先加载 en-US 英文基准
                if (enUsRes != null)
                {
                    LoadResourceHjson(mod.Code, enUsRes);
                }

                // 2. 若当前语言不是英文，则加载当前语言文件进行精确覆盖
                if (activeCultureRes != null && !string.Equals(activeCultureRes, enUsRes, StringComparison.OrdinalIgnoreCase))
                {
                    LoadResourceHjson(mod.Code, activeCultureRes);
                }
                else if (enUsRes == null && activeCultureRes == null)
                {
                    // 若无标准命名的多语言资源，加载唯一的 hjson 资源
                    foreach (var name in names)
                    {
                        if (name.EndsWith(".hjson", StringComparison.OrdinalIgnoreCase))
                        {
                            LoadResourceHjson(mod.Code, name);
                        }
                    }
                }

                InjectToVanillaLanguage();
                RefreshAllItemLocalizations();
                ModLoader.Log($"[LocalizationLoader] 成功载入模组 [{mod.Name}] 本地化，当前词条总数={_translations.Count}");
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[LocalizationLoader] 载入模组 [{mod.Name}] 本地化异常: {ex.Message}");
            }
        }

        private static void LoadResourceHjson(Assembly asm, string resourceName)
        {
            using (Stream stream = asm.GetManifestResourceStream(resourceName))
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
                        if (endIdx >= 0)
                        {
                            string contentPart = rawLine.Substring(0, endIdx);
                            if (!string.IsNullOrWhiteSpace(contentPart))
                            {
                                multilineLines.Add(contentPart);
                            }
                        }
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
                        multilineLines.Add(rawLine.Trim());
                    }
                    i++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                {
                    i++;
                    continue;
                }

                if (line.EndsWith("{"))
                {
                    string header = line.Substring(0, line.Length - 1).Trim();
                    if (header.EndsWith(":"))
                    {
                        header = header.Substring(0, header.Length - 1).Trim();
                    }
                    string[] segs = header.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in segs)
                    {
                        string clean = s.Trim().Trim('"', '\'').TrimEnd(':');
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
                    string fullKey = pathStack.Count > 0 ? string.Join(".", pathStack) + "." + key : key;

                    if (val.StartsWith("'''"))
                    {
                        multilineKey = fullKey;
                        inMultilineString = true;
                        multilineLines.Clear();
                        string rest = val.Substring(3);
                        if (rest.EndsWith("'''") && rest.Length >= 3)
                        {
                            target[fullKey] = rest.Substring(0, rest.Length - 3).Trim();
                            inMultilineString = false;
                        }
                        else if (!string.IsNullOrEmpty(rest))
                        {
                            multilineLines.Add(rest.Trim());
                        }
                        i++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(val))
                    {
                        // 探测下一有效行是否为多行文本起始 '''
                        int nextIdx = i + 1;
                        while (nextIdx < lines.Length && string.IsNullOrWhiteSpace(lines[nextIdx]))
                        {
                            nextIdx++;
                        }
                        if (nextIdx < lines.Length && lines[nextIdx].Trim().StartsWith("'''"))
                        {
                            multilineKey = fullKey;
                            inMultilineString = true;
                            multilineLines.Clear();
                            string startTrim = lines[nextIdx].Trim();
                            if (startTrim.Length > 3)
                            {
                                multilineLines.Add(startTrim.Substring(3).Trim());
                            }
                            i = nextIdx + 1;
                            continue;
                        }
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

                    target[fullKey] = val;
                }

                i++;
            }
        }

        public static void RefreshAllItemLocalizations()
        {
            foreach (var item in ItemLoader.Items)
            {
                try
                {
                    ItemLoader.ResolveItemLocalization(item);
                    if (ContentSamples.ItemsByType.TryGetValue(item.Type, out Item sample) && sample != null)
                    {
                        string name = ItemLoader.GetDisplayName(item.Type);
                        if (!string.IsNullOrEmpty(name))
                        {
                            sample.SetNameOverride(name);
                        }
                        string tooltip = ItemLoader.GetTooltip(item.Type);
                        if (!string.IsNullOrEmpty(tooltip))
                        {
                            string[] lines = tooltip.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                            sample.ToolTip = Terraria.UI.ItemTooltip.FromHardcodedText(lines);
                        }
                    }
                }
                catch { }
            }

            foreach (var npc in NPCLoader.NPCs)
            {
                try
                {
                    NPCLoader.ResolveNPCLocalization(npc);
                }
                catch { }
            }

            foreach (var buff in BuffLoader.Buffs)
            {
                try
                {
                    BuffLoader.ResolveBuffLocalization(buff);
                }
                catch { }
            }

            foreach (var proj in ProjectileLoader.Projectiles)
            {
                try
                {
                    ProjectileLoader.ResolveProjectileLocalization(proj);
                }
                catch { }
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
                        if (localizedDict.TryGetValue(key, out LocalizedText existing) && existing != null)
                        {
                            existing.SetValue(value);
                        }
                        else
                        {
                            LocalizedText newText = new LocalizedText(key, value);
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
