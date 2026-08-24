using AccessoryBox.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace AccessoryBox
{
    public class ModifyInterfaceLayers : PatchMain
    {
        public static ModifyInterfaceLayers Instance { get; private set; }

        public static UIState ui_game_state { get; private set; } = null;
        private static UserInterface ui_game = null;
        private static BoxWindow boxWindow = null;

        static ModifyInterfaceLayers()
        {
            ui_game = new UserInterface();
            ui_game_state = new UIState();
            ui_game.SetState(ui_game_state);
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            Instance = this;

            int invIndex = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Inventory");
            if (invIndex != -1)
            {
                gameInterfaceLayers.Insert(invIndex, new LegacyGameInterfaceLayer(
                    "StaticTile.AccessoryBox: Box Window",
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
            if (Main.gameMenu)
            {
                if (boxWindow?.IsOpen == true) boxWindow.Close();
                return;
            }

            // 饰品箱开启时，若物品栏关闭（如按 ESC、E 键等），饰品箱同步一并关闭
            if (boxWindow?.IsOpen == true && !Main.playerInventory)
            {
                boxWindow.Close();
            }

            ui_game?.Update(gameTime);

            // 通过 tpml 统一 ModKeybind 系统检测快捷键
            if (Common.AccessoryBox.EnableMod && AccessoryBoxKeybind.ToggleKeybind?.JustPressed == true)
            {
                SwitchBox(fromKeybind: true);
            }
        }

        /// <summary>
        /// 饰品箱窗口是否打开
        /// </summary>
        public static bool BoxIsOpen => boxWindow?.IsOpen == true;

        /// <summary>
        /// 鼠标是否悬停在饰品箱窗口内
        /// </summary>
        public static bool BoxIsHovering => boxWindow?.IsOpen == true && boxWindow.ContainsPoint(Main.MouseScreen);

        /// <summary>
        /// 开关饰品箱窗口
        /// </summary>
        /// <param name="fromKeybind">是否由快捷键触发（快捷键关闭时同步关闭物品栏）</param>
        public static void SwitchBox(bool fromKeybind = false)
        {
            if (Common.AccessoryBox.EnableMod == false) return;

            if (boxWindow == null) boxWindow = new BoxWindow();

            if (boxWindow.IsOpen)
            {
                boxWindow.Close();
                if (fromKeybind && Main.playerInventory)
                {
                    Main.playerInventory = false;
                    SoundEngine.PlaySound(SoundID.MenuClose);
                }
            }
            else
            {
                boxWindow.Open(ui_game_state);
            }
        }

        public void SwitchWindow() => SwitchBox(false);
    }
}
