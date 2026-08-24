using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria.GameInput;
using Terraria.UI;

namespace Terraria.ModLoader.Engine
{
    /// <summary>
    /// tModLoader 兼容层按需动态 Harmony 钩子调度器
    /// </summary>
    public static class TModHookDispatcher
    {
        private static Harmony _harmony;
        private static bool _initialized = false;

        public static readonly List<ModPlayer> ActiveModPlayers = new List<ModPlayer>();
        public static readonly List<ModSystem> ActiveModSystems = new List<ModSystem>();
        public static readonly List<GlobalItem> ActiveGlobalItems = new List<GlobalItem>();

        private static bool _firstInvDrawLogged = false;

        public static void Initialize(string harmonyId = "Terraria.ModLoader.Shim.Dispatcher")
        {
            if (_initialized) return;
            _harmony = new Harmony(harmonyId);
            _initialized = true;
        }

        public static void RegisterHookInstances(IEnumerable<ILoadable> contents)
        {
            foreach (var item in contents)
            {
                if (item is ModPlayer player && !ActiveModPlayers.Contains(player))
                    ActiveModPlayers.Add(player);
                else if (item is ModSystem system && !ActiveModSystems.Contains(system))
                    ActiveModSystems.Add(system);
                else if (item is GlobalItem gItem && !ActiveGlobalItems.Contains(gItem))
                    ActiveGlobalItems.Add(gItem);
            }

            TModShimEngine.Log($"[TModHookDispatcher] 注册内容: ModPlayers={ActiveModPlayers.Count}, ModSystems={ActiveModSystems.Count}, GlobalItems={ActiveGlobalItems.Count}");
            ApplyOnDemandPatches();
        }

        public static void Clear()
        {
            ActiveModPlayers.Clear();
            ActiveModSystems.Clear();
            ActiveGlobalItems.Clear();
            _harmony?.UnpatchAll(_harmony.Id);
            _initialized = false;
            _firstInvDrawLogged = false;
        }

        private static bool HasOverride(Type type, string methodName, Type baseType)
        {
            var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null && method.DeclaringType != baseType;
        }

        private static void ApplyOnDemandPatches()
        {
            if (_harmony == null) Initialize();

            // 1. ModPlayer.PreUpdate & PostUpdate
            if (ActiveModPlayers.Any(p => HasOverride(p.GetType(), nameof(ModPlayer.PreUpdate), typeof(ModPlayer)) ||
                                         HasOverride(p.GetType(), nameof(ModPlayer.PostUpdate), typeof(ModPlayer))))
            {
                PatchPlayerUpdate();
            }

            // 2. ModPlayer.ProcessTriggers & Keybinds
            if (KeybindLoader.Keybinds.Count > 0 || ActiveModPlayers.Any(p => HasOverride(p.GetType(), nameof(ModPlayer.ProcessTriggers), typeof(ModPlayer))))
            {
                PatchInput();
            }

            // 3. ModPlayer.OnPickup 拾取拦截
            if (ActiveModPlayers.Any(p => HasOverride(p.GetType(), nameof(ModPlayer.OnPickup), typeof(ModPlayer))))
            {
                PatchPlayerPickup();
            }

            // 4. ModSystem.UpdateUI & PostUpdateEverything
            PatchUpdateHooks();

            // 5. ModSystem.ModifyInterfaceLayers (挂钩原版 SetupDrawInterfaceLayers)
            PatchInterfaceLayers();
        }

        #region Harmony Patches

