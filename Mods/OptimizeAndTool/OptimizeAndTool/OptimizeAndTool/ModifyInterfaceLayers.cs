using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using OptimizeAndTool.Content.BigBag;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Content.Storage.Core;
using OptimizeAndTool.Content.Creative;
using OptimizeAndTool.Content.QoL.Pipette;
using OptimizeAndTool.Content.QoL.InfiniteBuff;
using TPML;
using TPML.UI;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool
{
    public class ModifyInterfaceLayers : TPML.Content.ModSystem
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
            CreativeInventoryKeybind.Initialize();
            PipetteKeybind.Register();
            InfiniteBuffKeybind.Initialize();
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

                // 重铸前缀优化 UI 绘制层
                gameInterfaceLayers.Insert(invIndex + 2, new LegacyGameInterfaceLayer(
                    "StaticTile.OptimizeAndTool: Reforge UI Layer",
                    () =>
                    {
                        try
                        {
                            Content.QoL.Reforge.ReforgeOptimization.DrawReforgeUI(Main.spriteBatch);
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
                CloseAllCustomWindows();
                return;
            }

            // 若物品栏关闭，自动关闭所有依附于物品栏生命周期的扩展窗口（大背包、饰品箱、实体收纳袋等）
            if (!Main.playerInventory)
            {
                CloseInventoryBoundWindows();
            }

            ui_game?.Update(gameTime);

            // 原生快捷键响应调度
            if (Content.BigBag.BigBag.EnableBigBag.val && BigBagKeybind.ToggleKeybind?.JustPressed == true)
            {
                SwitchBigBag(fromKeybind: true);
            }

            Content.Storage.Core.CarriedBagInteractionHooks.UpdateKeybinds();
            PipetteKeybind.Update();

            if (CreativeInventoryKeybind.ToggleKeybind?.JustPressed == true)
            {
                CreativeInventory.SwitchOpenOrClose();
            }

            if (InfiniteBuffKeybind.ToggleKeybind?.JustPressed == true)
            {
                InfiniteBuffWindow.Instance.Toggle();
            }
        }

        public override void DrawMenuPrefix(GameTime gameTime)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            ui_menu.Draw(Main.spriteBatch, gameTime);
            Main.spriteBatch.End();
        }

        public static bool BigBagIsOpen => bigBagWindow?.IsOpen == true;
        public static bool BoxIsOpen => AccessoryBagWindow.IsOpen;
        public static bool InfiniteBuffIsOpen => InfiniteBuffWindow.IsWindowOpen;

        /// <summary>
        /// 关闭所有依附于物品栏生命周期的窗口（如大背包、饰品箱及各实体收纳袋）
        /// </summary>
        public static void CloseInventoryBoundWindows()
        {
            if (ui_game_state?.Children == null) return;
            List<UIWindow> toClose = new List<UIWindow>();
            foreach (var elem in ui_game_state.Children)
            {
                if (elem is UIWindow win && win.IsOpen && (elem is BigBagWindow || elem is UniversalBagWindow))
                {
                    toClose.Add(win);
                }
            }
            for (int i = 0; i < toClose.Count; i++)
            {
                toClose[i].Close();
            }
        }

        /// <summary>
        /// 关闭所有已打开的自定义模组窗口
        /// </summary>
        public static void CloseAllCustomWindows()
        {
            if (ui_game_state?.Children == null) return;
            List<UIWindow> toClose = new List<UIWindow>();
            foreach (var elem in ui_game_state.Children)
            {
                if (elem is UIWindow win && win.IsOpen)
                {
                    toClose.Add(win);
                }
            }
            for (int i = 0; i < toClose.Count; i++)
            {
                toClose[i].Close();
            }
        }

        /// <summary>
        /// 统一判定光标是否悬停在当前任何已打开的自定义模组窗口内（大背包、饰品箱、各类收纳袋、物品浏览器、无尽增益等）
        /// </summary>
        public static bool IsAnyCustomWindowHovering()
        {
            if (ui_game_state?.Children == null) return false;
            foreach (var elem in ui_game_state.Children)
            {
                if (elem is UIWindow win && win.IsOpen && IsHoveringWindow(win))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判定光标是否悬停在指定自定义窗口内（结合 IsMouseHovering 与 16px 边框容差，确保滑条与外沿判定 100% 覆盖）
        /// </summary>
        public static bool IsHoveringWindow(UIElement win)
        {
            if (win == null) return false;

            if (win is UIWindow uiWin && !uiWin.IsOpen) return false;

            if (win.IsMouseHovering) return true;

            CalculatedStyle dims = win.GetDimensions();
            if (dims.Width <= 0 || dims.Height <= 0) return false;

            // 增加 16 像素外沿容差，彻底覆盖窗口边框、滚动条凸出区、阴影与缩放抓手
            float margin = 16f;
            Rectangle winRect = new Rectangle(
                (int)(dims.X - margin),
                (int)(dims.Y - margin),
                (int)(dims.Width + margin * 2f),
                (int)(dims.Height + margin * 2f)
            );

            return winRect.Contains(Main.MouseScreen.ToPoint());
        }

        public static void SwitchBigBag(bool fromKeybind = false)
        {
            if (Content.BigBag.BigBag.EnableBigBag.val == false) return;

            if (bigBagWindow == null) bigBagWindow = new BigBagWindow();

            if (bigBagWindow.IsOpen)
            {
                bigBagWindow.Close();
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
            else
            {
                bigBagWindow.Open(ui_game_state);
            }
        }
    }
}
