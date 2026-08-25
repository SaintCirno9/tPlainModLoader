using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Items;
using Terraria.GameInput;
using Terraria.UI;
using TPML.Content.UI;

namespace TPML.Content.Engine
{
    /// <summary>
    /// TPML 原生内容引擎核心 Harmony 钩子分发与生命周期调度器
    /// </summary>
    public static class ContentHookDispatcher
    {
        private static Harmony _harmony;
        private static bool _initialized = false;
        private static bool _patchesApplied = false;

        public static readonly List<ModPlayer> ActiveModPlayers = new List<ModPlayer>();
        public static readonly List<ModSystem> ActiveModSystems = new List<ModSystem>();
        public static readonly List<GlobalItem> ActiveGlobalItems = new List<GlobalItem>();

        private static bool _firstInvDrawLogged = false;

        public static void Initialize(string harmonyId = "TPML.Content.HookDispatcher")
        {
            if (_initialized) return;
            _harmony = new Harmony(harmonyId);
            _initialized = true;
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
            TPML.Content.Fusion.InventoryFusionManager.Clear();
            _harmony?.UnpatchAll(_harmony.Id);
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
            if (_harmony == null) Initialize();

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
            PatchInput();
            PatchPlayerPickup();
            PatchUpdateHooks();
            PatchInterfaceLayers();
            PatchLang();
            PatchPopupText();

            // 框架级全量背包融合系统补丁矩阵 (通用外部容器/魔杖/油漆/HasItem/ConsumeItem)
            _harmony.CreateClassProcessor(typeof(TPML.Content.Fusion.Patch_UnifiedInventoryFusion)).Patch();

            _patchesApplied = true;
        }

