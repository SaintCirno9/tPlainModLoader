using Microsoft.Xna.Framework.Graphics;
using System;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace QuickSetting.KeyBind
{
    public class SettingKeyBind : ModSetting
    {
        public override string Name => "按键绑定";
        public override string Title => "快速设置: 按键绑定";
        public override string FilePath => "keyBind.json";
        public override Type DataType => typeof(string);

        public override void Load(object v)
        {
            // 统一由 KeybindLoader 从 input profiles.json 进行持久化恢复
            QuickSettingKeybind.Initialize();
        }

        public override UIElement GetUI()
        {
            UIKeyBind ui_item = new UIKeyBind(Main.Assets.Request<Texture2D>("Images/UI/Camera_1", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "开关快捷设置 (可前往控件设置修改)");
            ui_item.SetKey(QuickSettingKeybind.GetCurrentBoundKey());
            return ui_item;
        }

        public override object GetSaveData() => QuickSettingKeybind.GetCurrentBoundKey();

        public override void SetDefault()
        {
        }
    }
}
