using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace CreativeInventory.KeyBind
{
    internal class UIKeyBind : UIItem
    {
        private string _key = null;
        private UIText ui_text = null;

        public UIKeyBind(Texture2D ico = null, string text = null) : base(ico, text)
        {
            ui_text = new UIText(Terraria.Lang.menu[195].Value);
            ui_text.HAlign = 1;
            ui_text.VAlign = 0.5f;
            ui_text.TextColor = Color.LightGray;

            OnLeftMouseUp += (e, s) =>
            {
                SetKey(CreativeInventoryKeybind.GetCurrentBoundKey());
                Main.NewText("提示：可在游戏主菜单或 ESC 设置中的 [控件 (Controls)] 统一自定义所有模组快捷键与手柄绑定。", Color.Gold);
            };

            Append(ui_text);
        }

        public void SetKey(string key)
        {
            _key = key;

            if (string.IsNullOrEmpty(_key) || _key == "None")
            {
                ui_text.SetText(Terraria.Lang.menu[195].Value);
                ui_text.TextColor = Color.Gray;
            }
            else
            {
                ui_text.SetText($"[ {_key} ]");
                ui_text.TextColor = Color.White;
            }
        }
    }
}
