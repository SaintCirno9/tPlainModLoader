using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Items;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using TPML.Content.UI;
using TPML.Core.Diagnostics;
using TPML.Core.Logging;

namespace TPML.Content.Engine
{
    /// <summary>
    /// TPML 原生内容引擎核心 MonoMod 钩子分发与生命周期调度器（自 Harmony 迁移，M2）
    /// </summary>
    public static class ContentHookDispatcher
    {
        private static bool _initialized = false;
        private static bool _patchesApplied = false;

        public static readonly List<ModPlayer> ActiveModPlayers = new List<ModPlayer>();
        public static readonly List<ModSystem> ActiveModSystems = new List<ModSystem>();
        public static readonly List<GlobalItem> ActiveGlobalItems = new List<GlobalItem>();
        public static readonly List<GlobalNPC> ActiveGlobalNPCs = new List<GlobalNPC>();

        private static bool _firstInvDrawLogged = false;

        #region Tooltip 管线自定义委托（原方法含 ref 参数，Action/Func 无法表达）

        private delegate void Orig_TooltipLines(Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Color[] lineColors);
        private delegate void Hook_TooltipLines(Orig_TooltipLines orig, Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Color[] lineColors);

        #endregion

        private static readonly ILogger Logger = LogManager.GetLogger("ContentHookDispatcher");

        public static void Initialize(string harmonyId = "TPML.Content.HookDispatcher")
        {
            if (_initialized) return;
            _initialized = true;

            if (!_patchesApplied)
            {
                Logger.Info("[ContentHookDispatcher] 正在应用原生内容引擎核心钩子与背包融合矩阵...");
                ApplyOnDemandPatches();
                _patchesApplied = true;
                Logger.Info("[ContentHookDispatcher] 原生内容引擎核心钩子与背包融合矩阵应用完毕");
            }
        }

        public static void RegisterHookInstances(IEnumerable<ILoadable> contents)
        {
            if (!_initialized) Initialize();

            foreach (var item in contents)
            {
                if (item is ModPlayer player && !ActiveModPlayers.Contains(player))
                {
                    ActiveModPlayers.Add(player);
                }
                else if (item is ModSystem system && !ActiveModSystems.Contains(system))
                {
                    ActiveModSystems.Add(system);
                }
                else if (item is GlobalItem gItem && !ActiveGlobalItems.Contains(gItem))
                {
                    ActiveGlobalItems.Add(gItem);
                }
                else if (item is GlobalNPC gNpc && !ActiveGlobalNPCs.Contains(gNpc))
                {
                    ActiveGlobalNPCs.Add(gNpc);
                }
            }

            if (!_patchesApplied)
            {
                ApplyOnDemandPatches();
                _patchesApplied = true;
            }
        }

        public static void Clear()
        {
            ActiveModPlayers.Clear();
            ActiveModSystems.Clear();
            ActiveGlobalItems.Clear();
            ActiveGlobalNPCs.Clear();
            TPML.Content.Fusion.InventoryFusionManager.Clear();
            TPML.Content.Fusion.UnifiedInventoryFusionHooks.UnregisterAll();
            HookRegistry.Clear(HookScope.Content);
            _initialized = false;
            _patchesApplied = false;
            _firstInvDrawLogged = false;
        }

        private static bool HasOverride(Type type, string methodName, Type baseType)
        {
            var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null && method.DeclaringType != baseType;
        }

