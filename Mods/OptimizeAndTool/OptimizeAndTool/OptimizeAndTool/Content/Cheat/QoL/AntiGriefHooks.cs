using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 防非玩家爆炸物破坏地形门控（拦截小丑炸弹、机械骷髅王炸弹、非玩家敌怪爆炸物，基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    internal static class AntiGriefHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Projectile.ExplodeTiles += Hook_ExplodeTiles;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Projectile.ExplodeTiles -= Hook_ExplodeTiles;
            _registered = false;
        }

        private static void Hook_ExplodeTiles(On_Projectile.orig_ExplodeTiles orig, Projectile self, Vector2 compareSpot, int radius, int minI, int maxI, int minJ, int maxJ, bool wallSplode, bool explodeHardmodeOres)
        {
            if (QoLValSet.antiGriefExplosions.val)
            {
                // 1. 小丑的快乐炸弹
                if (self.type == ProjectileID.HappyBomb)
                {
                    return;
                }

                // 2. 机械骷髅王炸弹
                if (self.type == ProjectileID.BombSkeletronPrime)
                {
                    return;
                }

                // 3. 敌怪弹幕 / NPC 生成的投掷物 / 非玩家弹幕
                if (self.hostile || self.npcProj || self.owner == 255)
                {
                    return;
                }

                if (self.owner >= 0 && self.owner < Main.player.Length)
                {
                    Player ownerPlayer = Main.player[self.owner];
                    if (ownerPlayer == null || !ownerPlayer.active)
                    {
                        return;
                    }
                }
            }

            orig(self, compareSpot, radius, minI, maxI, minJ, maxJ, wallSplode, explodeHardmodeOres);
        }
    }
}
