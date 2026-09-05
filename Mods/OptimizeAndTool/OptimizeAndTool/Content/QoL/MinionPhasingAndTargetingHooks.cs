using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TPML;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 召唤物与哨兵穿墙、智能透视索敌、发射物穿墙与防拉回脱战机制
    /// 作者: SaintCirno9
    /// </summary>
    public static class MinionPhasingAndTargetingHooks
    {
        private static bool _registered = false;

        // 线程局部上下文：标记当前线程正在执行召唤物/哨兵的 AI 索敌
        [ThreadStatic]
        private static Projectile _currentSummonProjectile;

        // 标记投射物是否是由仆从/哨兵发射的攻击子弹（如小鬼火球、黄蜂毒针、哨兵弹幕等）
        private static readonly bool[] _isMinionShot = new bool[Main.maxProjectiles + 1];

        // 走地仆从正在进行相位突进状态
        private static readonly bool[] _isPhasingDash = new bool[Main.maxProjectiles + 1];

        // 走地仆从已知集合
        private static readonly HashSet<int> WalkingMinionTypes = new HashSet<int>
        {
            ProjectileID.BabySlime,
            ProjectileID.VenomSpider,
            ProjectileID.JumperSpider,
            ProjectileID.DangerousSpider,
            ProjectileID.OneEyedPirate,
            ProjectileID.SoulscourgePirate,
            ProjectileID.PirateCaptain,
            ProjectileID.Pygmy,
            ProjectileID.Pygmy2,
            ProjectileID.Pygmy3,
            ProjectileID.Pygmy4,
            ProjectileID.StormTigerTier1,
            ProjectileID.StormTigerTier2,
            ProjectileID.StormTigerTier3
        };

        public static void RegisterAll()
        {
            if (_registered) return;

            On_Projectile.Update += Hook_Update;
            On_Projectile.AI += Hook_AI;
            On_Projectile.Kill += Hook_Kill;
            On_Projectile.CanHitWithOwnBody += Hook_CanHitWithOwnBody;
            On_Projectile.Minion_FindTargetInRange += Hook_Minion_FindTargetInRange;
            On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier += Hook_NewProjectile;

            On_Collision.CanHitLine += Hook_CanHitLine;
            On_Collision.CanHit_Vector2_int_int_Vector2_int_int += Hook_CanHit_Vector2;
            On_Collision.CanHit_Entity_Entity += Hook_CanHit_Entity;

            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;

            On_Projectile.Update -= Hook_Update;
            On_Projectile.AI -= Hook_AI;
            On_Projectile.Kill -= Hook_Kill;
            On_Projectile.CanHitWithOwnBody -= Hook_CanHitWithOwnBody;
            On_Projectile.Minion_FindTargetInRange -= Hook_Minion_FindTargetInRange;
            On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier -= Hook_NewProjectile;

            On_Collision.CanHitLine -= Hook_CanHitLine;
            On_Collision.CanHit_Vector2_int_int_Vector2_int_int -= Hook_CanHit_Vector2;
            On_Collision.CanHit_Entity_Entity -= Hook_CanHit_Entity;

            Array.Clear(_isMinionShot, 0, _isMinionShot.Length);
            Array.Clear(_isPhasingDash, 0, _isPhasingDash.Length);

            _registered = false;
        }

        public static bool IsMinionOrSentry(Projectile proj)
        {
            if (proj == null || !proj.active) return false;
            if (proj.minion || proj.sentry) return true;
            if (ProjectileID.Sets.MinionSacrificable[proj.type] || ProjectileID.Sets.MinionTargetingFeature[proj.type]) return true;
            if (proj.minionSlots > 0f) return true;
            return false;
        }

        public static bool IsWalkingMinion(Projectile proj)
        {
            if (proj == null) return false;
            if (WalkingMinionTypes.Contains(proj.type)) return true;
            if (proj.aiStyle == 26)
            {
                switch (proj.type)
                {
                    case ProjectileID.FlyingImp:
                    case ProjectileID.Hornet:
                    case ProjectileID.Retanimini:
                    case ProjectileID.Spazmamini:
                    case ProjectileID.DeadlySphere:
                    case ProjectileID.UFOMinion:
                    case ProjectileID.Tempest:
                        return false;
                    default:
                        return true;
                }
            }
            return false;
        }

        #region Collision & LoS Hooks (透视索敌门控)

        private static bool Hook_CanHitLine(On_Collision.orig_CanHitLine orig, Vector2 Position1, int Width1, int Height1, Vector2 Position2, int Width2, int Height2)
        {
            if (_currentSummonProjectile != null && QoLValSet.minionPhasing.val)
            {
                return true;
            }
            return orig(Position1, Width1, Height1, Position2, Width2, Height2);
        }

        private static bool Hook_CanHit_Vector2(On_Collision.orig_CanHit_Vector2_int_int_Vector2_int_int orig, Vector2 Position1, int Width1, int Height1, Vector2 Position2, int Width2, int Height2)
        {
            if (_currentSummonProjectile != null && QoLValSet.minionPhasing.val)
            {
                return true;
            }
            return orig(Position1, Width1, Height1, Position2, Width2, Height2);
        }

        private static bool Hook_CanHit_Entity(On_Collision.orig_CanHit_Entity_Entity orig, Entity source, Entity target)
        {
            if (_currentSummonProjectile != null && QoLValSet.minionPhasing.val)
            {
                return true;
            }
            return orig(source, target);
        }

        private static bool Hook_CanHitWithOwnBody(On_Projectile.orig_CanHitWithOwnBody orig, Projectile self, Entity ent)
        {
            if (IsMinionOrSentry(self) && QoLValSet.minionPhasing.val)
            {
                float maxDist = QoLValSet.minionRangeBoost.val ? 1800f : self.ownerHitCheckDistance;
                if (self.Distance(ent.Center) <= maxDist)
                {
                    return true;
                }
                return false;
            }
            return orig(self, ent);
        }

        private static void Hook_Minion_FindTargetInRange(
            On_Projectile.orig_Minion_FindTargetInRange orig,
            Projectile self,
            int startAttackRange,
            ref int attackTarget,
            bool skipIfCannotHitWithOwnBody,
            Func<Entity, int, bool> customEliminationCheck,
            bool respectOwnerTarget)
        {
            if (QoLValSet.minionRangeBoost.val && startAttackRange < 1800)
            {
                startAttackRange = 1800;
            }
            if (QoLValSet.minionPhasing.val)
            {
                skipIfCannotHitWithOwnBody = false;
            }
            orig(self, startAttackRange, ref attackTarget, skipIfCannotHitWithOwnBody, customEliminationCheck, respectOwnerTarget);
        }

        #endregion

        #region Projectile Lifecycle & Phasing Hooks (移动穿墙与弹幕穿墙)

        private static int Hook_NewProjectile(
            On_Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier orig,
            IEntitySource spawnSource, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1, float ai2, NewProjectileModifier modifer)
        {
            int result = orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer);
            if (result >= 0 && result < Main.maxProjectiles)
            {
                // 若当前正在仆从/哨兵的 AI 期间发射子弹，标记该投射物为仆从衍生弹幕
                if (_currentSummonProjectile != null && QoLValSet.minionPhasing.val)
                {
                    _isMinionShot[result] = true;
                    Projectile proj = Main.projectile[result];
                    if (proj != null && proj.active)
                    {
                        proj.tileCollide = false;
                    }
                }
            }
            return result;
        }

        private static void Hook_Kill(On_Projectile.orig_Kill orig, Projectile self)
        {
            if (self.whoAmI >= 0 && self.whoAmI < Main.maxProjectiles)
            {
                _isMinionShot[self.whoAmI] = false;
                _isPhasingDash[self.whoAmI] = false;
            }
            orig(self);
        }

        private static void Hook_AI(On_Projectile.orig_AI orig, Projectile self)
        {
            bool isSummon = IsMinionOrSentry(self);
            if (isSummon && QoLValSet.minionPhasing.val)
            {
                _currentSummonProjectile = self;
                try
                {
                    orig(self);
                }
                finally
                {
                    _currentSummonProjectile = null;
                }
            }
            else
            {
                orig(self);
            }
        }

        private static void Hook_Update(On_Projectile.orig_Update orig, Projectile self, int i)
        {
            if (self != null && self.active)
            {
                // 1. 仆从发射的子弹持续保持穿墙
                if (_isMinionShot[self.whoAmI])
                {
                    if (QoLValSet.minionPhasing.val)
                    {
                        self.tileCollide = false;
                    }
                }
                // 2. 仆从本体穿墙管理
                else if (IsMinionOrSentry(self))
                {
                    if (QoLValSet.minionPhasing.val)
                    {
                        if (IsWalkingMinion(self))
                        {
                            HandleWalkingMinionPhasing(self);
                        }
                        else
                        {
                            // 飞行类仆从与哨兵：完全穿墙移动
                            self.tileCollide = false;
                        }
                    }
                    else
                    {
                        _isPhasingDash[self.whoAmI] = false;
                    }
                }
            }

            orig(self, i);
        }

        private static void HandleWalkingMinionPhasing(Projectile self)
        {
            // 获取可能锁定的目标
            NPC target = self.OwnerMinionAttackTargetNPC;
            if (target == null || !target.CanBeChasedBy(self))
            {
                // 扫描视野范围内的最近有效敌怪
                float closestDist = 1800f;
                int found = -1;
                for (int n = 0; n < Main.maxNPCs; n++)
                {
                    NPC npc = Main.npc[n];
                    if (npc != null && npc.active && npc.CanBeChasedBy(self))
                    {
                        float d = Vector2.Distance(self.Center, npc.Center);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            found = n;
                        }
                    }
                }
                if (found >= 0)
                {
                    target = Main.npc[found];
                }
            }

            bool insideSolid = Collision.SolidCollision(self.position, self.width, self.height);

            // 当有追击目标时进行受阻判断
            if (target != null && target.active && target.CanBeChasedBy(self))
            {
                // 未 Hook 的真实视线检测判断是否隔墙
                bool hasLineOfSight = Collision.CanHitLine(self.position, self.width, self.height, target.position, target.width, target.height);

                if (!hasLineOfSight || insideSolid)
                {
                    // 激活相位穿墙突进模式
                    _isPhasingDash[self.whoAmI] = true;
                    self.tileCollide = false;

                    // 在物块中穿梭时赋予冲刺速度并产生微弱幻影粒子
                    if (insideSolid)
                    {
                        Vector2 dir = (target.Center - self.Center).SafeNormalize(Vector2.UnitY);
                        float currentSpd = self.velocity.Length();
                        float targetSpd = Math.Max(currentSpd, 10f);
                        self.velocity = Vector2.Lerp(self.velocity, dir * targetSpd, 0.2f);

                        if (Main.rand.Next(4) == 0)
                        {
                            Dust d = Dust.NewDustDirect(self.position, self.width, self.height, DustID.PinkCrystalShard, 0f, 0f, 150, default, 1f);
                            d.noGravity = true;
                            d.velocity *= 0.2f;
                        }
                    }
                    return;
                }
            }

            // 离开物块且视线畅通时无缝恢复地面碰撞物理
            if (!insideSolid)
            {
                _isPhasingDash[self.whoAmI] = false;
                self.tileCollide = true;
            }
        }

        #endregion
    }
}