        private static void PatchItemClone()
        {
            var target = typeof(Item).GetMethod(nameof(Item.Clone), BindingFlags.Instance | BindingFlags.Public);
            if (target != null)
            {
                var postfix = typeof(ContentHookDispatcher).GetMethod(nameof(Item_Clone_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
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
            var prefixSet = typeof(ContentHookDispatcher).GetMethod(
                nameof(Item_SetDefaults_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var setDefaults = typeof(Item).GetMethod(
                nameof(Item.SetDefaults),
                new[] { typeof(int), typeof(ItemVariant) });
            if (setDefaults != null)
            {
                _harmony.Patch(setDefaults, prefix: new HarmonyMethod(prefixSet));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Item.SetDefaults(int, ItemVariant)");
            }
            else
            {
                ModLoader.Log("[ContentHookDispatcher] 错误: 未能找到 Item.SetDefaults(int, ItemVariant)!");
            }

            var prefixNet = typeof(ContentHookDispatcher).GetMethod(
                nameof(Item_NetDefaults_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var netDefaults = typeof(Item).GetMethod(
                nameof(Item.netDefaults),
                new[] { typeof(int) });
            if (netDefaults != null)
            {
                _harmony.Patch(netDefaults, prefix: new HarmonyMethod(prefixNet));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Item.netDefaults(int)");
            }
        }

        private static bool Item_SetDefaults_Prefix(Item __instance, int Type)
        {
            var modItem = ItemLoader.GetItem(Type);
            if (modItem == null)
                return true;

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
            var target = typeof(Main).GetMethod(
                nameof(Main.MouseText_DrawItemTooltip_GetLinesInfo),
                new[] { typeof(Item), typeof(int).MakeByRefType(), typeof(float), typeof(int).MakeByRefType(), typeof(string[]), typeof(Microsoft.Xna.Framework.Color[]) });
            if (target != null)
            {
                var postfix = typeof(ContentHookDispatcher).GetMethod(nameof(Main_MouseText_DrawItemTooltip_GetLinesInfo_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Main.MouseText_DrawItemTooltip_GetLinesInfo (Tooltip 支持)");
            }
        }

        private static void Main_MouseText_DrawItemTooltip_GetLinesInfo_Postfix(Item item, ref int yoyoLogo, float oldKB, ref int numLines, string[] toolTipLine, Microsoft.Xna.Framework.Color[] lineColors)
        {
            if (item == null || item.IsAir) return;

            var modItem = ItemLoader.GetItem(item.type);
            string baseTip = ItemLoader.GetTooltip(item.type);

            var list = new List<TooltipLine>();
            if (!string.IsNullOrEmpty(baseTip))
            {
                var lines = baseTip.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    list.Add(new TooltipLine(modItem?.Mod ?? null, $"Tooltip{i}", lines[i]));
                }
            }

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
            var target = typeof(Player).GetMethod(
                nameof(Player.ItemCheck_Shoot),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (target != null)
            {
                var prefix = typeof(ContentHookDispatcher).GetMethod(nameof(Player_ItemCheck_Shoot_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.ItemCheck_Shoot (物品射击与动作拦截)");
            }
        }

        private static bool Player_ItemCheck_Shoot_Prefix(Player __instance, int i, Item sItem, int weaponDamage, bool withAudioVisualFeedback)
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
                var postfix = typeof(ContentHookDispatcher).GetMethod(nameof(Player_ItemCheck_StartActualUse_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
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
            var target = typeof(Player).GetMethod(
                nameof(Player.ItemCheck),
                BindingFlags.Instance | BindingFlags.Public);
            if (target != null)
            {
                var prefix = typeof(ContentHookDispatcher).GetMethod(nameof(Player_ItemCheck_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.ItemCheck (CanUseItem 检查)");
            }
        }

        private static bool Player_ItemCheck_Prefix(Player __instance)
        {
            if (__instance.CCed) return true;
            Item item = __instance.inventory[__instance.selectedItem];
            if (item != null && !item.IsAir && __instance.itemAnimation > 0 && __instance.itemAnimation == __instance.itemAnimationMax)
            {
                bool? canUse = ItemLoader.CanUseItem(item, __instance);
                if (canUse == false)
                {
                    __instance.itemAnimation = 0;
                    __instance.itemTime = 0;
                    return false;
                }

                ItemLoader.UseItem(item, __instance);
            }
            return true;
        }

        #region Harmony Patches

        private static void PatchPlayerUpdate()
        {
            var target = typeof(Player).GetMethod(nameof(Player.Update), new[] { typeof(int) });
            var prefix = typeof(ContentHookDispatcher).GetMethod(nameof(Player_Update_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
            var postfix = typeof(ContentHookDispatcher).GetMethod(nameof(Player_Update_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            _harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
            ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.Update");
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
                mp.PostUpdate();
            }
        }

        private static void PatchPlayerPickup()
        {
            var target = typeof(Player).GetMethod(nameof(Player.GetItem), new[] { typeof(int), typeof(Item), typeof(GetItemSettings) });
            if (target != null)
            {
                var prefix = typeof(ContentHookDispatcher).GetMethod(nameof(Player_GetItem_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Player.GetItem (OnPickup)");
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
                    ModLoader.Log($"[ContentHookDispatcher] OnPickup 异常: {ex.Message}");
                }
            }
            return true;
        }

        private static void PatchInput()
        {
            var target = typeof(PlayerInput).GetMethod(nameof(PlayerInput.UpdateInput), BindingFlags.Static | BindingFlags.Public);
            var postfix = typeof(ContentHookDispatcher).GetMethod(nameof(PlayerInput_UpdateInput_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
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
            var target = typeof(Main).GetMethod("UpdateUIStates", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (target != null)
            {
                var postfix = typeof(ContentHookDispatcher).GetMethod(nameof(Main_UpdateUIStates_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
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
                    sys.UpdateUI(gt);
                    sys.PostUpdateEverything();
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
                var postfix = typeof(ContentHookDispatcher).GetMethod(nameof(Main_SetupDrawInterfaceLayers_Postfix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
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

        #endregion

        #region Lang 语言与文本安全拦截

        private static void PatchLang()
        {
            var prefixGetItemNameValue = typeof(ContentHookDispatcher).GetMethod(
                nameof(Lang_GetItemNameValue_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var getItemNameValue = typeof(Lang).GetMethod(
                nameof(Lang.GetItemNameValue),
                new[] { typeof(int) });
            if (getItemNameValue != null)
            {
                _harmony.Patch(getItemNameValue, prefix: new HarmonyMethod(prefixGetItemNameValue));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Lang.GetItemNameValue(int)");
            }

            var prefixGetItemName = typeof(ContentHookDispatcher).GetMethod(
                nameof(Lang_GetItemName_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var getItemName = typeof(Lang).GetMethod(
                nameof(Lang.GetItemName),
                new[] { typeof(int) });
            if (getItemName != null)
            {
                _harmony.Patch(getItemName, prefix: new HarmonyMethod(prefixGetItemName));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Lang.GetItemName(int)");
            }

            var prefixGetPrefixed = typeof(ContentHookDispatcher).GetMethod(
                nameof(Lang_GetPrefixedItemName_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var getPrefixedItemName = typeof(Lang).GetMethod(
                nameof(Lang.GetPrefixedItemName),
                new[] { typeof(int), typeof(int) });
            if (getPrefixedItemName != null)
            {
                _harmony.Patch(getPrefixedItemName, prefix: new HarmonyMethod(prefixGetPrefixed));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 Lang.GetPrefixedItemName(int, int)");
            }

            var prefixGetTooltip = typeof(ContentHookDispatcher).GetMethod(
                nameof(Lang_GetTooltip_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var getTooltip = typeof(Lang).GetMethod(
                nameof(Lang.GetTooltip),
                new[] { typeof(int) });
            if (getTooltip != null)
            {
                _harmony.Patch(getTooltip, prefix: new HarmonyMethod(prefixGetTooltip));
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
                    __result = new ItemTooltip(tip.Split('\n'));
                }
                return false;
            }
            return true;
        }

        private static void PatchPopupText()
        {
            var prefixNewText = typeof(ContentHookDispatcher).GetMethod(
                nameof(PopupText_NewText_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var newTextMethod = typeof(PopupText).GetMethod(
                nameof(PopupText.NewText),
                new[] { typeof(PopupTextContext), typeof(Item), typeof(Vector2), typeof(int), typeof(bool), typeof(bool) });
            if (newTextMethod != null)
            {
                _harmony.Patch(newTextMethod, prefix: new HarmonyMethod(prefixNewText));
                ModLoader.Log("[ContentHookDispatcher] 已挂钩 PopupText.NewText(PopupTextContext, Item, Vector2, int, bool, bool)");
            }

            var prefixUpdate = typeof(ContentHookDispatcher).GetMethod(
                nameof(PopupText_Update_Prefix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var updateMethod = typeof(PopupText).GetMethod(
                nameof(PopupText.Update),
                new[] { typeof(int) });
            if (updateMethod != null)
            {
                _harmony.Patch(updateMethod, prefix: new HarmonyMethod(prefixUpdate));
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