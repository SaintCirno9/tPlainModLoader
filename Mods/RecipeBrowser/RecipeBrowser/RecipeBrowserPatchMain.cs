using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace RecipeBrowser
{
    public class RecipeBrowserPatchMain : PatchMain
    {
        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            if (Main.dedServ) return;

            int mouseTextIndex = gameInterfaceLayers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                gameInterfaceLayers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "RecipeBrowser: UI",
                    () =>
                    {
                        try
                        {
                            RecipeBrowserMod.Instance?.DrawUI();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[RecipeBrowser] Draw UI Error: {ex}");
                        }
                        return true;
                    },
                    InterfaceScaleType.UI));
            }

            int logic4Index = gameInterfaceLayers.FindIndex(layer => layer.Name.Equals("Vanilla: Interface Logic 4"));
            if (logic4Index != -1)
            {
                gameInterfaceLayers.Insert(logic4Index + 1, new LegacyGameInterfaceLayer(
                    "RecipeBrowser: Arrow",
                    () =>
                    {
                        try
                        {
                            // 箭头追踪
                        }
                        catch { }
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void UpdateUIStatesPostfix(GameTime gameTime)
        {
            if (Main.dedServ) return;

            // 1. 更新 RecipeBrowser UI 状态机
            try
            {
                RecipeBrowserMod.Instance?.UpdateUI(gameTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RecipeBrowser] Update UI Error: {ex}");
            }
        }
    }
}
