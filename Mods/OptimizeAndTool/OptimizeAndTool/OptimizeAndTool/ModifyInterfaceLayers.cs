using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using OptimizeAndTool.Content.BigBag;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool
{
    public class ModifyInterfaceLayers : PatchMain
    {
        public static UIState ui_menu_state { get; private set; } = null;
        private static UserInterface ui_menu = null;

        // 游戏内窗口 UI（巨大背包）
        public static UIState ui_game_state { get; private set; } = null;
        private static UserInterface ui_game = null;
        private static BigBagWindow bigBagWindow = null;

        static ModifyInterfaceLayers()
        {
            ui_menu = new UserInterface();
            ui_menu_state = new UIState();
            ui_menu.SetState(ui_menu_state);

            ui_game = new UserInterface();
            ui_game_state = new UIState();
            ui_game.SetState(ui_game_state);
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            int index = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Laser Ruler");
            if (index != -1)
            {
                ++index;
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.SundryTool: Laser Ruler Postfix",
                    () =>
                    {
                        Content.DisplayProjectileInfo.Draw();
                        return true;
                    },
                    InterfaceScaleType.Game));
            }

            // 巨大背包窗口绘制层，插在物品栏前
            int invIndex = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Inventory");
            if (invIndex != -1)
            {
                gameInterfaceLayers.Insert(invIndex, new LegacyGameInterfaceLayer(
                    "StaticTile.OptimizeAndTool: BigBag Window",
                    () =>
                    {
                        try
                        {
                            ui_game?.Draw(Main.spriteBatch, Main.gameTimeCache);
                        }
                        catch { }
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void UpdateUIStatesPostfix(GameTime gameTime)
        {
            ui_menu?.Update(gameTime);

            if (Main.gameMenu)
            {
                if (bigBagWindow?.IsOpen == true) bigBagWindow.Close();
                return;
            }

            // 大背包开启时，若物品栏关闭（如按 ESC、E 键等），大背包同步一并关闭
            if (bigBagWindow?.IsOpen == true && !Main.playerInventory)
            {
                bigBagWindow.Close();
            }

            ui_game?.Update(gameTime);

            // 通过 tpml 统一 ModKeybind 系统检测快捷键（原版 PlayerInput 自动在打字与聊天时静默）
            if (Content.BigBag.BigBag.EnableBigBag.val && Content.BigBag.BigBagKeybind.ToggleKeybind?.JustPressed == true)
            {
                SwitchBigBag(fromKeybind: true);
            }
        }

        public override void DrawMenuPrefix(GameTime gameTime)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            ui_menu.Draw(Main.spriteBatch, gameTime);
            Main.spriteBatch.End();
        }

        /// <summary>
        /// 巨大背包窗口是否打开
        /// </summary>
        public static bool BigBagIsOpen => bigBagWindow?.IsOpen == true;

        /// <summary>
        /// 鼠标是否悬停在巨大背包窗口内
        /// </summary>
        public static bool BigBagIsHovering => bigBagWindow?.IsOpen == true && bigBagWindow.ContainsPoint(Main.MouseScreen);

        /// <summary>
        /// 开关巨大背包窗口
        /// </summary>
        /// <param name="fromKeybind">是否由快捷键触发（快捷键关闭时同步关闭物品栏，鼠标关闭时保持物品栏不变）</param>
        public static void SwitchBigBag(bool fromKeybind = false)
        {
            if (Content.BigBag.BigBag.EnableBigBag.val == false) return;

            if (bigBagWindow == null) bigBagWindow = new BigBagWindow();

            if (bigBagWindow.IsOpen)
            {
                bigBagWindow.Close();
                if (fromKeybind && Main.playerInventory)
                {
                    Main.playerInventory = false;
                    SoundEngine.PlaySound(SoundID.MenuClose);
                }
            }
            else
            {
                bigBagWindow.Open(ui_game_state);
            }
        }
    }
}
