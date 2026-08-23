using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.UI;

namespace ReduceMouseLag
{
    internal class ModConfig : ModSetting
    {
        public static ModConfig Instance { get; private set; }

        public class Data
        {
            [JsonProperty("启用减少鼠标延迟")]
            public bool Enabled = true;

            [JsonProperty("Win32原生光标采样")]
            public bool UseWin32Direct = true;
        }

        public override string Name => "设置";
        public override string Title => "减少鼠标延迟: 设置";
        public override string FilePath => "减少鼠标延迟_配置.txt";
        public override Type DataType => typeof(Data);

        private static Data data = null;

        public ModConfig()
        {
            Instance = this;
        }

        public override void Load(object v)
        {
            if (v is Data loadedData)
            {
                data = loadedData;
            }
            else
            {
                SetDefault();
                Save();
            }

            SyncToEngine();
        }

        public override void SetDefault()
        {
            data = new Data();
            NeedSave = true;
            SyncToEngine();
        }

        public override object GetSaveData() => data;

        private static void SyncToEngine()
        {
            if (data == null) data = new Data();
            MouseLagFixEngine.Enabled = data.Enabled;
            MouseLagFixEngine.UseWin32Direct = data.UseWin32Direct;
        }

        public static bool IsEnabled
        {
            get
            {
                if (data == null) data = new Data();
                return data.Enabled;
            }
        }

        public static void SetEnabled(bool value, bool saveImmediate = false)
        {
            if (data == null) data = new Data();
            if (data.Enabled == value) return;
            data.Enabled = value;
            SyncToEngine();
            if (Instance != null)
            {
                Instance.NeedSave = true;
                if (saveImmediate)
                {
                    Instance.Save();
                }
            }
        }

        public static void SetUseWin32Direct(bool value, bool saveImmediate = false)
        {
            if (data == null) data = new Data();
            if (data.UseWin32Direct == value) return;
            data.UseWin32Direct = value;
            SyncToEngine();
            if (Instance != null)
            {
                Instance.NeedSave = true;
                if (saveImmediate)
                {
                    Instance.Save();
                }
            }
        }

        public static bool ToggleEnabled(bool saveImmediate = true)
        {
            if (data == null) data = new Data();
            bool newVal = !data.Enabled;
            SetEnabled(newVal, saveImmediate);
            return newVal;
        }

        public override UIElement GetUI()
        {
            UIScrollViewer2 sv = new UIScrollViewer2();
            sv.Width.Set(0, 1);
            sv.Height.Set(0, 1);

            List<UIElement> list = GetUIElements();
            foreach (var item in list)
            {
                sv.AddChild(item);
            }

            return sv;
        }

        public static List<UIElement> GetUIElements()
        {
            List<UIElement> list = new List<UIElement>();

            // 1. 启用开关
            UIItemSwitch swEnable = new UIItemSwitch(null, "启用高频鼠标采样");
            swEnable.OnUpdate += _ =>
            {
                if (data == null) return;
                swEnable.SetVal(data.Enabled);
                if (swEnable.IsMouseHovering) Main.instance.MouseText("在渲染每帧与光标绘制前执行即时采样，消除 60Hz 固有输入延迟");
            };
            swEnable.OnValUpdate += v =>
            {
                SetEnabled(v, saveImmediate: true);
            };
            list.Add(swEnable);

            // 2. Win32 原生光标开关
            UIItemSwitch swWin32 = new UIItemSwitch(null, "Win32 原生光标采样");
            swWin32.OnUpdate += _ =>
            {
                if (data == null) return;
                swWin32.SetVal(data.UseWin32Direct);
                if (swWin32.IsMouseHovering) Main.instance.MouseText("直接通过 Windows API 查询当前硬件光标微秒坐标，绕过 XNA 消息循环");
            };
            swWin32.OnValUpdate += v =>
            {
                SetUseWin32Direct(v, saveImmediate: true);
            };
            list.Add(swWin32);

            return list;
        }
    }
}
