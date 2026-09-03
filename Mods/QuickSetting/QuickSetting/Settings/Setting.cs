using System;
using Microsoft.Xna.Framework.Graphics;
using TPML;
using TPML.UI.ModSet;
using Terraria;
using Terraria.UI;

namespace QuickSetting.Settings
{
    public class Setting : ModSetting
    {
        public class Data
        {
            public bool isLinkage = true;
            public float? windowPosX = null;
            public float? windowPosY = null;
            public float? windowWidth = null;
            public float? windowHeight = null;
        }

        public override string Name => "设置";
        public override string Title => "快捷设置: 设置";
        public override string FilePath => "setting.json";
        public override Type DataType => typeof(Data);
        private Data data = null;
        private Action<bool> updateUi = null;
        private Action onWindowChanged = null;

        public override void Load(object v)
        {
            data = v as Data;
            if (data == null)
            {
                data = new Data();
                NeedSave = true;
                Save();
            }

            ModLinkage.ModQuickButton.IsLinkage = data.isLinkage;

            QuickSetting.SetWindowGeometry(data.windowPosX, data.windowPosY, data.windowWidth, data.windowHeight);

            // 拖动或改变大小结束后即时落盘
            QuickSetting.OnWindowGeometryChanged -= onWindowChanged;
            onWindowChanged = () =>
            {
                var (x, y, w, h) = QuickSetting.GetWindowGeometry();
                data.windowPosX = x;
                data.windowPosY = y;
                data.windowWidth = w;
                data.windowHeight = h;
                NeedSave = true;
                Save();
            };
            QuickSetting.OnWindowGeometryChanged += onWindowChanged;
        }

        public override object GetSaveData() => data;

        public override void SetDefault()
        {
            data = new Data();
            NeedSave = true;
            updateUi?.Invoke(data.isLinkage);
        }

        public override UIElement GetUI()
        {
            UIItemSwitch ui = new UIItemSwitch(
                Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value,
                "添加到快捷按钮");
            ui.OnValUpdate += v =>
            {
                if (data == null) return;
                if (data.isLinkage == v) return;
                data.isLinkage = v;
                NeedSave = true;
            };
            ui.OnUpdate += _ => { if (ui.IsMouseHovering) Main.instance.MouseText("重新加载模组生效"); };
            updateUi = ui.SetVal;

            if (data != null) updateUi(data.isLinkage);

            return ui;
        }
    }
}
