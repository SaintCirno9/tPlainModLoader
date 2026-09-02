using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TPML.Content;
using TPML.Network;
using TPML.Utils;
using Terraria;
using Terraria.GameInput;
using Terraria.IO;
using Terraria.UI;
using TPML.Content.Engine;
using TPML.Content.IO;
using TPML.Content.UI;
using TPML.Core.Diagnostics;
using TPML.Core.Logging;

namespace TPML.ModPatch
{
    /// <summary>
    /// 主游戏主循环与核心生命周期强类型门面调度中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class MainHooks
    {
        private static readonly ILogger Logger = LogManager.GetLogger("MainHooks");

        private static bool _hooksInitialized = false;
        private static bool _firstInvDrawLogged = false;

        /// <summary>集中注册 Main/TimeLogger 相关的 HookGen 强类型静态门面钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_Main.Update += Hook_Update;
            On_Main.SetupDrawInterfaceLayers += Hook_SetupDrawInterfaceLayers;
            On_Main.UpdateUIStates += Hook_UpdateUIStates;
            On_Main.DoUpdateInWorld += Hook_DoUpdateInWorld;
            On_Main.DrawMap += Hook_DrawMap;
            On_Main.DrawMenu += Hook_DrawMenu;
            On_Main.MouseText_DrawItemTooltip_GetLinesInfo += Hook_MouseText_DrawItemTooltip_GetLinesInfo;
            On_Main.DoDraw += Hook_DoDraw;
            On_Main.PlayerFocusedScreenPosition += Hook_PlayerFocusedScreenPosition;
            On_Main.ErasePlayer += Hook_ErasePlayer;
            On_Main.EraseWorld += Hook_EraseWorld;
            On_Main.DrawInventory += Hook_DrawInventory;
            On_TimeLogger.DrawException += Hook_DrawException;
            On_WorldFile.SaveWorld += Hook_SaveWorld;
            On_WorldFile.LoadWorld += Hook_LoadWorld;

            _hooksInitialized = true;
            Logger.Info("MainHooks 强类型生命周期门面钩子初始化完成");
        }

        #region Hook Handlers

