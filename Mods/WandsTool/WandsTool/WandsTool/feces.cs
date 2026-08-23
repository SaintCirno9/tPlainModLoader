using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using tContentPatch;
using Terraria;
using Terraria.UI;
using WandsTool.Content;

namespace WandsTool
{
    /// <summary>
    /// 哥们别看了, 这里都是直接移植的陈年老屎
    /// 原本想优化下的但看了下还不如直接重写
    /// </summary>
    public class feces : PatchMain
    {
        private static wandsPanel uiInstance = null;
        private static wandsPanel UI
        {
            get
            {
                if (uiInstance == null && !Main.dedServ)
                {
                    Resources.Load();
                    uiInstance = new wandsPanel();
                }
                return uiInstance;
            }
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            int index = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Laser Ruler");
            if (index != -1)
            {
                ++index;
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.WandsTool: Laser Ruler Postfix Game",
                    () =>
                    {
                        if (gameMain.Wand_isEnable)
                        {
                            Wands.Draw();
                        }
                        return true;
                    },
                    InterfaceScaleType.Game));

                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.WandsTool: Laser Ruler Postfix UI",
                    () =>
                    {
                        if (gameMain.UI_WandsPanel1_isOpen && gameMain.Wand_isEnable)
                        {
                            UI?.Draw(Main.spriteBatch, Main.gameTimeCache);
                        }

                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void UpdateUIStatesPostfix(GameTime gameTime)
        {
            if (Main.gameMenu)
            {
                gameMain.Wand_isEnable = false;
                UI?.Close();
                return;
            }

            if (gameMain.UI_WandsPanel1_isOpen && gameMain.Wand_isEnable)
            {
                UI?.Update(gameTime);
                UI?.update(gameTime);
            }
        }

        private static int lastSelectedItem = -1;

        public override void DoUpdateInWorldPostfix()
        {
            Player player = Main.LocalPlayer;
            if (gameMain.Wand_isEnable && player != null)
            {
                // 监听快捷栏手持物品切换，实时自适应魔棒工作模式
                if (player.selectedItem != lastSelectedItem)
                {
                    lastSelectedItem = player.selectedItem;
                    gameMain.AutoAdaptModeToHeldItem(player);
                }

                bool wasSelecting = Wands.Selecting;
                Wands.Update();
                WandAction.Update();

                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    if (!wasSelecting && !Wands.Selecting)
                    {
                        UI?.Toggle();
                    }
                    else
                    {
                        Main.mouseRightRelease = false;
                    }
                }
            }
            else
            {
                lastSelectedItem = -1;
                UI?.Close();

                Wands.Reset();
            }
        }
    }
}
