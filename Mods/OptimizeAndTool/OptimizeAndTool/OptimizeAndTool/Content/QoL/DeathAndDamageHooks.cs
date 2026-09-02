using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using TPML;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 死亡与伤害规则优化门控（基于 HookGen 强类型 On_ 门控）：
    /// 1. 禁止生成墓碑（拦截 Player.DropTombstone，钱币掉落不受影响）；
    /// 2. 禁用伤害波动机制（拦截 Main.DamageVar，伤害固定为原值）；
    /// 3. 满血复活（原版普通死亡复活仅半血 max(100, maxHP/2)，Player.Spawn 的 spawnMax 满血分支为死代码）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class DeathAndDamageHooks
    {
        // 1. 禁止墓碑
        public static GetSetReset<bool> BanTombstone = new GetSetReset<bool>(false, false);
        // 2. 禁用伤害波动
        public static GetSetReset<bool> DisableDamageVar = new GetSetReset<bool>(false, false);
        // 3. 满血复活
        public static GetSetReset<bool> RespawnWithFullHP = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.Spawn += Hook_Spawn;
            On_Main.DamageVar += Hook_DamageVar;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.Spawn -= Hook_Spawn;
            On_Main.DamageVar -= Hook_DamageVar;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("banTombstone", BanTombstone),
                CommandBuild.get2("disableDamageVar", DisableDamageVar),
                CommandBuild.get2("respawnWithFullHP", RespawnWithFullHP)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(BanTombstone, "玩家死亡时不再生成墓碑（钱币仍正常掉落）", "Images/Item_3230", "禁止生成墓碑"),
                UIBuild.get2(DisableDamageVar, "消除原版伤害 ±15% 随机浮动，所有伤害固定为面板原值", "Images/Item_6", "禁用伤害波动"),
                UIBuild.get2(RespawnWithFullHP, "死亡复活时以满血满蓝重生（原版为半血 max(100, 上限/2)）", "Images/Item_29", "满血复活")
            };
        }

        private static void Hook_Spawn(On_Player.orig_Spawn orig, Player self, PlayerSpawnContext context)
        {
            if (RespawnWithFullHP.val)
            {
                self.spawnMax = true;
            }

            orig(self, context);
        }

        private static int Hook_DamageVar(On_Main.orig_DamageVar orig, float dmg, float luck)
        {
            if (DisableDamageVar.val)
            {
                return (int)dmg;
            }

            return orig(dmg, luck);
        }
    }

    /// <summary>
    /// 墓碑拦截：CanDropTombstone 返回 false 即跳过 Player.DropTombstone（仅影响墓碑，不影响掉钱）
    /// </summary>
    internal class Patch_NoTombstone : TPML.Content.ModPlayer
    {
        public override bool CanDropTombstone(Player This, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            return !DeathAndDamageHooks.BanTombstone.val;
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class DeathAndDamage
    {
        public static GetSetReset<bool> BanTombstone => DeathAndDamageHooks.BanTombstone;
        public static GetSetReset<bool> DisableDamageVar => DeathAndDamageHooks.DisableDamageVar;
        public static GetSetReset<bool> RespawnWithFullHP => DeathAndDamageHooks.RespawnWithFullHP;

        public static List<CommandObject> GetCO() => DeathAndDamageHooks.GetCO();
        public static List<UIElement> GetUI() => DeathAndDamageHooks.GetUI();
    }
}