        private static void Hook_Update(On_Main.orig_Update orig, Main self, GameTime gameTime)
        {
            try
            {
                AutoLoadMod.Prefix(gameTime);
                Threading.MainThreadDispatcher.CaptureMainThread();
                Threading.MainThreadDispatcher.Pump();

                float delta = gameTime != null ? (float)gameTime.ElapsedGameTime.TotalSeconds : 1f / 60f;
                PerformanceProfiler.Update(delta);

                UpdatePrefix_CanUpdateGameplay();

                var activeSystems = ContentHookDispatcher.ActiveModSystems;
                for (int idx = 0; idx < activeSystems.Length; idx++)
                {
                    try
                    {
                        activeSystems[idx].UpdatePrefix(gameTime);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModSystem.UpdatePrefix 异常: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }

            orig(self, gameTime);

            var systems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < systems.Length; idx++)
            {
                try
                {
                    systems[idx].UpdatePostfix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.UpdatePostfix 异常: {ex.Message}", ex);
                }
            }

            for (int idx = 0; idx < systems.Length; idx++)
            {
                try
                {
                    systems[idx].PostUpdateEverything();
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.PostUpdateEverything 异常: {ex.Message}", ex);
                }
            }

            RegisterNetModule.Postfix(gameTime);
        }

        private static void Hook_SetupDrawInterfaceLayers(On_Main.orig_SetupDrawInterfaceLayers orig, Main self)
        {
            orig(self);

            List<GameInterfaceLayer> gameInterfaceLayers = Main.instance?._gameInterfaceLayers;
            if (gameInterfaceLayers == null) return;

            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].ModifyInterfaceLayers(gameInterfaceLayers);
                }
                catch (Exception ex)
                {
                    Logger.Error($"执行 ModSystem.ModifyInterfaceLayers 异常: {ex.Message}", ex);
                }
            }

            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].SetupDrawInterfaceLayersPostfix(gameInterfaceLayers);
                }
                catch (Exception ex)
                {
                    Logger.Error($"执行 ModSystem.SetupDrawInterfaceLayersPostfix 异常: {ex.Message}", ex);
                }
            }

            if (Main.playerInventory && !_firstInvDrawLogged)
            {
                Logger.Info($"[MainHooks] 原生图层已注入模组图层，当前总图层数={gameInterfaceLayers.Count}");
                _firstInvDrawLogged = true;
            }
        }

        private static int _preUpdateScrollWheelForUI = 0;

        private static void Hook_UpdateUIStates(On_Main.orig_UpdateUIStates orig, GameTime gameTime)
        {
            _preUpdateScrollWheelForUI = PlayerInput.ScrollWheelDeltaForUI;

            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    PlayerInput.ScrollWheelDeltaForUI = _preUpdateScrollWheelForUI;
                    activeSystems[idx].UpdateUIStatesPrefix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.UpdateUIStatesPrefix 异常: {ex.Message}", ex);
                }
            }

            PlayerInput.ScrollWheelDeltaForUI = _preUpdateScrollWheelForUI;

            orig(gameTime);

            int scrollWheel = PlayerInput.ScrollWheelDeltaForUI;
            if (scrollWheel == 0 && _preUpdateScrollWheelForUI != 0)
            {
                scrollWheel = _preUpdateScrollWheelForUI;
            }

            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    if (PlayerInput.ScrollWheelDeltaForUI == 0 && scrollWheel != 0)
                    {
                        PlayerInput.ScrollWheelDeltaForUI = scrollWheel;
                    }
                    activeSystems[idx].UpdateUIStatesPostfix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.UpdateUIStatesPostfix 异常: {ex.Message}", ex);
                }
            }

            GameTime gt = gameTime ?? new GameTime();
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                var sys = activeSystems[idx];
                try
                {
                    if (PerformanceProfiler.IsEnabled)
                    {
                        using (PerformanceProfiler.Measure(sys.Mod?.Name ?? sys.GetType().Assembly.GetName().Name, sys.GetType().Name + ".UpdateUI"))
                        {
                            sys.UpdateUI(gt);
                        }
                    }
                    else
                    {
                        sys.UpdateUI(gt);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.UpdateUI 异常: {ex.Message}", ex);
                }
            }
        }

        private static void Hook_DoUpdateInWorld(On_Main.orig_DoUpdateInWorld orig, Main self)
        {
            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].DoUpdateInWorldPrefix();
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.DoUpdateInWorldPrefix 异常: {ex.Message}", ex);
                }
            }

            orig(self);

            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].DoUpdateInWorldPostfix();
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.DoUpdateInWorldPostfix 异常: {ex.Message}", ex);
                }
            }
        }

        private static void Hook_DrawMap(On_Main.orig_DrawMap orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);

            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].DrawMapPostfix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.DrawMapPostfix 异常: {ex.Message}", ex);
                }
            }
        }

        private static void Hook_DrawMenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
        {
            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].DrawMenuPrefix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.DrawMenuPrefix 异常: {ex.Message}", ex);
                }
            }

            orig(self, gameTime);

            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].DrawMenuPostfix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.DrawMenuPostfix 异常: {ex.Message}", ex);
                }
            }
        }

        private static void Hook_MouseText_DrawItemTooltip_GetLinesInfo(On_Main.orig_MouseText_DrawItemTooltip_GetLinesInfo orig, Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Color[] lineColors)
        {
            if (item != null && item.type >= ItemLoader.ModItemOffset)
            {
                ItemLoader.EnsureArraySizes(item.type);
            }

            orig(item, ref yoyoLogo, oldKB, ref numLines, toolTipLine, lineColors);

            if (item == null || item.IsAir) return;

            // 1. 若模组物品原生 Tooltip 行未被原版载入，动态补充 HJSON / ModItem 描述行
            if (item.type >= ItemLoader.ModItemOffset)
            {
                string tooltip = ItemLoader.GetTooltip(item.type);
                if (!string.IsNullOrEmpty(tooltip))
                {
                    string[] rawLines = tooltip.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    foreach (var l in rawLines)
                    {
                        if (!string.IsNullOrWhiteSpace(l))
                        {
                            bool exists = false;
                            for (int k = 0; k < numLines; k++)
                            {
                                if (toolTipLine[k] == l)
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (!exists && numLines < toolTipLine.Length)
                            {
                                toolTipLine[numLines] = l;
                                lineColors[numLines] = Color.White;
                                numLines++;
                            }
                        }
                    }
                }
            }

            // 2. 分发 ModItem.ModifyTooltips 与 GlobalItem.ModifyTooltips
            var list = new List<TooltipLine>();
            ItemLoader.ModifyTooltips(item, list);

            foreach (var line in list)
            {
                if (numLines < toolTipLine.Length && !string.IsNullOrEmpty(line.Text))
                {
                    toolTipLine[numLines] = line.Text;
                    if (line.OverrideColor.HasValue)
                    {
                        lineColors[numLines] = line.OverrideColor.Value;
                    }
                    numLines++;
                }
            }

            // 3. 分发 ModSystem 的 Tooltip 拓展
            float oldKBLocal = oldKB;
            string[] toolTipLineLocal = toolTipLine;
            Color[] lineColorsLocal = lineColors;
            MouseText_DrawItemTooltip_GetLinesInfoPostfix(item, ref yoyoLogo, ref oldKBLocal, ref numLines, ref toolTipLineLocal, ref lineColorsLocal);
        }

        private static void Hook_DoDraw(On_Main.orig_DoDraw orig, Main self, GameTime gameTime)
        {
            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].DoDrawPrefix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.DoDrawPrefix 异常: {ex.Message}", ex);
                }
            }

            orig(self, gameTime);

            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].DoDrawPostfix(gameTime);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.DoDrawPostfix 异常: {ex.Message}", ex);
                }
            }

            DrawIME.Postfix(gameTime);
            DrawTip.PatchDoDraw.Postfix(gameTime);
        }

        private static Vector2 Hook_PlayerFocusedScreenPosition(On_Main.orig_PlayerFocusedScreenPosition orig)
        {
            Vector2 result = orig();
            PlayerFocusedScreenPosition(ref result);
            return result;
        }

        private static void Hook_ErasePlayer(On_Main.orig_ErasePlayer orig, int i)
        {
            ErasePlayerPrefix(i);
            orig(i);
        }

        private static void Hook_EraseWorld(On_Main.orig_EraseWorld orig, int i)
        {
            EraseWorldPrefix(i);
            orig(i);
        }

        private static void Hook_DrawInventory(On_Main.orig_DrawInventory orig, Main self)
        {
            try
            {
                orig(self);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Main.DrawInventory] 物品栏绘制异常:\n{ex}");
                throw;
            }
        }

        private static void Hook_DrawException(On_TimeLogger.orig_DrawException orig, Exception ex)
        {
            Logger.Error($"[TimeLogger.DrawException] 捕获到 UI/绘制管线异常:\n{ex}");
            orig(ex);
        }

        private static void Hook_SaveWorld(On_WorldFile.orig_SaveWorld orig, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            if (Main.netMode == 0 || Main.netMode == 2)
            {
                ModItemSidecarEngine.OnWorldSavePrefix();
            }

            orig(resetTime, useTemps, canBeSkipped);

            if (Main.netMode == 0 || Main.netMode == 2)
            {
                ModItemSidecarEngine.OnWorldSavePostfix();
            }
        }

        private static void Hook_LoadWorld(On_WorldFile.orig_LoadWorld orig)
        {
            orig();
            if (Main.netMode == 0 || Main.netMode == 2)
            {
                ModItemSidecarEngine.OnWorldLoaded();
            }
        }

        #endregion

        #region Internal Dispatch & Lifecycle Logic

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
                var activeSystems = ContentHookDispatcher.ActiveModSystems;
                for (int idx = 0; idx < activeSystems.Length; idx++)
                {
                    try
                    {
                        activeSystems[idx].OnEnterWorld();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModSystem.OnEnterWorld 异常: {ex.Message}", ex);
                    }
                }
            }
            else if (_UpdatePrefix_CanUpdateGameplay_old && Main.CanUpdateGameplay == false)
            {
                var activeSystems = ContentHookDispatcher.ActiveModSystems;
                for (int idx = 0; idx < activeSystems.Length; idx++)
                {
                    try
                    {
                        activeSystems[idx].OnLeaveWorld();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"ModSystem.OnLeaveWorld 异常: {ex.Message}", ex);
                    }
                }
                // 离开世界时自动持久化所有脏模组设置（容器数据保留在内存中以确保随后的 Player.SavePlayer 正确写盘）
                ModSetting.SaveAllDirty();
            }

            if (!_UpdatePrefix_gameMenu_old && Main.gameMenu)
            {
                ModSetting.SaveAllDirty();
            }

            _UpdatePrefix_CanUpdateGameplay_old = Main.CanUpdateGameplay;
            _UpdatePrefix_gameMenu_old = Main.gameMenu;
        }

        public static void SetupDrawInterfaceLayersPostfix()
        {
            List<GameInterfaceLayer> gameInterfaceLayers = Main.instance?._gameInterfaceLayers;
            if (gameInterfaceLayers == null) return;

            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].SetupDrawInterfaceLayersPostfix(gameInterfaceLayers);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.SetupDrawInterfaceLayersPostfix 异常: {ex.Message}", ex);
                }
            }
        }

        public static void MouseText_DrawItemTooltip_GetLinesInfoPostfix(Item item, ref int yoyoLogo,
            ref float oldKB, ref int numLines, ref string[] toolTipLine, ref Color[] lineColors)
        {
            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    activeSystems[idx].MouseText_DrawItemTooltip_GetLinesInfoPostfix(item, ref yoyoLogo, ref oldKB, ref numLines, ref toolTipLine, ref lineColors);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.MouseText_DrawItemTooltip_GetLinesInfoPostfix 异常: {ex.Message}", ex);
                }
            }
        }

        public static void PlayerFocusedScreenPosition(ref Vector2 __result)
        {
            Vector2 origin = __result;
            Vector2 modifi = __result;

            var activeSystems = ContentHookDispatcher.ActiveModSystems;
            for (int idx = 0; idx < activeSystems.Length; idx++)
            {
                try
                {
                    modifi = activeSystems[idx].PlayerFocusedScreenPosition(origin, modifi);
                }
                catch (Exception ex)
                {
                    Logger.Error($"ModSystem.PlayerFocusedScreenPosition 异常: {ex.Message}", ex);
                }
            }

            __result = modifi;
        }

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

        #endregion
    }
}
