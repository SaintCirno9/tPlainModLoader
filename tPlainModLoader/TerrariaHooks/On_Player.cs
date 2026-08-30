using System;
using System.Reflection;
using MonoMod.RuntimeDetour.HookGen;
using Terraria;

/// <summary>
/// MonoMod HookGen On_Player 门面（对齐 tML 标准 TerrariaHooks 签名）
/// </summary>
public static class On_Player
{
    public delegate Item orig_QuickHeal_GetItemToUse(Player self);
    public delegate Item hook_QuickHeal_GetItemToUse(orig_QuickHeal_GetItemToUse orig, Player self);

    public delegate Item orig_QuickMana_GetItemToUse(Player self);
    public delegate Item hook_QuickMana_GetItemToUse(orig_QuickMana_GetItemToUse orig, Player self);

    private static readonly MethodBase _target_QuickHeal_GetItemToUse =
        typeof(Player).GetMethod(nameof(Player.QuickHeal_GetItemToUse), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly MethodBase _target_QuickMana_GetItemToUse =
        typeof(Player).GetMethod(nameof(Player.QuickMana_GetItemToUse), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    public static event hook_QuickHeal_GetItemToUse QuickHeal_GetItemToUse
    {
        add
        {
            if (_target_QuickHeal_GetItemToUse != null)
                HookEndpointManager.Add<hook_QuickHeal_GetItemToUse>(_target_QuickHeal_GetItemToUse, value);
        }
        remove
        {
            if (_target_QuickHeal_GetItemToUse != null)
                HookEndpointManager.Remove<hook_QuickHeal_GetItemToUse>(_target_QuickHeal_GetItemToUse, value);
        }
    }

    public static event hook_QuickMana_GetItemToUse QuickMana_GetItemToUse
    {
        add
        {
            if (_target_QuickMana_GetItemToUse != null)
                HookEndpointManager.Add<hook_QuickMana_GetItemToUse>(_target_QuickMana_GetItemToUse, value);
        }
        remove
        {
            if (_target_QuickMana_GetItemToUse != null)
                HookEndpointManager.Remove<hook_QuickMana_GetItemToUse>(_target_QuickMana_GetItemToUse, value);
        }
    }
}
