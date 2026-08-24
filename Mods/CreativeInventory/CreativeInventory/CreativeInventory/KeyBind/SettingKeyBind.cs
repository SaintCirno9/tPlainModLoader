using Microsoft.Xna.Framework.Graphics;
using System;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace CreativeInventory.KeyBind
{
    public class SettingKeyBind : ModSetting
    {
        public override string Name => "按键绑定";
        public override string Title => "创造物品栏: 按键绑定";
        public override string FilePath => "keyBind.json";
        public override Type DataType => typeof(string);

        public override void Load(object v)
        {
            // 统一由 KeybindLoader 从 input profiles.json 进行持久化恢复
            CreativeInventoryKeybind.Initialize();
        }

        public override UIElement GetUI()
        {
            UIKeyBind ui_item = new UIKeyBind(Main.Assets.Request<Texture2D>("Images/Item_306", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "开关创造物品栏 (可前往控件设置修改)");
            ui_item.SetKey(CreativeInventoryKeybind.GetCurrentBoundKey());
            return ui_item;
        }

        public override object GetSaveData() => CreativeInventoryKeybind.GetCurrentBoundKey();

        public override void SetDefault()
        {
        }
    }
}
