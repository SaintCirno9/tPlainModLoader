using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Terraria.ModLoader.Config
{
    public enum ConfigScope
    {
        ClientSide,
        ServerSide
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public class LabelAttribute : Attribute
    {
        public string Label { get; }
        public LabelAttribute(string label) => Label = label;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public class TooltipAttribute : Attribute
    {
        public string Tooltip { get; }
        public TooltipAttribute(string tooltip) => Tooltip = tooltip;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DefaultValueAttribute : System.ComponentModel.DefaultValueAttribute
    {
        public DefaultValueAttribute(bool value) : base(value) { }
        public DefaultValueAttribute(int value) : base(value) { }
        public DefaultValueAttribute(float value) : base(value) { }
        public DefaultValueAttribute(string value) : base(value) { }
        public DefaultValueAttribute(object value) : base(value) { }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class RangeAttribute : Attribute
    {
        public object Min { get; }
        public object Max { get; }
        public RangeAttribute(int min, int max) { Min = min; Max = max; }
        public RangeAttribute(float min, float max) { Min = min; Max = max; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public class ReloadRequiredAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HeaderAttribute : Attribute
    {
        public string Header { get; }
        public HeaderAttribute(string header) => Header = header;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IncrementAttribute : Attribute
    {
        public object Increment { get; }
        public IncrementAttribute(object increment) => Increment = increment;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SeparatePageAttribute : Attribute { }

    /// <summary>
    /// tModLoader 模组配置基类
    /// </summary>
    public abstract class ModConfig : ModType
    {
        [JsonIgnore]
        public abstract ConfigScope Mode { get; }

        [JsonIgnore]
        public virtual bool NeedsReload => false;

        public virtual void OnChanged() { }
        public virtual void OnLoaded() { }

        public override void Load(Mod mod)
        {
            base.Load(mod);
            ConfigManager.Load(this);
        }

        public void Save()
        {
            ConfigManager.Save(this);
        }
    }

    /// <summary>
    /// 模组配置持久化管理器
    /// </summary>
    public static class ConfigManager
    {
        public static string ConfigDir { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "My Games", "Terraria", "tPlainModLoader", "ModConfigs");

        public static void Load(ModConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                string path = Path.Combine(ConfigDir, $"{config.Mod.Name}_{config.Name}.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    JsonConvert.PopulateObject(json, config);
                }
                config.OnLoaded();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigManager] 读取配置失败 ({config.Name}): {ex.Message}");
            }
        }

        public static void Save(ModConfig config)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                string path = Path.Combine(ConfigDir, $"{config.Mod.Name}_{config.Name}.json");
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(path, json);
                config.OnChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigManager] 保存配置失败 ({config.Name}): {ex.Message}");
            }
        }
    }
}
