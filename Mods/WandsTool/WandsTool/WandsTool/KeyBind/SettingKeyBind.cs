using Microsoft.Xna.Framework.Graphics;
using System;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.UI;
using WandsTool.Content;

namespace WandsTool.KeyBind
{
    public class SettingKeyBind : ModSetting
    {
        public override string Name => "按键绑定";
        public override string Title => "魔杖工具: 按键绑定";
        public override string FilePath => "KeyBind.json";
        public override Type DataType => typeof(string);
        private static string key = "Z";
        private static Action<string> updateUI = null;

        public override void Load(object v)
        {
            if (v is string s && !string.IsNullOrEmpty(s))
            {
                key = s;
            }
            else
            {
                key = "Z";
                SetDefault();
                Save();
            }

            Bind(key);
        }

        public static void Bind(string newKey)
        {
            if (!string.IsNullOrEmpty(key))
            {
                ListenInput.DelListenInput(key, OnKeyPressed);
            }

            key = newKey;

            if (!string.IsNullOrEmpty(key))
            {
                ListenInput.AddListenInput(key, OnKeyPressed);
            }
        }

        private static void OnKeyPressed(bool isOne)
        {
            if (!isOne) return;
            if (Main.gameMenu || Main.drawingPlayerChat || Main.editChest || Main.editSign) return;

            SoundEngine.PlaySound(12);
            gameMain.Wand_isEnable = !gameMain.Wand_isEnable;
            if (gameMain.Wand_isEnable)
            {
                gameMain.AutoAdaptModeToHeldItem(Main.LocalPlayer);
            }
        }

        public override UIElement GetUI()
        {
            updateUI = null;

            Texture2D icon = tContentPatch.Utils.Resource.GetTexture2D($"{nameof(WandsTool)}.Resources.Wand.png");
            UIKeyBind ui_item = new UIKeyBind(icon, "开关魔杖模式");
            ui_item.SetKey(key);
            ui_item.OnKeyUpdate += s =>
            {
                Bind(s);
                NeedSave = true;
                Save();
            };

            updateUI += s =>
            {
                ui_item.SetKey(s);
            };

            return ui_item;
        }

        public override object GetSaveData() => key;

        public override void SetDefault()
        {
            Bind("Z");
            NeedSave = true;
            Save();
            updateUI?.Invoke("Z");
        }
    }
}
