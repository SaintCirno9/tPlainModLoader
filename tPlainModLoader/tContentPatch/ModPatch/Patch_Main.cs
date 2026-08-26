using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using tContentPatch.Utils;
using Terraria;
using Terraria.GameInput;
using Terraria.IO;
using Terraria.UI;
using TPML.Content.IO;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Main))]
    internal class Patch_Main : ListCopy<PatchMain>
    {
        private static List<PatchMain> mod = new List<PatchMain>();

        public Patch_Main() : base(mod) { }



        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void UpdatePrefix(GameTime gameTime)
        {
            try
            {
                UpdatePrefix_CanUpdateGameplay();

                mod.ForTry(item => item.UpdatePrefix(gameTime));
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }

        private static bool _UpdatePrefix_CanUpdateGameplay_old = false;
        private static bool _UpdatePrefix_gameMenu_old = true;

        private static void UpdatePrefix_CanUpdateGameplay()
        {
            if (Main.netMode != 0 && Main.netMode != 1) return;

            if (_UpdatePrefix_CanUpdateGameplay_old == false && Main.CanUpdateGameplay == true)
            {
                if (Main.LocalPlayer != null)
                {
                    ModItemSidecarEngine.OnPlayerLoaded(Main.LocalPlayer);
                }
                mod.ForTry(item => item.OnEnterWorld());
            }
            else if (_UpdatePrefix_CanUpdateGameplay_old && Main.CanUpdateGameplay == false)
            {
                mod.ForTry(item => item.OnEnterWorldPrefix());
                // 离开世界退回主菜单时，清理所有扩展容器驻留数据与调度状态
                ModItemSidecarEngine.ResetContainers();
                // 离开世界时自动持久化所有脏模组设置
                ModSetting.SaveAllDirty();
            }

            if (!_UpdatePrefix_gameMenu_old && Main.gameMenu)
            {
                // 检测到进入主菜单，安全复位所有容器内存驻留状态
                ModItemSidecarEngine.ResetContainers();
                ModSetting.SaveAllDirty();
            }

            _UpdatePrefix_CanUpdateGameplay_old = Main.CanUpdateGameplay;
            _UpdatePrefix_gameMenu_old = Main.gameMenu;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.UpdatePostfix(gameTime));
        }

        [HarmonyPatch("SetupDrawInterfaceLayers")]
        [HarmonyPostfix]
        public static void SetupDrawInterfaceLayersPostfix()
        {
            try
            {
                List<GameInterfaceLayer> gameInterfaceLayers = Main.instance._gameInterfaceLayers;

                mod.ForTry(item => item.SetupDrawInterfaceLayersPostfix(gameInterfaceLayers));
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }

        private static int _preUpdateScrollWheelForUI = 0;

        [HarmonyPatch("UpdateUIStates")]
        [HarmonyPrefix]
        public static void UpdateUIStatesPrefix(GameTime gameTime)
        {
            _preUpdateScrollWheelForUI = PlayerInput.ScrollWheelDeltaForUI;

            mod.ForTry(item =>
            {
                PlayerInput.ScrollWheelDeltaForUI = _preUpdateScrollWheelForUI;
                item.UpdateUIStatesPrefix(gameTime);
            });

            PlayerInput.ScrollWheelDeltaForUI = _preUpdateScrollWheelForUI;
        }

        [HarmonyPatch("UpdateUIStates")]
        [HarmonyPostfix]
        public static void UpdateUIStatesPostfix(GameTime gameTime)
        {
            int scrollWheel = PlayerInput.ScrollWheelDeltaForUI;
            if (scrollWheel == 0 && _preUpdateScrollWheelForUI != 0)
            {
                scrollWheel = _preUpdateScrollWheelForUI;
            }

            mod.ForTry(item =>
            {
                if (PlayerInput.ScrollWheelDeltaForUI == 0 && scrollWheel != 0)
                {
                    PlayerInput.ScrollWheelDeltaForUI = scrollWheel;
                }
                item.UpdateUIStatesPostfix(gameTime);
            });
        }

        [HarmonyPatch("DoUpdateInWorld")]
        [HarmonyPrefix]
        public static void DoUpdateInWorldPrefix()
        {
            mod.ForTry(item => item.DoUpdateInWorldPrefix());
        }

        [HarmonyPatch("DoUpdateInWorld")]
        [HarmonyPostfix]
        public static void DoUpdateInWorldPostfix()
        {
            mod.ForTry(item => item.DoUpdateInWorldPostfix());
        }

        [HarmonyPatch("DrawMap")]
        [HarmonyPostfix]
        public static void DrawMapPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMapPostfix(gameTime));
        }

        [HarmonyPatch("DrawMenu")]
        [HarmonyPrefix]
        public static void DrawMenuPrefix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMenuPrefix(gameTime));
        }

        [HarmonyPatch("DrawMenu")]
        [HarmonyPostfix]
        public static void DrawMenuPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMenuPostfix(gameTime));
        }

        [HarmonyPatch("MouseText_DrawItemTooltip_GetLinesInfo")]
        [HarmonyPostfix]
        public static void MouseText_DrawItemTooltip_GetLinesInfoPostfix(Item item, ref int yoyoLogo,
            ref float oldKB, ref int numLines, ref string[] toolTipLine, ref Color[] lineColors)
        {
            try
            {
                foreach (PatchMain i in mod) i.MouseText_DrawItemTooltip_GetLinesInfoPostfix(item, ref yoyoLogo,
                    ref oldKB, ref numLines, ref toolTipLine, ref lineColors);
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }

        [HarmonyPatch("DoDraw")]
        [HarmonyPrefix]
        public static void DoDrawPrefix(GameTime gameTime)
        {
            mod.ForTry(item => item.DoDrawPrefix(gameTime));
        }

        [HarmonyPatch("DoDraw")]
        [HarmonyPostfix]
        public static void DoDrawPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.DoDrawPostfix(gameTime));
        }

        [HarmonyPatch("PlayerFocusedScreenPosition")]
        [HarmonyPostfix]
        public static void PlayerFocusedScreenPosition(ref Vector2 __result)
        {
            Vector2 origin = __result;
            Vector2 modifi = __result;

            mod.ForTry(item => modifi = item.PlayerFocusedScreenPosition(origin, modifi));

            __result = modifi;
        }

        [HarmonyPatch("ErasePlayer")]
        [HarmonyPrefix]
        public static void ErasePlayerPrefix(int i)
        {
            try
            {
                if (Main.PlayerList != null && i >= 0 && i < Main.PlayerList.Count)
                {
                    PlayerFileData playerFile = Main.PlayerList[i];
                    string playerName = playerFile?.Name ?? playerFile?.Player?.name;
                    if (!string.IsNullOrEmpty(playerName))
                    {
                        SidecarSaveManager.DeletePlayerSave(playerName);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }

        [HarmonyPatch("EraseWorld")]
        [HarmonyPrefix]
        public static void EraseWorldPrefix(int i)
        {
            try
            {
                if (Main.WorldList != null && i >= 0 && i < Main.WorldList.Count)
                {
                    WorldFileData worldFile = Main.WorldList[i];
                    if (worldFile != null)
                    {
                        string worldName = worldFile.Name ?? worldFile.GetWorldName();
                        int worldId = worldFile.WorldId;
                        SidecarSaveManager.DeleteWorldSave(worldName, worldId);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }
    }
}
