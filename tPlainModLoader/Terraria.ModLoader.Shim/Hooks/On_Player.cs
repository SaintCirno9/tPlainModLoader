using System;
using System.Reflection;
using HarmonyLib;

namespace Terraria
{
    /// <summary>
    /// tModLoader / TerrariaHooks 运行时委托与钩子存根
    /// </summary>
    public static class On_Player
    {
        private static Harmony _harmony = null;
        private static bool _healPatched = false;
        private static bool _manaPatched = false;

        private static void EnsureHarmony()
        {
            if (_harmony == null)
            {
                _harmony = new Harmony("Terraria.ModLoader.Shim.On_Player");
            }
        }

        #region QuickHeal_GetItemToUse

        public delegate Item orig_QuickHeal_GetItemToUse(Player self);
        public delegate Item hook_QuickHeal_GetItemToUse(orig_QuickHeal_GetItemToUse orig, Player self);

        private static hook_QuickHeal_GetItemToUse _quickHealHook;

        public static event hook_QuickHeal_GetItemToUse QuickHeal_GetItemToUse
        {
            add
            {
                _quickHealHook += value;
                PatchQuickHeal();
            }
            remove
            {
                _quickHealHook -= value;
            }
        }

        private static void PatchQuickHeal()
        {
            if (_healPatched) return;
            EnsureHarmony();
            var target = typeof(Player).GetMethod(nameof(Player.QuickHeal_GetItemToUse), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (target != null)
            {
                var prefix = typeof(On_Player).GetMethod(nameof(QuickHeal_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                _healPatched = true;
            }
        }

        private static bool QuickHeal_Prefix(Player __instance, ref Item __result)
        {
            if (_quickHealHook != null)
            {
                Item res = _quickHealHook.Invoke(self => null, __instance);
                if (res != null)
                {
                    __result = res;
                    return false; // 跳过原版逻辑
                }
            }
            return true;
        }

        #endregion

        #region QuickMana_GetItemToUse

        public delegate Item orig_QuickMana_GetItemToUse(Player self);
        public delegate Item hook_QuickMana_GetItemToUse(orig_QuickMana_GetItemToUse orig, Player self);

        private static hook_QuickMana_GetItemToUse _quickManaHook;

        public static event hook_QuickMana_GetItemToUse QuickMana_GetItemToUse
        {
            add
            {
                _quickManaHook += value;
                PatchQuickMana();
            }
            remove
            {
                _quickManaHook -= value;
            }
        }

        private static void PatchQuickMana()
        {
            if (_manaPatched) return;
            EnsureHarmony();
            var target = typeof(Player).GetMethod(nameof(Player.QuickMana_GetItemToUse), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (target != null)
            {
                var prefix = typeof(On_Player).GetMethod(nameof(QuickMana_Prefix), BindingFlags.Static | BindingFlags.NonPublic);
                _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                _manaPatched = true;
            }
        }

        private static bool QuickMana_Prefix(Player __instance, ref Item __result)
        {
            if (_quickManaHook != null)
            {
                Item res = _quickManaHook.Invoke(self => null, __instance);
                if (res != null)
                {
                    __result = res;
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
