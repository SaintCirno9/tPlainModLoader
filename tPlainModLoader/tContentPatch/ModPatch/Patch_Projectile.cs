using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Projectile 生命周期补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_Projectile : ListCopy<PatchProjectile>
    {
        private static List<PatchProjectile> mod = new List<PatchProjectile>();

        public Patch_Projectile() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var projectile = typeof(Projectile);

            // Projectile.Update(int)
            HookRegistry.Add(GetInstance(projectile, "Update", typeof(int)),
                (Action<Action<Projectile, int>, Projectile, int>)((orig, self, i) =>
                {
                    UpdatePrefix(self, i);
                    orig(self, i);
                    UpdatePostfix(self, i);
                }));

            // Projectile.Kill()
            HookRegistry.Add(GetInstance(projectile, "Kill"),
                (Action<Action<Projectile>, Projectile>)((orig, self) =>
                {
                    KillPrefix(self);
                    orig(self);
                    KillPostfix(self);
                }));

            // Projectile.SetDefaults(int)
            HookRegistry.Add(GetInstance(projectile, "SetDefaults", typeof(int)),
                (Action<Action<Projectile, int>, Projectile, int>)((orig, self, Type) =>
                {
                    SetDefaultsPrefix(self, Type);
                    orig(self, Type);
                    SetDefaultsPostfix(self, Type);
                }));

            // Projectile.NewProjectile(IEntitySource, float, float, float, float, int, int, float, int, float, float, float, NewProjectileModifier)（静态，返回 int）
            // 注意：NewProjectileModifier 位于 Terraria 命名空间（非 DataStructures）
            HookRegistry.Add(GetStatic(projectile, "NewProjectile",
                    typeof(IEntitySource), typeof(float), typeof(float), typeof(float), typeof(float),
                    typeof(int), typeof(int), typeof(float), typeof(int),
                    typeof(float), typeof(float), typeof(float), typeof(Terraria.NewProjectileModifier)),
                (Func<Func<IEntitySource, float, float, float, float, int, int, float, int, float, float, float, Terraria.NewProjectileModifier, int>,
                    IEntitySource, float, float, float, float, int, int, float, int, float, float, float, Terraria.NewProjectileModifier, int>)((orig, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer) =>
                {
                    int result = orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer);
                    NewProjectilePostfix(result, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer);
                    return result;
                }));

            // Projectile.AI_203_GetLightningColor()（实例，返回 Color，postfix 修改 __result）
            HookRegistry.Add(GetInstance(projectile, "AI_203_GetLightningColor"),
                (Func<Func<Projectile, Color>, Projectile, Color>)((orig, self) =>
                {
                    Color result = orig(self);
                    AI_203_GetLightningColor(self, ref result);
                    return result;
                }));
        }

        private static MethodInfo GetInstance(Type type, string name, params Type[] types)
        {
            return MethodLookup.Instance(type, name, types);
        }

        private static MethodInfo GetStatic(Type type, string name, params Type[] types)
        {
            return MethodLookup.Static(type, name, types);
        }

        public static void UpdatePrefix(Projectile __instance, int i)
        {
            mod.ForTry(item => item.UpdatePrefix(__instance, i));
        }

        public static void UpdatePostfix(Projectile __instance, int i)
        {
            mod.ForTry(item => item.UpdatePostfix(__instance, i));
        }

        public static void KillPrefix(Projectile __instance)
        {
            mod.ForTry(item => item.KillPrefix(__instance));
        }

        public static void KillPostfix(Projectile __instance)
        {
            mod.ForTry(item => item.KillPostfix(__instance));
        }

        public static void SetDefaultsPrefix(Projectile __instance, int Type)
        {
            mod.ForTry(item => item.SetDefaultsPrefix(__instance, Type));
        }

        public static void SetDefaultsPostfix(Projectile __instance, int Type)
        {
            mod.ForTry(item => item.SetDefaultsPostfix(__instance, Type));
        }

        public static void NewProjectilePostfix(int __result,
            IEntitySource spawnSource,
            float X, float Y, float SpeedX, float SpeedY,
            int Type, int Damage, float KnockBack, int Owner,
            float ai0, float ai1, float ai2,
            NewProjectileModifier modifer)
        {
            mod.ForTry(item => item.NewProjectilePostfix(
                __result, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer));
        }

        public static void AI_203_GetLightningColor(Projectile __instance, ref Color __result)
        {
            Color color = __result;

            mod.ForTry(item =>
            {
                color = item.AI_203_GetLightningColor(__instance, color);
            });

            __result = color;
        }
    }
}
