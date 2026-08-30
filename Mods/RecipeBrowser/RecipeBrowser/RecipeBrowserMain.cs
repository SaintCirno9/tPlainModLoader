using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using tContentPatch;
using Terraria;
using Terraria.UI;
using TPML.Core.Logging;

namespace RecipeBrowser
{
    public class RecipeBrowserMain : PatchMain
    {
        private static readonly ILogger Logger = LogManager.GetLogger("RecipeBrowser");
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
                            Logger.Error("Draw UI 异常", ex);
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
                            // 箭头追踪（对齐原版 HandleArrow）
                            RecipeBrowserUI.instance?.HandleArrow();
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
                Logger.Error("Update UI 异常", ex);
            }
        }
    }
}
