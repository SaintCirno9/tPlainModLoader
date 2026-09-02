using Microsoft.Xna.Framework;
using PixelArt.Content;
using System.Collections.Generic;
using System.Diagnostics;
using TPML;
using Terraria;
using Terraria.UI;

namespace PixelArt
{
    public class ModifyInterfaceLayers : TPML.Content.ModSystem
    {
        public static UIState ui_state { get; private set; } = null;
        private static UserInterface ui = null;
        private static Window window = null;

        static ModifyInterfaceLayers()
        {
            if (Main.dedServ) return;

            ui_state = new UIState();
            ui = new UserInterface();
            ui.SetState(ui_state);
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            int index = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Inventory");
            if (index != -1)
            {
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.SundryTool: Inventory Prefix UI",
                    () =>
                    {
                        ui.Draw(Main.spriteBatch, Main.gameTimeCache);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }

            index = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Laser Ruler");
            if (index != -1)
            {
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.SundryTool: Laser Ruler Prefix Game",
                    () =>
                    {
                        Content.PixelArt.Draw();
                        return true;
                    },
                    InterfaceScaleType.Game));
            }
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
            }
        }

        public override void DoUpdateInWorldPrefix()
        {
            Content.PixelArt.Update(Main.LocalPlayer);
        }

        public static void OCWindow()
        {
            if (window == null)
            {
                window = new Window("像素画", 350, 320);
            }

            if (window.IsOpen) window.Close();
            else window.Open(ui_state);
        }
    }
}
