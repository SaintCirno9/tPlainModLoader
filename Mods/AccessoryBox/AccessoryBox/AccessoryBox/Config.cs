using System;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria.UI;

namespace AccessoryBox
{
    internal class Config : ModSetting
    {
        public class ConfigData
        {
            public bool EnableMod = true;
            public bool EnablePassive = true;
            public int Capacity = 100;
        }

        public override string Name => "设置";
        public override string Title => "饰品箱: 设置";
        public override string FilePath => "setting.json";
        public override Type DataType => typeof(ConfigData);
        public static Config Instance { get; protected set; } = null;
        private ConfigData data = new ConfigData();

        public override void Load(object v)
        {
            Instance = this;

            if (v == null)
            {
                SetDefault();
                NeedSave = true;
                Save();
            }
            else if (v is bool legacyBool)
            {
                // 兼容旧版仅保存一个 bool 的配置
                data = new ConfigData
                {
                    EnableMod = legacyBool,
                    EnablePassive = true,
                    Capacity = 100
                };
                NeedSave = true;
                Save();
            }
            else if (v is ConfigData cfg)
            {
                data = cfg;
            }
        }

        public override object GetSaveData() => data;

        public override void SetDefault()
        {
            data = new ConfigData();
        }

        public override UIElement GetUI()
        {
            UIStackPanel sp = new UIStackPanel();
            sp.Width.Set(0, 1);
            sp.IsAutoUpdateSize = true;
            sp.ItemMargin = 6;

            UIItemSwitch uiMod = new UIItemSwitch(null, "启用模组");
            uiMod.OnUpdate += _ => uiMod.SetVal(data.EnableMod);
            uiMod.OnValUpdate += v => SetEnableMod(v);
            sp.Append(uiMod);

            UIItemSwitch uiPassive = new UIItemSwitch(null, "箱内饰品属性与被动生效");
            uiPassive.OnUpdate += _ => uiPassive.SetVal(data.EnablePassive);
            uiPassive.OnValUpdate += v => SetEnablePassive(v);
            sp.Append(uiPassive);

            return sp;
        }

        public bool GetEnableMod() => data.EnableMod;
        public void SetEnableMod(bool v)
        {
            if (data.EnableMod == v) return;
            data.EnableMod = v;
            NeedSave = true;
            Save();
        }

        public bool GetEnablePassive() => data.EnablePassive;
        public void SetEnablePassive(bool v)
        {
            if (data.EnablePassive == v) return;
            data.EnablePassive = v;
            NeedSave = true;
            Save();
        }

        public int GetCapacity() => data.Capacity;
        public void SetCapacity(int cap)
        {
            cap = Math.Max(40, Math.Min(500, cap));
            if (data.Capacity == cap) return;
            data.Capacity = cap;
            NeedSave = true;
            Save();
            Common.AccessoryBox.SetCapacity(cap);
        }
    }
}