        private static void ApplyOnDemandPatches()
        {
            // 自定义物品必须在原版 Item.SetDefaults 入口处短路到 ItemLoader
            PatchItemDefaults();

            // 挂钩物品 Tooltip 生成管线 (支持 ItemLoader.GetTooltip 和 ModItem.ModifyTooltips)
            PatchItemTooltips();

            // 挂钩物品射击与使用动作拦截 (拦截原版木箭弹幕并分发给 ModItem.Shoot)
            PatchItemShoot();

            // 挂钩物品使用动作 (ModItem.UseItem)
            PatchItemUse();

            // 挂钩物品使用许可检查 (ModItem.CanUseItem)
            PatchItemCheck();

            // 挂钩物品克隆 (ModItem 实体数据保持)
            PatchItemClone();

            // 1. ModPlayer.PreUpdate & PostUpdate
            PatchPlayerUpdate();
            PatchPlayerKill();
            PatchInput();
            PatchPlayerPickup();
            PatchUpdateHooks();
            PatchInterfaceLayers();
            PatchDrawExceptions();
            PatchLang();
            PatchPopupText();

            // 框架级全量背包融合系统 Hook 门控矩阵 (通用外部容器/魔杖/油漆/HasItem/ConsumeItem)
            TPML.Content.Fusion.UnifiedInventoryFusionHooks.RegisterAll();

            _patchesApplied = true;
        }

        #region Item 系列

