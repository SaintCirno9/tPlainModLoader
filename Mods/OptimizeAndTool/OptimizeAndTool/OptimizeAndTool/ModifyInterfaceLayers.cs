using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using OptimizeAndTool.Content.BigBag;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Content.Creative;
using OptimizeAndTool.Content.QoL.Pipette;
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

        // 游戏内窗口 UI 主状态（大背包、饰品箱、创造物品栏共用）
        public static UIState ui_game_state { get; private set; } = null;
        public static UIState ui_state => ui_game_state;
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

            // 注册原生按键绑定
            BigBagKeybind.Initialize();
            AccessoryBoxKeybind.Register();
            CreativeInventoryKeybind.Initialize();
            PipetteKeybind.Register();
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

            int invIndex = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Inventory");
            if (invIndex != -1)
            {
                // 玩家信息透视层
                gameInterfaceLayers.Insert(invIndex, new LegacyGameInterfaceLayer(
                    "StaticTile.SundryTool: InventoryPrefix",
                    () =>
                    {
                        Content.Cheat.Function1.Function_displayPlay.Draw(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI));

                // 统一窗口 UI 绘制层（大背包、饰品箱、创造模式浏览器）
                gameInterfaceLayers.Insert(invIndex + 1, new LegacyGameInterfaceLayer(
                    "StaticTile.OptimizeAndTool: Windows Layer",
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
                if (BoxWindow.IsOpen) BoxWindow.Instance.Close();
                return;
            }

            // 大背包/饰品盒开启时，若物品栏关闭，同步关闭对应扩展窗口
            if (bigBagWindow?.IsOpen == true && !Main.playerInventory)
            {
                bigBagWindow.Close();
            }
            if (BoxWindow.IsOpen && !Main.playerInventory)
            {
                BoxWindow.Instance.Close();
            }

            ui_game?.Update(gameTime);

            // 原生快捷键响应调度
            if (Content.BigBag.BigBag.EnableBigBag.val && BigBagKeybind.ToggleKeybind?.JustPressed == true)
            {
                SwitchBigBag(fromKeybind: true);
            }

            AccessoryBoxKeybind.Update();
            PipetteKeybind.Update();

            if (CreativeInventoryKeybind.ToggleKeybind?.JustPressed == true)
            {
                CreativeInventory.SwitchOpenOrClose();
            }
        }

        public override void DrawMenuPrefix(GameTime gameTime)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            ui_menu.Draw(Main.spriteBatch, gameTime);
            Main.spriteBatch.End();
        }

        public static bool BigBagIsOpen => bigBagWindow?.IsOpen == true;
        public static bool BigBagIsHovering => bigBagWindow?.IsOpen == true && bigBagWindow.ContainsPoint(Main.MouseScreen);

        public static bool BoxIsOpen => BoxWindow.IsOpen;
        public static bool BoxIsHovering => BoxWindow.IsOpenAndHovering;

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
