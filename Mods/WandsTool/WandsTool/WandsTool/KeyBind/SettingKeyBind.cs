using Microsoft.Xna.Framework.Graphics;
using System;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace WandsTool.KeyBind
{
    public class SettingKeyBind : ModSetting
    {
        public override string Name => "按键绑定";
        public override string Title => "魔杖工具: 按键绑定";
        public override string FilePath => "KeyBind.json";
        public override Type DataType => typeof(string);

        public override void Load(object v)
        {
            // 统一由 KeybindLoader 从 input profiles.json 进行持久化恢复
            WandsKeybind.Initialize();
        }

        public override UIElement GetUI()
        {
            Texture2D icon = tContentPatch.Utils.Resource.GetTexture2D($"{nameof(WandsTool)}.Resources.Wand.png");
            UIKeyBind ui_item = new UIKeyBind(icon, "开关魔杖模式 (可前往控件设置修改)");
            ui_item.SetKey(WandsKeybind.GetCurrentBoundKey());
            return ui_item;
        }

        public override object GetSaveData() => WandsKeybind.GetCurrentBoundKey();

        public override void SetDefault()
        {
        }
    }
}
