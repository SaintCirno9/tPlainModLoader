using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using tContentPatch.Utils;
using Terraria;
using Terraria.GameInput;
using Terraria.IO;
using Terraria.UI;
using TPML.Content.Engine;
using TPML.Content.IO;
using TPML.Core.Diagnostics;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Main 主循环/绘制/存档生命周期补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_Main : ListCopy<PatchMain>
    {
        private static List<PatchMain> mod = new List<PatchMain>();

        public Patch_Main() : base(mod) { }

        #region Tooltip 管线自定义委托（原方法含 ref 参数）

        private delegate void Orig_MouseTextLines(Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Color[] lineColors);
        private delegate void Hook_MouseTextLines(Orig_MouseTextLines orig, Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Color[] lineColors);

        #endregion

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var main = typeof(Main);

            // Main.Update(GameTime)：性能采样 + 生命周期切换 + 模组分发
            Register("Main.Update(GameTime)", MethodLookup.Instance(main, "Update", typeof(GameTime)),
                (Action<Action<Main, GameTime>, Main, GameTime>)((orig, self, gameTime) =>
                {
                    UpdatePrefix(gameTime);
                    orig(self, gameTime);
                    UpdatePostfix(gameTime);
                }));

            // Main.SetupDrawInterfaceLayers()
            Register("Main.SetupDrawInterfaceLayers()", GetInstance(main, "SetupDrawInterfaceLayers"),
                (Action<Action<Main>, Main>)((orig, self) =>
                {
                    orig(self);
                    SetupDrawInterfaceLayersPostfix();
                }));

            // Main.UpdateUIStates(GameTime)：滚轮 delta 跨阶段保持（1.4.5.8 为静态方法）
            Register("Main.UpdateUIStates(GameTime)", GetStatic(main, "UpdateUIStates", typeof(GameTime)),
                (Action<Action<GameTime>, GameTime>)((orig, gameTime) =>
                {
                    UpdateUIStatesPrefix(gameTime);
                    orig(gameTime);
                    UpdateUIStatesPostfix(gameTime);
                }));

            // Main.DoUpdateInWorld()
            Register("Main.DoUpdateInWorld()", GetInstance(main, "DoUpdateInWorld"),
                (Action<Action<Main>, Main>)((orig, self) =>
                {
                    DoUpdateInWorldPrefix();
                    orig(self);
                    DoUpdateInWorldPostfix();
                }));

            // Main.DrawMap(GameTime)
            Register("Main.DrawMap(GameTime)", GetInstance(main, "DrawMap", typeof(GameTime)),
                (Action<Action<Main, GameTime>, Main, GameTime>)((orig, self, gameTime) =>
                {
                    orig(self, gameTime);
                    DrawMapPostfix(gameTime);
                }));

            // Main.DrawMenu(GameTime)
            Register("Main.DrawMenu(GameTime)", GetInstance(main, "DrawMenu", typeof(GameTime)),
                (Action<Action<Main, GameTime>, Main, GameTime>)((orig, self, gameTime) =>
                {
                    DrawMenuPrefix(gameTime);
                    orig(self, gameTime);
                    DrawMenuPostfix(gameTime);
                }));

            // Main.MouseText_DrawItemTooltip_GetLinesInfo（静态，ref 参数，自定义委托）
            Register("Main.MouseText_DrawItemTooltip_GetLinesInfo", MethodLookup.Static(main, "MouseText_DrawItemTooltip_GetLinesInfo",
                typeof(Item), typeof(int).MakeByRefType(), typeof(float), typeof(int).MakeByRefType(),
                typeof(string[]), typeof(Color[])),
                (Hook_MouseTextLines)MouseTextLinesHook);

            // Main.DoDraw(GameTime)
            Register("Main.DoDraw(GameTime)", GetInstance(main, "DoDraw", typeof(GameTime)),
                (Action<Action<Main, GameTime>, Main, GameTime>)((orig, self, gameTime) =>
                {
                    DoDrawPrefix(gameTime);
                    orig(self, gameTime);
                    DoDrawPostfix(gameTime);
                }));

            // Main.PlayerFocusedScreenPosition（静态，返回 Vector2）
            Register("Main.PlayerFocusedScreenPosition()", GetStatic(main, "PlayerFocusedScreenPosition"),
                (Func<Func<Vector2>, Vector2>)(orig =>
                {
                    Vector2 result = orig();
                    PlayerFocusedScreenPosition(ref result);
                    return result;
                }));

            // Main.ErasePlayer(int)（静态）
            Register("Main.ErasePlayer(int)", GetStatic(main, "ErasePlayer", typeof(int)),
                (Action<Action<int>, int>)((orig, i) =>
                {
                    ErasePlayerPrefix(i);
                    orig(i);
                }));

            // Main.EraseWorld(int)（静态）
            Register("Main.EraseWorld(int)", GetStatic(main, "EraseWorld", typeof(int)),
                (Action<Action<int>, int>)((orig, i) =>
                {
                    EraseWorldPrefix(i);
                    orig(i);
                }));
        }

        /// <summary>带诊断的注册：目标查找失败时输出具体方法名与运行时签名</summary>
        private static void Register(string what, MethodBase target, Delegate detour)
        {
            if (target == null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[Patch_Main] 目标方法查找失败: {what}");
                sb.AppendLine($"  typeof(GameTime): {typeof(GameTime).AssemblyQualifiedName}  @ {typeof(GameTime).Assembly.Location}");
                Type foundParamType = null;
                foreach (var m in typeof(Main).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
                {
                    if (m.Name == "Update")
                    {
                        var ps = m.GetParameters();
                        string[] arr = new string[ps.Length];
                        for (int i = 0; i < ps.Length; i++) arr[i] = ps[i].ParameterType.FullName;
                        sb.AppendLine("  Update(" + string.Join(", ", arr) + ")  decl=" + m.DeclaringType.FullName + " static=" + m.IsStatic);
                        foreach (var p in ps)
                        {
                            sb.AppendLine($"     param {p.Name}: {p.ParameterType.AssemblyQualifiedName}  @ {p.ParameterType.Assembly.Location}");
                            if (p.Name == "gameTime") foundParamType = p.ParameterType;
                        }
                    }
                }
                if (foundParamType != null)
                {
                    sb.AppendLine($"  ReferenceEquals(typeof(GameTime), param): {object.ReferenceEquals(typeof(GameTime), foundParamType)}");
                    sb.AppendLine($"  GetMethod(Update, {foundParamType.FullName}): {(typeof(Main).GetMethod("Update", new[] { foundParamType }) != null ? "FOUND" : "NULL")}");
                    sb.AppendLine($"  GetMethod(Update, noTypes): {(typeof(Main).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic) != null ? "FOUND" : "NULL")}");
                }
                sb.AppendLine($"  Main assembly: {(typeof(Main).Assembly.Location.Length > 0 ? typeof(Main).Assembly.Location : "(byte[] 加载，无位置)")}");
                TPML.Core.Logging.LogManager.GetLogger("Patch_Main").Error(sb.ToString());
                throw new MissingMethodException(what);
            }
            HookRegistry.Add(target, detour);
        }

        private static MethodInfo GetInstance(Type type, string name, params Type[] types)
        {
            return MethodLookup.Instance(type, name, types);
        }

        private static MethodInfo GetStatic(Type type, string name, params Type[] types)
        {
            return MethodLookup.Static(type, name, types);
        }

        private static void MouseTextLinesHook(Orig_MouseTextLines orig, Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Color[] lineColors)
        {
            if (item != null && item.type >= TPML.Content.ItemLoader.ModItemOffset)
            {
                TPML.Content.ItemLoader.EnsureArraySizes(item.type);
            }
            orig(item, ref yoyoLogo, oldKB, ref numLines, toolTipLine, lineColors);

            // 后缀期望全 ref；值参数（oldKB/toolTipLine/lineColors）用本地副本传递，改动不回写（与 Harmony 语义一致）
            float oldKBLocal = oldKB;
            string[] toolTipLineLocal = toolTipLine;
            Color[] lineColorsLocal = lineColors;
            MouseText_DrawItemTooltip_GetLinesInfoPostfix(item, ref yoyoLogo, ref oldKBLocal, ref numLines, ref toolTipLineLocal, ref lineColorsLocal);
        }

        public static void UpdatePrefix(GameTime gameTime)
        {
            try
            {
                float delta = gameTime != null ? (float)gameTime.ElapsedGameTime.TotalSeconds : 1f / 60f;
                PerformanceProfiler.Update(delta);

                UpdatePrefix_CanUpdateGameplay();

                if (PerformanceProfiler.IsEnabled)
                {
                    for (int i = 0; i < mod.Count; i++)
                    {
                        var item = mod[i];
                        if (item == null) continue;
                        using (PerformanceProfiler.Measure(item.GetType().Assembly.GetName().Name, item.GetType().Name + ".UpdatePrefix"))
                        {
                            try { item.UpdatePrefix(gameTime); }
                            catch (Exception ex) { OutputDebug.OutputException(ex, 2); }
                        }
                    }
                }
                else
                {
                    mod.ForTry(item => item.UpdatePrefix(gameTime));
                }
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

        public static void UpdatePostfix(GameTime gameTime)
        {
            if (PerformanceProfiler.IsEnabled)
            {
                for (int i = 0; i < mod.Count; i++)
                {
                    var item = mod[i];
                    if (item == null) continue;
                    using (PerformanceProfiler.Measure(item.GetType().Assembly.GetName().Name, item.GetType().Name + ".UpdatePostfix"))
                    {
                        try { item.UpdatePostfix(gameTime); }
                        catch (Exception ex) { OutputDebug.OutputException(ex, 2); }
                    }
                }
            }
            else
            {
                mod.ForTry(item => item.UpdatePostfix(gameTime));
            }
        }

        public static void SetupDrawInterfaceLayersPostfix()
        {
            try
            {
                List<GameInterfaceLayer> gameInterfaceLayers = Main.instance._gameInterfaceLayers;

                if (PerformanceProfiler.IsEnabled)
                {
                    for (int i = 0; i < mod.Count; i++)
                    {
                        var item = mod[i];
                        if (item == null) continue;
                        using (PerformanceProfiler.Measure(item.GetType().Assembly.GetName().Name, item.GetType().Name + ".SetupDrawInterfaceLayers"))
                        {
                            try { item.SetupDrawInterfaceLayersPostfix(gameInterfaceLayers); }
                            catch (Exception ex) { OutputDebug.OutputException(ex, 2); }
                        }
                    }
                }
                else
                {
                    mod.ForTry(item => item.SetupDrawInterfaceLayersPostfix(gameInterfaceLayers));
                }
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }

        private static int _preUpdateScrollWheelForUI = 0;

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

        public static void DoUpdateInWorldPrefix()
        {
            mod.ForTry(item => item.DoUpdateInWorldPrefix());
        }

        public static void DoUpdateInWorldPostfix()
        {
            mod.ForTry(item => item.DoUpdateInWorldPostfix());
        }

        public static void DrawMapPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMapPostfix(gameTime));
        }

        public static void DrawMenuPrefix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMenuPrefix(gameTime));
        }

        public static void DrawMenuPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMenuPostfix(gameTime));
        }

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

        public static void DoDrawPrefix(GameTime gameTime)
        {
            mod.ForTry(item => item.DoDrawPrefix(gameTime));
        }

        public static void DoDrawPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.DoDrawPostfix(gameTime));
        }

        public static void PlayerFocusedScreenPosition(ref Vector2 __result)
        {
            Vector2 origin = __result;
            Vector2 modifi = __result;

            mod.ForTry(item => modifi = item.PlayerFocusedScreenPosition(origin, modifi));

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
    }
}
