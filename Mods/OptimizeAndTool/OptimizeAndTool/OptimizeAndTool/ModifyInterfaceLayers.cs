using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using OptimizeAndTool.Content.BigBag;
using tContentPatch;
using Terraria;
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
        private static bool oldBKey = false;

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

            ui_game?.Update(gameTime);

            // 自定义快捷键开关巨大背包（聊天输入/编辑告示牌时忽略）
            Keys targetKey = Keys.X;
            string keyStr = Content.BigBag.BigBag.HotKey.val;
            if (!string.IsNullOrEmpty(keyStr) && System.Enum.TryParse(keyStr, true, out Keys parsedKey))
            {
                targetKey = parsedKey;
            }

            bool hotKeyDown = targetKey != Keys.None && Main.keyState.IsKeyDown(targetKey);
            if (hotKeyDown && !oldBKey && !Main.drawingPlayerChat && !Main.editSign)
            {
                SwitchBigBag();
            }
            oldBKey = hotKeyDown;
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
        public static void SwitchBigBag()
        {
            if (Content.BigBag.BigBag.EnableBigBag.val == false) return;

            if (bigBagWindow == null) bigBagWindow = new BigBagWindow();

            if (bigBagWindow.IsOpen) bigBagWindow.Close();
            else bigBagWindow.Open(ui_game_state);
        }
    }
}
