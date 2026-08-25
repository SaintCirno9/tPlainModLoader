using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.UI;
using WandsTool.Content;
using WandsTool.KeyBind;

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

        public override void Initialize()
        {
            WandsKeybind.Initialize();
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            WandsKeybind.Initialize();

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
                        if (gameMain.Wand_isEnable)
                        {
                            Wands.DrawCursorModeTooltip(Main.spriteBatch);
                        }

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
        private static bool lastPlayerInventory = false;

        public override void DoUpdateInWorldPostfix()
        {
            Player player = Main.LocalPlayer;
            if (player == null || Main.gameMenu) return;

            // 监听统一 ModKeybind 开关魔杖模式（自动全局静默聊天/打字等）
            if (WandsKeybind.ToggleWand?.JustPressed == true)
            {
                SoundEngine.PlaySound(12);
                gameMain.ToggleWand();
            }

            // 监听背包开启/关闭状态切换（QoL：开启或关闭背包时自动退出魔杖模式）
            if (Main.playerInventory != lastPlayerInventory)
            {
                lastPlayerInventory = Main.playerInventory;
                if (gameMain.Wand_isEnable)
                {
                    gameMain.SetWandEnabled(false);
                }
            }

            if (gameMain.Wand_isEnable)
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
                WandAction.Clear();
                gameMain.CutSourceRect = null;
                gameMain.Wand_StructureMode = gameMain.StructureMode.None;
                wandsPanel.AutoReopenManagerAfterPlacement = false;
            }
        }
    }
}