        private static void PatchPlayerUpdate()
        {
            var target = typeof(Player).GetMethod(nameof(Player.Update), new[] { typeof(int) });
            var prefix = typeof(TModHookDispatcher).GetMethod(nameof(Player_Update_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            var postfix = typeof(TModHookDispatcher).GetMethod(nameof(Player_Update_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            _harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
            TModShimEngine.Log("[TModHookDispatcher] 已挂钩 Player.Update");
        }

        private static void Player_Update_Prefix(Player __instance, int i)
        {
            if (__instance != Main.LocalPlayer) return;
            for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
            {
                var mp = ActiveModPlayers[idx];
                mp.Player = __instance;
                mp.PreUpdate();
            }
        }

        private static void Player_Update_Postfix(Player __instance, int i)
        {
            if (__instance != Main.LocalPlayer) return;
            for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
            {
                var mp = ActiveModPlayers[idx];
                mp.Player = __instance;
                mp.PostUpdate();
            }
        }

        private static void PatchPlayerPickup()
        {
            var target = typeof(Player).GetMethod(nameof(Player.GetItem), new[] { typeof(int), typeof(Item), typeof(GetItemSettings) });
            if (target != null)
            {
                var prefix = typeof(TModHookDispatcher).GetMethod(nameof(Player_GetItem_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                TModShimEngine.Log("[TModHookDispatcher] 已挂钩 Player.GetItem (OnPickup)");
            }
        }

        private static bool Player_GetItem_Prefix(Player __instance, int plr, Item newItem, GetItemSettings settings, ref Item __result)
        {
            if (__instance != Main.LocalPlayer || newItem == null || newItem.IsAir) return true;

            for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
            {
                var mp = ActiveModPlayers[idx];
                mp.Player = __instance;
                try
                {
                    if (!mp.OnPickup(newItem))
                    {
                        __result = new Item();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    TModShimEngine.Log($"[TModHookDispatcher] OnPickup 异常: {ex.Message}");
                }
            }
            return true;
        }

        private static void PatchInput()
        {
            var target = typeof(PlayerInput).GetMethod(nameof(PlayerInput.UpdateInput), BindingFlags.Static | BindingFlags.Public);
            var postfix = typeof(TModHookDispatcher).GetMethod(nameof(PlayerInput_UpdateInput_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            TModShimEngine.Log("[TModHookDispatcher] 已挂钩 PlayerInput.UpdateInput");
        }

        private static void PlayerInput_UpdateInput_Postfix()
        {
            KeybindLoader.Update();
            TriggersSet triggers = PlayerInput.Triggers.Current;
            for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
            {
                ActiveModPlayers[idx].ProcessTriggers(triggers);
            }
        }

        private static void PatchUpdateHooks()
        {
            var target = typeof(Main).GetMethod("UpdateUIStates", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (target != null)
            {
                var postfix = typeof(TModHookDispatcher).GetMethod(nameof(Main_UpdateUIStates_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                TModShimEngine.Log("[TModHookDispatcher] 已挂钩 Main.UpdateUIStates");
            }
        }

        private static void Main_UpdateUIStates_Postfix(GameTime gameTime)
        {
            GameTime gt = gameTime ?? new GameTime();

            for (int idx = 0; idx < ActiveModSystems.Count; idx++)
            {
                var sys = ActiveModSystems[idx];
                try
                {
                    sys.UpdateUI(gt);
                    sys.PostUpdateEverything();
                }
                catch (Exception ex)
                {
                    TModShimEngine.Log($"[TModHookDispatcher] UpdateUI 异常: {ex.Message}");
                }
            }
        }

        private static void PatchInterfaceLayers()
        {
            var target = typeof(Main).GetMethod("SetupDrawInterfaceLayers", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (target != null)
            {
                var postfix = typeof(TModHookDispatcher).GetMethod(nameof(Main_SetupDrawInterfaceLayers_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                TModShimEngine.Log("[TModHookDispatcher] 已挂钩 Main.SetupDrawInterfaceLayers (原生图层管道接入)");
            }
            else
            {
                TModShimEngine.Log("[TModHookDispatcher] 未找到 Main.SetupDrawInterfaceLayers 方法！");
            }
        }

        private static void Main_SetupDrawInterfaceLayers_Postfix()
        {
            List<GameInterfaceLayer> gameInterfaceLayers = Main.instance?._gameInterfaceLayers;
            if (gameInterfaceLayers == null) return;

            for (int idx = 0; idx < ActiveModSystems.Count; idx++)
            {
                try
                {
                    ActiveModSystems[idx].ModifyInterfaceLayers(gameInterfaceLayers);
                }
                catch (Exception ex)
                {
                    TModShimEngine.Log($"[TModHookDispatcher] 执行 ModifyInterfaceLayers 异常: {ex.Message}");
                }
            }

            // 安全包裹所有模组图层，确保任何模组异常都不会中断后续原版图层（如鼠标文本渲染）
            for (int i = 0; i < gameInterfaceLayers.Count; i++)
            {
                var layer = gameInterfaceLayers[i];
                if (layer != null && !layer.Name.StartsWith("Vanilla:", StringComparison.OrdinalIgnoreCase) && !(layer is SafeGameInterfaceLayer))
                {
                    gameInterfaceLayers[i] = new SafeGameInterfaceLayer(layer);
                }
            }

            if (Main.playerInventory && !_firstInvDrawLogged)
            {
                TModShimEngine.Log($"[TModHookDispatcher] 原生图层已注入模组图层，当前总图层数={gameInterfaceLayers.Count}:");
                foreach (var l in gameInterfaceLayers)
                {
                    TModShimEngine.Log($"  - 图层: [{l.Name}]");
                }
                _firstInvDrawLogged = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// 安全图层包装器：捕获模组图层内部异常，绝不中断原版后续渲染管线
    /// </summary>
    internal class SafeGameInterfaceLayer : GameInterfaceLayer
    {
        private readonly GameInterfaceLayer _inner;
        private static int _errorThrottle = 0;

        public SafeGameInterfaceLayer(GameInterfaceLayer inner)
            : base(inner?.Name ?? "SafeModLayer", inner?.ScaleType ?? InterfaceScaleType.UI)
        {
            _inner = inner;
        }

        private static readonly MethodInfo _drawSelfMethod = typeof(GameInterfaceLayer).GetMethod("DrawSelf", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        protected override bool DrawSelf()
        {
            if (_inner == null) return true;
            try
            {
                if (_drawSelfMethod != null)
                {
                    return (bool)_drawSelfMethod.Invoke(_inner, null);
                }
                return true;
            }
            catch (Exception ex)
            {
                var realEx = ex.InnerException ?? ex;
                if (_errorThrottle++ % 60 == 0)
                {
                    TModShimEngine.Log($"[SafeLayer] 绘制模组图层 [{_inner.Name}] 捕获异常: {realEx.GetType().FullName}: {realEx.Message}\n{realEx.StackTrace}");
                }
                return true;
            }
        }
    }
}
