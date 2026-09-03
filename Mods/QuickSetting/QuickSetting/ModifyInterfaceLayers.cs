using Microsoft.Xna.Framework;
using QuickSetting.KeyBind;
using System.Collections.Generic;
using TPML;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace QuickSetting
{
    public class ModifyInterfaceLayers : ModSystem
    {
        public static UIState ui_state { get; protected set; } = null;
        private static UserInterface ui = null;

        static ModifyInterfaceLayers()
        {
            ui = new UserInterface();
            ui_state = new UIState();
            ui.SetState(ui_state);
        }

        public override void Initialize()
        {
            QuickSettingKeybind.Initialize();
        }

        public override void UpdateUIStatesPostfix(GameTime gameTime)
        {
            if (ui == null) return;

            if (Main.gameMenu)
            {
                ui.SetState(null);
            }
            else
            {
                ui.SetState(ui_state);
                ui.Update(gameTime);

                // 监听统一 ModKeybind 快捷键输入
                if (QuickSettingKeybind.ToggleKeybind?.JustPressed == true)
                {
                    QuickSetting.SwitchOpenOrClose();
                }
            }
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            QuickSettingKeybind.Initialize();

            int index = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Inventory");
            if (index != -1)
            {
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.QuickSetting: Inventory Prefix",
                    () =>
                    {
                        ui.Draw(Main.spriteBatch, Main.gameTimeCache);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }
    }

    public class QuickSettingContentMod : TPML.Content.Mod { }
}