        private static void PatchItemClone()
        {
            var target = typeof(Item).GetMethod(nameof(Item.Clone), BindingFlags.Instance | BindingFlags.Public);
            if (target != null)
            {
                HookRegistry.AddContent(target, (Func<Func<Item, Item>, Item, Item>)((orig, self) =>
                {
                    Item result = orig(self);
                    Item_Clone_Postfix(self, result);
                    return result;
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Item.Clone (ModItem 实体克隆与数据保持)");
            }
        }

        private static void Item_Clone_Postfix(Item __instance, Item __result)
        {
            if (__instance == null || __result == null) return;
            ItemLoader.OnItemCloned(__instance, __result);
        }

        private static void PatchItemDefaults()
        {
            // Item.SetDefaults 与 Item.netDefaults 已由 Cecil Prepatcher 在启动期织入头部原生短路拦截
            // (ItemLoader.OnSetDefaultsPrefix)，彻底杜绝 JIT 内联与运行时 Detour 冲突
        }

        private static bool Item_SetDefaults_Prefix(Item __instance, int Type)
        {
            var modItem = ItemLoader.GetItem(Type);
            if (modItem == null)
            {
                ModLoader.Log($"[SetDefaults-Prefix] type={Type} modItem=NULL -> 走原版");
                return true;
            }

            ModLoader.Log($"[SetDefaults-Prefix] type={Type} modItem={modItem.FullName} -> 设type跳过原版");
            __instance.type = Type;
            __instance.stack = 1;
            __instance.prefix = 0;
            ItemLoader.SetDefaults(__instance);
            return false;
        }

        private static bool Item_NetDefaults_Prefix(Item __instance, int type)
        {
            return Item_SetDefaults_Prefix(__instance, type);
        }

        private static void PatchItemTooltips()
        {
            var target = MethodLookup.Static(typeof(Main), nameof(Main.MouseText_DrawItemTooltip_GetLinesInfo),
                typeof(Item), typeof(int).MakeByRefType(), typeof(float), typeof(int).MakeByRefType(), typeof(string[]), typeof(Microsoft.Xna.Framework.Color[]));
            if (target != null)
            {
                HookRegistry.AddContent(target, (Hook_TooltipLines)TooltipLinesHook);
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Main.MouseText_DrawItemTooltip_GetLinesInfo (Tooltip 支持)");
            }
        }

        private static void TooltipLinesHook(Orig_TooltipLines orig, Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Color[] lineColors)
        {
            if (item != null && item.type >= ItemLoader.ModItemOffset)
            {
                ItemLoader.EnsureArraySizes(item.type);
            }
            orig(item, ref yoyoLogo, oldKB, ref numLines, toolTipLine, lineColors);
            Main_MouseText_DrawItemTooltip_GetLinesInfo_Postfix(item, ref yoyoLogo, oldKB, ref numLines, toolTipLine, lineColors);
        }

        private static void Main_MouseText_DrawItemTooltip_GetLinesInfo_Postfix(Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Microsoft.Xna.Framework.Color[] lineColors)
        {
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
                                lineColors[numLines] = Microsoft.Xna.Framework.Color.White;
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
        }

        private static void PatchItemShoot()
        {
            var target = MethodLookup.Instance(typeof(Player), nameof(Player.ItemCheck_Shoot), typeof(int), typeof(Item), typeof(int), typeof(bool));
            if (target != null)
            {
                HookRegistry.AddContent(target, (Action<Action<Player, int, Item, int, bool>, Player, int, Item, int, bool>)((orig, self, i, sItem, weaponDamage, withAudioVisualFeedback) =>
                {
                    if (!Player_ItemCheck_Shoot_Prefix(self, i, sItem, weaponDamage)) return;
                    orig(self, i, sItem, weaponDamage, withAudioVisualFeedback);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.ItemCheck_Shoot (物品射击与动作拦截)");
            }
        }

        private static bool Player_ItemCheck_Shoot_Prefix(Player __instance, int i, Item sItem, int weaponDamage)
        {
            if (sItem == null || sItem.IsAir) return true;
            var modItem = ItemLoader.GetItem(sItem.type);
            if (modItem != null)
            {
                Vector2 position = __instance.RotatedRelativePoint(__instance.MountedCenter, true);
                Vector2 velocity = Vector2.Normalize(Main.MouseWorld - position) * (sItem.shootSpeed > 0 ? sItem.shootSpeed : 5f);
                var source = new EntitySource_ItemUse_WithAmmo(__instance, sItem, 0);
                bool canShootVanilla = ItemLoader.Shoot(sItem, __instance, source, position, velocity, sItem.shoot, weaponDamage, sItem.knockBack);
                if (!canShootVanilla)
                {
                    return false; // 拦截原版弹幕生成
                }
            }
            return true;
        }

        private static void PatchItemUse()
        {
            var target = typeof(Player).GetMethod(
                nameof(Player.ItemCheck_StartActualUse),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (target != null)
            {
                HookRegistry.AddContent(target, (Action<Action<Player, Item>, Player, Item>)((orig, self, sItem) =>
                {
                    orig(self, sItem);
                    Player_ItemCheck_StartActualUse_Postfix(self, sItem);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.ItemCheck_StartActualUse (物品使用逻辑)");
            }
        }

        private static void Player_ItemCheck_StartActualUse_Postfix(Player __instance, Item sItem)
        {
            if (sItem == null || sItem.IsAir) return;
            ItemLoader.UseItem(sItem, __instance);
        }

        private static void PatchItemCheck()
        {
            // 挂在原版真正的使用许可检查上，而不是 ItemCheck 动画峰值（常规路径 itemAnimation 仍为 0，旧条件恒不成立）
            var target = MethodLookup.Instance(typeof(Player), "ItemCheck_CheckCanUse_Inner", typeof(Item), typeof(bool));
            if (target != null)
            {
                HookRegistry.AddContent(target, (Func<Func<Player, Item, bool, bool>, Player, Item, bool, bool>)((orig, self, sItem, ignoreCursed) =>
                {
                    // 彻底对齐 tML 官方 CombinedHooks.CanUseItem 规则：空物品或堆叠 <= 0 的幽灵物品直接禁止使用
                    if (sItem == null || sItem.IsAir || sItem.stack <= 0 || sItem.type <= 0)
                    {
                        return false;
                    }

                    bool result = orig(self, sItem, ignoreCursed);
                    if (!result || self == null) return result;
                    bool? canUse = ItemLoader.CanUseItem(sItem, self);
                    return canUse != false;
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.ItemCheck_CheckCanUse_Inner (CanUseItem 检查)");
            }
        }

        #endregion

        #region MonoMod Hooks

        private static void PatchPlayerUpdate()
        {
            var target = MethodLookup.Instance(typeof(Player), nameof(Player.Update), typeof(int));
            HookRegistry.AddContent(target, (Action<Action<Player, int>, Player, int>)((orig, self, i) =>
            {
                Player_Update_Prefix(self, i);
                orig(self, i);
                Player_Update_Postfix(self, i);
            }));
            ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.Update");
        }

        private static void PatchPlayerKill()
        {
            var target = MethodLookup.Instance(typeof(Player), nameof(Player.KillMe), typeof(PlayerDeathReason), typeof(double), typeof(int), typeof(bool));
            if (target != null)
            {
                HookRegistry.AddContent(target, (Action<Action<Player, PlayerDeathReason, double, int, bool>, Player, PlayerDeathReason, double, int, bool>)((orig, self, damageSource, dmg, hitDirection, pvp) =>
                {
                    bool playSound = true;
                    bool genDust = true;
                    bool continueKill = true;

                    for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
                    {
                        var mp = ActiveModPlayers[idx];
                        mp.Player = self;
                        try
                        {
                            if (!mp.PreKill(dmg, hitDirection, pvp, ref playSound, ref genDust, ref damageSource))
                            {
                                continueKill = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            ModLoader.Log($"[ContentHookDispatcher] ModPlayer.PreKill 异常: {ex.Message}");
                        }
                    }

                    if (!continueKill) return;

                    orig(self, damageSource, dmg, hitDirection, pvp);

                    for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
                    {
                        var mp = ActiveModPlayers[idx];
                        mp.Player = self;
                        try
                        {
                            mp.Kill(dmg, hitDirection, pvp, damageSource);
                        }
                        catch (Exception ex)
                        {
                            ModLoader.Log($"[ContentHookDispatcher] ModPlayer.Kill 异常: {ex.Message}");
                        }
                    }
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.KillMe (ModPlayer.PreKill/Kill 派发)");
            }
        }

        private static void Player_Update_Prefix(Player __instance, int i)
        {
            if (__instance == null) return;

            // 确保玩家 adjTile 数组容量满足模组物块需求，防止 UpdateRecipeList 判定配方时越界
            if (__instance.adjTile == null || __instance.adjTile.Length <= TileLoader.TileCount)
            {
                int req = Math.Max(TileLoader.TileCount + 64, 800);
                int cur = __instance.adjTile?.Length ?? 0;
                bool[] newAdj = new bool[Math.Max(req, cur * 2)];
                if (__instance.adjTile != null)
                {
                    Array.Copy(__instance.adjTile, newAdj, __instance.adjTile.Length);
                }
                __instance.adjTile = newAdj;
            }

            if (__instance != Main.LocalPlayer) return;

            for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
            {
                var mp = ActiveModPlayers[idx];
                mp.Player = __instance;
                if (PerformanceProfiler.IsEnabled)
                {
                    using (PerformanceProfiler.Measure(mp.Mod?.Name ?? mp.GetType().Assembly.GetName().Name, mp.GetType().Name + ".PreUpdate"))
                    {
                        mp.PreUpdate();
                    }
                }
                else
                {
                    mp.PreUpdate();
                }
            }
        }

        private static void Player_Update_Postfix(Player __instance, int i)
        {
            if (__instance != Main.LocalPlayer) return;

            // 保持鼠标物品与 58 槽位无 stack <= 0 幽灵残留（对齐 tML）
            if (Main.mouseItem != null && Main.mouseItem.type > 0 && Main.mouseItem.stack <= 0)
            {
                Main.mouseItem.TurnToAir();
            }
            if (__instance.inventory != null && __instance.inventory.Length > 58 && __instance.inventory[58] != null && __instance.inventory[58].type > 0 && __instance.inventory[58].stack <= 0)
            {
                __instance.inventory[58].TurnToAir();
            }

            if (__instance.HeldItem != null && !__instance.HeldItem.IsAir)
            {
                ItemLoader.HoldItem(__instance.HeldItem, __instance);
            }

            // 防御失焦/无物理按键时的 controlUseItem 残留，保护消耗品手持安全
            if (!Main.mouseLeft && !Main.mouseRight && __instance.itemAnimation == 0)
            {
                __instance.controlUseItem = false;
            }

            for (int idx = 0; idx < ActiveModPlayers.Count; idx++)
            {
                var mp = ActiveModPlayers[idx];
                mp.Player = __instance;
                if (PerformanceProfiler.IsEnabled)
                {
                    using (PerformanceProfiler.Measure(mp.Mod?.Name ?? mp.GetType().Assembly.GetName().Name, mp.GetType().Name + ".PostUpdate"))
                    {
                        mp.PostUpdate();
                    }
                }
                else
                {
                    mp.PostUpdate();
                }
            }
        }

        private static void PatchPlayerPickup()
        {
            // 1.4.5.8: Player.GetItem(Item newItem, GetItemSettings settings)（无 plr 参数）
            var target = MethodLookup.Instance(typeof(Player), nameof(Player.GetItem), typeof(Item), typeof(GetItemSettings));
            if (target != null)
            {
                HookRegistry.AddContent(target, (Func<Func<Player, Item, GetItemSettings, Item>, Player, Item, GetItemSettings, Item>)((orig, self, newItem, settings) =>
                {
                    Item result = null;
                    if (!Player_GetItem_Prefix(self, newItem, settings, ref result)) return result;
                    return orig(self, newItem, settings);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.GetItem (OnPickup)");
            }
        }

        private static bool Player_GetItem_Prefix(Player __instance, Item newItem, GetItemSettings settings, ref Item __result)
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
                    ModLoader.Log($"[ContentHookDispatcher] OnPickup 异常: {ex.Message}");
                }
            }
            return true;
        }

        private static void PatchInput()
        {
            var target = typeof(PlayerInput).GetMethod(nameof(PlayerInput.UpdateInput), BindingFlags.Static | BindingFlags.Public);
            HookRegistry.AddContent(target, (Action<Action>)(orig =>
            {
                orig();
                PlayerInput_UpdateInput_Postfix();
            }));
            ModLoader.Log("[ContentHookDispatcher] 已挂钩 PlayerInput.UpdateInput");
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
            // 1.4.5.8 中 Main.UpdateUIStates 为静态方法
            var target = typeof(Main).GetMethod("UpdateUIStates", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (target != null)
            {
                HookRegistry.AddContent(target, (Action<Action<GameTime>, GameTime>)((orig, gameTime) =>
                {
                    orig(gameTime);
                    Main_UpdateUIStates_Postfix(gameTime);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Main.UpdateUIStates");
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
                    if (PerformanceProfiler.IsEnabled)
                    {
                        using (PerformanceProfiler.Measure(sys.Mod?.Name ?? sys.GetType().Assembly.GetName().Name, sys.GetType().Name + ".UpdateUI"))
                        {
                            sys.UpdateUI(gt);
                            sys.PostUpdateEverything();
                        }
                    }
                    else
                    {
                        sys.UpdateUI(gt);
                        sys.PostUpdateEverything();
                    }
                }
                catch (Exception ex)
                {
                    ModLoader.Log($"[ContentHookDispatcher] UpdateUI 异常: {ex.Message}");
                }
            }
        }

        private static void PatchInterfaceLayers()
        {
            var target = typeof(Main).GetMethod("SetupDrawInterfaceLayers", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (target != null)
            {
                HookRegistry.AddContent(target, (Action<Action<Main>, Main>)((orig, self) =>
                {
                    orig(self);
                    Main_SetupDrawInterfaceLayers_Postfix();
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Main.SetupDrawInterfaceLayers (原生图层管道接入)");
            }
            else
            {
                ModLoader.Log("[ContentHookDispatcher] 未找到 Main.SetupDrawInterfaceLayers 方法！");
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
                    ModLoader.Log($"[ContentHookDispatcher] 执行 ModifyInterfaceLayers 异常: {ex.Message}");
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
                ModLoader.Log($"[ContentHookDispatcher] 原生图层已注入模组图层，当前总图层数={gameInterfaceLayers.Count}:");
                foreach (var l in gameInterfaceLayers)
                {
                    ModLoader.Log($"  - 图层: [{l.Name}]");
                }
                _firstInvDrawLogged = true;
            }
        }

        private static void PatchDrawExceptions()
        {
            var drawExTarget = typeof(TimeLogger).GetMethod(nameof(TimeLogger.DrawException), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (drawExTarget != null)
            {
                HookRegistry.AddContent(drawExTarget, (Action<Action<Exception>, Exception>)((orig, ex) =>
                {
                    Logger.Error($"[TimeLogger.DrawException] 捕获到 UI/绘制管线异常:\n{ex}");
                    orig(ex);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 TimeLogger.DrawException (全局绘制异常捕获与日志记录)");
            }

            var drawInvTarget = typeof(Main).GetMethod("DrawInventory", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (drawInvTarget != null)
            {
                HookRegistry.AddContent(drawInvTarget, (Action<Action<Main>, Main>)((orig, self) =>
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
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Main.DrawInventory (物品栏绘制异常捕获)");
            }
        }

        #endregion

        #region Lang 语言与文本安全拦截

        private static void PatchLang()
        {
            var getItemNameValue = MethodLookup.Static(typeof(Lang), nameof(Lang.GetItemNameValue), typeof(int));
            if (getItemNameValue != null)
            {
                HookRegistry.AddContent(getItemNameValue, (Func<Func<int, string>, int, string>)((orig, id) =>
                {
                    string result = null;
                    if (!Lang_GetItemNameValue_Prefix(id, ref result)) return result;
                    return orig(id);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Lang.GetItemNameValue(int)");
            }

            var getItemName = MethodLookup.Static(typeof(Lang), nameof(Lang.GetItemName), typeof(int));
            if (getItemName != null)
            {
                HookRegistry.AddContent(getItemName, (Func<Func<int, LocalizedText>, int, LocalizedText>)((orig, id) =>
                {
                    LocalizedText result = null;
                    if (!Lang_GetItemName_Prefix(id, ref result)) return result;
                    return orig(id);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Lang.GetItemName(int)");
            }

            var getPrefixedItemName = MethodLookup.Static(typeof(Lang), nameof(Lang.GetPrefixedItemName), typeof(int), typeof(int));
            if (getPrefixedItemName != null)
            {
                HookRegistry.AddContent(getPrefixedItemName, (Func<Func<int, int, string>, int, int, string>)((orig, id, prefixType) =>
                {
                    string result = null;
                    if (!Lang_GetPrefixedItemName_Prefix(id, prefixType, ref result)) return result;
                    return orig(id, prefixType);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Lang.GetPrefixedItemName(int, int)");
            }

            var getTooltip = MethodLookup.Static(typeof(Lang), nameof(Lang.GetTooltip), typeof(int));
            if (getTooltip != null)
            {
                HookRegistry.AddContent(getTooltip, (Func<Func<int, ItemTooltip>, int, ItemTooltip>)((orig, itemId) =>
                {
                    ItemTooltip result = null;
                    if (!Lang_GetTooltip_Prefix(itemId, ref result)) return result;
                    return orig(itemId);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Lang.GetTooltip(int)");
            }
        }

        private static bool Lang_GetItemNameValue_Prefix(int id, ref string __result)
        {
            if (id >= ItemLoader.ModItemOffset)
            {
                string name = ItemLoader.GetDisplayName(id);
                if (!string.IsNullOrEmpty(name))
                {
                    __result = name;
                    return false;
                }
            }
            return true;
        }

        private static bool Lang_GetItemName_Prefix(int id, ref Terraria.Localization.LocalizedText __result)
        {
            if (id >= ItemLoader.ModItemOffset)
            {
                string name = ItemLoader.GetDisplayName(id);
                if (!string.IsNullOrEmpty(name))
                {
                    __result = new Terraria.Localization.LocalizedText($"ItemName.{id}", name);
                    return false;
                }
            }
            return true;
        }

        private static bool Lang_GetPrefixedItemName_Prefix(int id, int prefixType, ref string __result)
        {
            if (id >= ItemLoader.ModItemOffset)
            {
                string baseName = ItemLoader.GetDisplayName(id);
                if (prefixType > 0 && prefixType < Lang.prefix.Length)
                {
                    string pName = Lang.prefix[prefixType]?.Value ?? string.Empty;
                    __result = string.IsNullOrEmpty(pName) ? baseName : $"{pName} {baseName}";
                }
                else
                {
                    __result = baseName;
                }
                return false;
            }
            return true;
        }

        private static bool Lang_GetTooltip_Prefix(int itemId, ref ItemTooltip __result)
        {
            if (itemId >= ItemLoader.ModItemOffset)
            {
                string tip = ItemLoader.GetTooltip(itemId);
                if (string.IsNullOrEmpty(tip))
                {
                    __result = ItemTooltip.None;
                }
                else
                {
                    string[] lines = tip.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    __result = ItemTooltip.FromHardcodedText(lines);
                }
                return false;
            }
            return true;
        }

        private static void PatchPopupText()
        {
            var newTextMethod = MethodLookup.Static(typeof(PopupText), nameof(PopupText.NewText), typeof(PopupTextContext), typeof(Item), typeof(Vector2), typeof(int), typeof(bool), typeof(bool));
            if (newTextMethod != null)
            {
                HookRegistry.AddContent(newTextMethod, (Func<Func<PopupTextContext, Item, Vector2, int, bool, bool, int>, PopupTextContext, Item, Vector2, int, bool, bool, int>)((orig, context, newItem, position, stack, noStack, longText) =>
                {
                    if (!PopupText_NewText_Prefix(context, newItem, position, stack, noStack, longText)) return 0;
                    return orig(context, newItem, position, stack, noStack, longText);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 PopupText.NewText(PopupTextContext, Item, Vector2, int, bool, bool)");
            }

            // PopupText.Update(int) 为实例方法
            var updateMethod = MethodLookup.Instance(typeof(PopupText), nameof(PopupText.Update), typeof(int));
            if (updateMethod != null)
            {
                HookRegistry.AddContent(updateMethod, (Action<Action<PopupText, int>, PopupText, int>)((orig, self, whoAmI) =>
                {
                    if (!PopupText_Update_Prefix(whoAmI)) return;
                    orig(self, whoAmI);
                }));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 PopupText.Update(int) [空引用终极防护]");
            }
        }

        private static bool PopupText_NewText_Prefix(PopupTextContext context, Item newItem, Vector2 position, int stack, bool noStack, bool longText)
        {
            if (newItem == null) return false;
            if (newItem.type >= ItemLoader.ModItemOffset)
            {
                if (string.IsNullOrEmpty(newItem.Name))
                {
                    string name = ItemLoader.GetDisplayName(newItem.type);
                    newItem.SetNameOverride(name);
                }
            }
            return true;
        }

        private static bool PopupText_Update_Prefix(int whoAmI)
        {
            if (whoAmI < 0 || whoAmI >= PopupText.popupText.Length) return true;
            var text = PopupText.popupText[whoAmI];
            if (text == null || !text.active) return true;

            // 终极安全防线：若 displayText 为 null，自动尝试使用 name 补全，若仍为空则注销该槽位
            if (text.displayText == null)
            {
                if (!string.IsNullOrEmpty(text.name))
                {
                    text.displayText = text.name;
                    if (text.stack > 1)
                    {
                        text.displayText += " (" + text.stack + ")";
                    }
                }
                else
                {
                    text.active = false;
                    return false;
                }
            }
            return true;
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
                    if (PerformanceProfiler.IsEnabled)
                    {
                        using (PerformanceProfiler.Measure("UILayers", _inner.Name))
                        {
                            return (bool)_drawSelfMethod.Invoke(_inner, null);
                        }
                    }
                    return (bool)_drawSelfMethod.Invoke(_inner, null);
                }
                return true;
            }
            catch (Exception ex)
            {
                var realEx = ex.InnerException ?? ex;
                if (_errorThrottle++ % 60 == 0)
                {
                    ModLoader.Log($"[SafeLayer] 绘制模组图层 [{_inner.Name}] 捕获异常: {realEx.GetType().FullName}: {realEx.Message}\n{realEx.StackTrace}");
                }
                return true;
            }
        }
    }
}
