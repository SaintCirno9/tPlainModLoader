using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using tContentPatch;
using Terraria;
using Terraria.UI;
using TPML.Content;

namespace QuickButton
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

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            int index = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Inventory");
            if (index != -1)
            {
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                "StaticTile.QuickButton: Inventory Prefix",
                () =>
                {
                    ui.Draw(Main.spriteBatch, Main.gameTimeCache);
                    return true;
                },
                InterfaceScaleType.UI));
            }
        }
    }
}
