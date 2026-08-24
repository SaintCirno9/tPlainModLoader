using HarmonyLib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 防非玩家爆炸物破坏地形（拦截小丑炸弹、机械骷髅王炸弹、非玩家敌怪爆炸物）
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Projectile))]
    internal static class Patch_AntiGrief
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Projectile.ExplodeTiles))]
        public static bool ExplodeTiles_Prefix(Projectile __instance, Vector2 compareSpot, int radius, int minI, int maxI, int minJ, int maxJ, bool wallSplode)
        {
            if (!QoLValSet.antiGriefExplosions.val) return true;

            // 1. 小丑的快乐炸弹
            if (__instance.type == ProjectileID.HappyBomb)
            {
                return false;
            }

            // 2. 机械骷髅王炸弹
            if (__instance.type == ProjectileID.BombSkeletronPrime)
            {
                return false;
            }

            // 3. 敌怪弹幕 / NPC 生成的投掷物 / 非玩家弹幕
            if (__instance.hostile || __instance.npcProj || __instance.owner == 255)
            {
                return false;
            }

            if (__instance.owner >= 0 && __instance.owner < Main.player.Length)
            {
                Player ownerPlayer = Main.player[__instance.owner];
                if (ownerPlayer == null || !ownerPlayer.active)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
