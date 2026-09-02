using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI;
using TPML.Core.Logging;

namespace TPML.Content.Engine
{
    /// <summary>
    /// TPML 原生内容引擎核心 HookGen 强类型门面调度器
    /// 作者: SaintCirno9
    /// </summary>
    public static class ContentHookDispatcher
    {
        private static bool _initialized = false;
        private static bool _patchesApplied = false;

        private static readonly object _syncLock = new object();
        private static readonly List<ModPlayer> _modPlayers = new List<ModPlayer>();
        private static readonly List<ModSystem> _modSystems = new List<ModSystem>();
        private static readonly List<GlobalItem> _globalItems = new List<GlobalItem>();
        private static readonly List<GlobalNPC> _globalNPCs = new List<GlobalNPC>();

        public static ModPlayer[] ActiveModPlayers { get; private set; } = Array.Empty<ModPlayer>();
        public static ModSystem[] ActiveModSystems { get; private set; } = Array.Empty<ModSystem>();
        public static GlobalItem[] ActiveGlobalItems { get; private set; } = Array.Empty<GlobalItem>();
        public static GlobalNPC[] ActiveGlobalNPCs { get; private set; } = Array.Empty<GlobalNPC>();

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

            lock (_syncLock)
            {
                foreach (var item in contents)
                {
                    if (item is ModPlayer player && !_modPlayers.Contains(player))
                    {
                        _modPlayers.Add(player);
                    }
                    else if (item is ModSystem system && !_modSystems.Contains(system))
                    {
                        _modSystems.Add(system);
                    }
                    else if (item is GlobalItem gItem && !_globalItems.Contains(gItem))
                    {
                        _globalItems.Add(gItem);
                    }
                    else if (item is GlobalNPC gNpc && !_globalNPCs.Contains(gNpc))
                    {
                        _globalNPCs.Add(gNpc);
                    }
                }

                CommitSnapshotsInternal();
            }

            if (!_patchesApplied)
            {
                ApplyOnDemandPatches();
                _patchesApplied = true;
            }
        }

        public static void CommitSnapshots()
        {
            lock (_syncLock)
            {
                CommitSnapshotsInternal();
            }
        }

        private static void CommitSnapshotsInternal()
        {
            ActiveModPlayers = _modPlayers.ToArray();
            ActiveModSystems = _modSystems.ToArray();
            ActiveGlobalItems = _globalItems.ToArray();
            ActiveGlobalNPCs = _globalNPCs.ToArray();
        }

        public static void Clear()
        {
            lock (_syncLock)
            {
                _modPlayers.Clear();
                _modSystems.Clear();
                _globalItems.Clear();
                _globalNPCs.Clear();
                CommitSnapshotsInternal();
            }
            TPML.Content.Fusion.InventoryFusionManager.Clear();
            TPML.Content.Fusion.UnifiedInventoryFusionHooks.UnregisterAll();
            HookRegistry.Clear(HookScope.Content);
            _initialized = false;
            _patchesApplied = false;
        }

        private static void ApplyOnDemandPatches()
        {
            // 1. 输入与按键拦截
            On_PlayerInput.UpdateInput += Hook_PlayerInput_UpdateInput;

            // 2. 语言与文本拦截
            On_Lang.GetItemNameValue += Hook_Lang_GetItemNameValue;
            On_Lang.GetItemName += Hook_Lang_GetItemName;
            On_Lang.GetPrefixedItemName += Hook_Lang_GetPrefixedItemName;
            On_Lang.GetTooltip += Hook_Lang_GetTooltip;

            // 3. 浮动文字安全拦截
            On_PopupText.NewText_PopupTextContext_Item_Vector2_int_bool_bool += Hook_PopupText_NewText;
            On_PopupText.Update += Hook_PopupText_Update;

            // 4. 物品克隆
            On_Item.Clone += Hook_Item_Clone;

            // 5. 框架级全量背包融合系统 Hook 门控矩阵 (通用外部容器/魔杖/油漆/HasItem/ConsumeItem)
            TPML.Content.Fusion.UnifiedInventoryFusionHooks.RegisterAll();

            _patchesApplied = true;
        }

        #region Hook Handlers

        private static void Hook_PlayerInput_UpdateInput(On_PlayerInput.orig_UpdateInput orig)
        {
            orig();
            KeybindLoader.Update();
            TriggersSet triggers = PlayerInput.Triggers.Current;
            for (int idx = 0; idx < ActiveModPlayers.Length; idx++)
            {
                ActiveModPlayers[idx].ProcessTriggers(triggers);
            }
        }

        private static Item Hook_Item_Clone(On_Item.orig_Clone orig, Item self)
        {
            Item result = orig(self);
            if (self != null && result != null)
            {
                ItemLoader.OnItemCloned(self, result);
            }
            return result;
        }

        private static string Hook_Lang_GetItemNameValue(On_Lang.orig_GetItemNameValue orig, int id)
        {
            if (id >= ItemLoader.ModItemOffset)
            {
                string name = ItemLoader.GetDisplayName(id);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            return orig(id);
        }

        private static LocalizedText Hook_Lang_GetItemName(On_Lang.orig_GetItemName orig, int id)
        {
            if (id >= ItemLoader.ModItemOffset)
            {
                string name = ItemLoader.GetDisplayName(id);
                if (!string.IsNullOrEmpty(name))
                {
                    return new LocalizedText($"ItemName.{id}", name);
                }
            }
            return orig(id);
        }

        private static string Hook_Lang_GetPrefixedItemName(On_Lang.orig_GetPrefixedItemName orig, int id, int prefixType)
        {
            if (id >= ItemLoader.ModItemOffset)
            {
                string baseName = ItemLoader.GetDisplayName(id);
                if (prefixType > 0 && prefixType < Lang.prefix.Length)
                {
                    string pName = Lang.prefix[prefixType]?.Value ?? string.Empty;
                    return string.IsNullOrEmpty(pName) ? baseName : $"{pName} {baseName}";
                }
                return baseName;
            }
            return orig(id, prefixType);
        }

        private static ItemTooltip Hook_Lang_GetTooltip(On_Lang.orig_GetTooltip orig, int itemId)
        {
            if (itemId >= ItemLoader.ModItemOffset)
            {
                string tip = ItemLoader.GetTooltip(itemId);
                if (string.IsNullOrEmpty(tip))
                {
                    return ItemTooltip.None;
                }
                string[] lines = tip.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                return ItemTooltip.FromHardcodedText(lines);
            }
            return orig(itemId);
        }

        private static int Hook_PopupText_NewText(On_PopupText.orig_NewText_PopupTextContext_Item_Vector2_int_bool_bool orig, PopupTextContext context, Item newItem, Vector2 position, int stack, bool noStack, bool longText)
        {
            if (newItem == null) return 0;
            if (newItem.type >= ItemLoader.ModItemOffset)
            {
                if (string.IsNullOrEmpty(newItem.Name))
                {
                    string name = ItemLoader.GetDisplayName(newItem.type);
                    newItem.SetNameOverride(name);
                }
            }
            return orig(context, newItem, position, stack, noStack, longText);
        }

        private static void Hook_PopupText_Update(On_PopupText.orig_Update orig, PopupText self, int whoAmI)
        {
            if (whoAmI >= 0 && whoAmI < PopupText.popupText.Length)
            {
                var text = PopupText.popupText[whoAmI];
                if (text != null && text.active && text.displayText == null)
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
                        return;
                    }
                }
            }
            orig(self, whoAmI);
        }

        #endregion
    }
}
