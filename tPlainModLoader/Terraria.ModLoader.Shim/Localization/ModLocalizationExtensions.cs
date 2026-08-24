using System;
using Terraria.Localization;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 本地化扩展存根
    /// </summary>
    public static class ModLocalizationExtensions
    {
        public static LocalizedText GetOrRegister(string key, Func<string> makeDefaultValue = null)
        {
            if (Language.Exists(key))
            {
                return Language.GetText(key);
            }

            string defaultVal = makeDefaultValue != null ? makeDefaultValue() : key;
            // 注入原版 LanguageManager
            LanguageManager.Instance.SetText(key, defaultVal);
            return Language.GetText(key);
        }

        public static void SetText(this LanguageManager manager, string key, string value)
        {
            if (manager == null || string.IsNullOrEmpty(key)) return;
            try
            {
                // 利用 Publicizer 直连访问 _localizedTexts
                if (manager._localizedTexts.TryGetValue(key, out var text))
                {
                    text.SetValue(value);
                }
                else
                {
                    manager._localizedTexts[key] = new LocalizedText(key, value);
                }
            }
            catch
            {
                // Fallback
            }
        }
    }
}
