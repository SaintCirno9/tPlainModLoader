using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.Localization;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 死亡与伤害规则优化：
    /// 1. 禁止生成墓碑（拦截 Player.DropTombstone，钱币掉落不受影响）；
    /// 2. 禁用伤害波动机制（拦截 Main.DamageVar，伤害固定为原值）；
    /// 3. 满血复活（原版普通死亡复活仅半血 max(100, maxHP/2)，Player.Spawn 的 spawnMax 满血分支为死代码）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class DeathAndDamage
    {
        // 1. 禁止墓碑
        public static GetSetReset<bool> BanTombstone = new GetSetReset<bool>(false, false);
        // 2. 禁用伤害波动
        public static GetSetReset<bool> DisableDamageVar = new GetSetReset<bool>(false, false);
        // 3. 满血复活
        public static GetSetReset<bool> RespawnWithFullHP = new GetSetReset<bool>(false, false);

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
    }

    /// <summary>
    /// 墓碑拦截：CanDropTombstone 返回 false 即跳过 Player.DropTombstone（仅影响墓碑，不影响掉钱）
    /// </summary>
    internal class Patch_NoTombstone : PatchPlayer
    {
        public override bool CanDropTombstone(Player This, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            return !DeathAndDamage.BanTombstone.val;
        }
    }

    /// <summary>
    /// 满血复活：Player.Spawn 内 statLife&lt;=0 时若 spawnMax 为 true 则满血满蓝（Player.cs:37889），
    /// 而 spawnMax 全代码库仅此处消费且从未被赋 true。Prefix 置位后原版分支即生效，无副作用。
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Spawn))]
    internal static class Patch_FullHpRespawn
    {
        [HarmonyPrefix]
        internal static void Prefix(Player __instance)
        {
            if (!DeathAndDamage.RespawnWithFullHP.val) return;
            __instance.spawnMax = true;
        }
    }

    /// <summary>
    /// 伤害波动拦截：Main.DamageVar(dmg, luck) 是全局伤害浮动唯一入口（含玩家与 NPC 双方），Prefix 直接返回固定值
    /// </summary>
    [HarmonyPatch(typeof(Main), nameof(Main.DamageVar))]
    internal static class Patch_DisableDamageVar
    {
        [HarmonyPrefix]
        public static bool Prefix(float dmg, float luck, ref int __result)
        {
            if (!DeathAndDamage.DisableDamageVar.val) return true;
            __result = (int)dmg;
            return false;
        }
    }
}
