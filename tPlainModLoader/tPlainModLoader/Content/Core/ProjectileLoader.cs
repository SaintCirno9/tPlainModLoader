using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using tContentPatch.ModPatch;
using TPML.Content.Assets;
using TPML.Content.Core;
using TPML.Content.Engine;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义弹幕 (ModProjectile) 注册、贴图加载、生命周期与分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class ProjectileLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("ProjectileLoader");

        public const int ModProjectileOffset = 1100;
        internal static readonly ContentRegistry<ModProjectile> Registry = new ContentRegistry<ModProjectile>(ModProjectileOffset);
        private static readonly ConditionalWeakTable<Projectile, ModProjectile> _modProjInstances = new ConditionalWeakTable<Projectile, ModProjectile>();

        public static int ProjectileCount => Registry.NextId;
        public static int NextProjID => Registry.NextId;
        public static IReadOnlyCollection<ModProjectile> Projectiles => Registry.Values as IReadOnlyCollection<ModProjectile> ?? new List<ModProjectile>(Registry.Values);

        private static volatile bool _hooksInitialized = false;
        private static readonly object _hookInitLock = new object();

        public static void InitializeHooks()
        {
            if (_hooksInitialized) return;

            lock (_hookInitLock)
            {
                if (_hooksInitialized) return;

                On_Projectile.Update += Hook_Update;
                On_Projectile.SetDefaults += Hook_SetDefaults;
                On_Projectile.AI += Hook_AI;
                On_Projectile.Kill += Hook_Kill;
                On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier += Hook_NewProjectile;
                On_Projectile.AI_203_GetLightningColor += Hook_AI_203_GetLightningColor;

                _hooksInitialized = true;
            }
        }

        private static void Hook_Update(On_Projectile.orig_Update orig, Projectile self, int i)
        {
            tContentPatch.ModPatch.Patch_Projectile.ModList.ForTry(item => item.UpdatePrefix(self, i));
            orig(self, i);
            tContentPatch.ModPatch.Patch_Projectile.ModList.ForTry(item => item.UpdatePostfix(self, i));
        }

        private static void Hook_SetDefaults(On_Projectile.orig_SetDefaults orig, Projectile self, int Type)
        {
            tContentPatch.ModPatch.Patch_Projectile.ModList.ForTry(item => item.SetDefaultsPrefix(self, Type));
            orig(self, Type);
            if (Type >= ModProjectileOffset)
            {
                SetDefaults(self);
            }
            else
            {
                _modProjInstances.Remove(self);
            }
            tContentPatch.ModPatch.Patch_Projectile.ModList.ForTry(item => item.SetDefaultsPostfix(self, Type));
        }

        private static void Hook_AI(On_Projectile.orig_AI orig, Projectile self)
        {
            ModProjectile modProj = GetModProjectile(self);
            if (modProj != null)
            {
                if (modProj.PreAI())
                {
                    int savedType = self.type;
                    orig(self);
                    if (self.type != savedType && savedType >= ModProjectileOffset)
                    {
                        self.type = savedType;
                    }
                    modProj.AI();
                }
                modProj.PostAI();
                return;
            }
            orig(self);
        }

        private static void Hook_Kill(On_Projectile.orig_Kill orig, Projectile self)
        {
            tContentPatch.ModPatch.Patch_Projectile.ModList.ForTry(item => item.KillPrefix(self));
            if (self != null)
            {
                ModProjectile modProj = GetModProjectile(self);
                if (modProj != null)
                {
                    try
                    {
                        modProj.OnKill(self.timeLeft);
                    }
                    catch (Exception ex)
                    {
                        ModLoader.Log($"[ProjectileLoader] {modProj.Name}.OnKill 异常: {ex}");
                    }
                    finally
                    {
                        _modProjInstances.Remove(self);
                    }
                }
            }
            orig(self);
            tContentPatch.ModPatch.Patch_Projectile.ModList.ForTry(item => item.KillPostfix(self));
        }

        private static int Hook_NewProjectile(On_Projectile.orig_NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier orig,
            IEntitySource spawnSource, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1, float ai2, NewProjectileModifier modifer)
        {
            int result = orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer);
            tContentPatch.ModPatch.Patch_Projectile.ModList.ForTry(item => item.NewProjectilePostfix(result, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, modifer));
            return result;
        }

        private static Color Hook_AI_203_GetLightningColor(On_Projectile.orig_AI_203_GetLightningColor orig, Projectile self)
        {
            Color result = orig(self);
            tContentPatch.ModPatch.Patch_Projectile.AI_203_GetLightningColor(self, ref result);
            return result;
        }

        public static int Register(ModProjectile proj)
        {
            if (proj == null) return 0;

            InitializeHooks();

            int type = Registry.ReserveNextId();
            proj.SetType(type);
            Registry.Register(proj, type);

            ModContent.RegisterProjectileType(proj.GetType(), type);

            EnsureArraySizes(type);
            LoadProjectileTexture(proj);

            proj.SetStaticDefaults();
            ContentHookDispatcher.RegisterHookInstances(new[] { proj });

            try
            {
                Projectile sample = new Projectile();
                sample.type = type;
                SetDefaults(sample);
                ContentSamples.ProjectilesByType[type] = sample;
            }
            catch (Exception ex)
            {
                Logger.Warn($"向 ContentSamples 注册弹幕 [{proj.FullName}] 异常: {ex.Message}");
            }

            ModLoader.Log($"[ProjectileLoader] 成功注册弹幕: [{proj.FullName}] -> ProjID={type}");
            return type;
        }

        public static void EnsureArraySizes(int maxType)
        {
            int required = maxType + 64;

            if (TextureAssets.Projectile != null && TextureAssets.Projectile.Length <= required)
            {
                int newLen = Math.Max(required, TextureAssets.Projectile.Length * 2);
                Array.Resize(ref TextureAssets.Projectile, newLen);
                Texture2D fallback = TextureAssets.Projectile[0]?.Value ?? TileLoader.GetFallbackTexture();
                for (int i = 0; i < TextureAssets.Projectile.Length; i++)
                {
                    if (TextureAssets.Projectile[i] == null)
                    {
                        TextureAssets.Projectile[i] = AssetFactory.CreateLoaded(fallback, string.Empty);
                    }
                }
            }

            if (Main.projFrames != null && Main.projFrames.Length <= required)
            {
                int newLen = Math.Max(required, Main.projFrames.Length * 2);
                int oldLen = Main.projFrames.Length;
                Array.Resize(ref Main.projFrames, newLen);
                for (int i = oldLen; i < newLen; i++)
                {
                    Main.projFrames[i] = 1;
                }
            }

            if (Main.projHostile != null && Main.projHostile.Length <= required)
            {
                Array.Resize(ref Main.projHostile, Math.Max(required, Main.projHostile.Length * 2));
            }
            if (Main.projHook != null && Main.projHook.Length <= required)
            {
                Array.Resize(ref Main.projHook, Math.Max(required, Main.projHook.Length * 2));
            }
            if (Main.projPet != null && Main.projPet.Length <= required)
            {
                Array.Resize(ref Main.projPet, Math.Max(required, Main.projPet.Length * 2));
            }

            // 自动递归扩容 ProjectileID.Sets
            ArrayResizer.ResizeSets(typeof(ProjectileID.Sets), required, 500);

            // 扩容 Lang._projectileNameCache
            if (Lang._projectileNameCache != null && Lang._projectileNameCache.Length <= required)
            {
                int newLen = Math.Max(required, Lang._projectileNameCache.Length * 2);
                int oldLen = Lang._projectileNameCache.Length;
                Array.Resize(ref Lang._projectileNameCache, newLen);
                for (int i = oldLen; i < newLen; i++)
                {
                    Lang._projectileNameCache[i] = LocalizedText.Empty;
                }
            }
        }

        public static void LoadProjectileTexture(ModProjectile proj)
        {
            EnsureArraySizes(proj.Type);
            ContentTextureLoader.Load(
                proj.Mod,
                proj.GetType().Assembly,
                proj.Texture,
                proj.Name,
                proj.FullName,
                proj.Type,
                asset => TextureAssets.Projectile[proj.Type] = asset,
                () =>
                {
                    GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                           Main.instance?.GraphicsDevice ??
                                           Main.graphics?.GraphicsDevice;
                    return TextureAssets.Projectile[0]?.Value ?? (device != null ? new Texture2D(device, 16, 16) : null);
                }
            );
        }

        public static ModProjectile GetProjectile(int type)
        {
            return Registry.Get(type);
        }

        public static ModProjectile GetModProjectile(Projectile proj)
        {
            if (proj == null) return null;
            if (_modProjInstances.TryGetValue(proj, out ModProjectile instance))
            {
                if (instance.Type == proj.type || proj.type >= ModProjectileOffset)
                {
                    return instance;
                }
                _modProjInstances.Remove(proj);
            }
            if (proj.type >= ModProjectileOffset && Registry.TryGet(proj.type, out ModProjectile template))
            {
                ModProjectile newInst = template.Clone(proj);
                newInst.Projectile = proj;
                newInst.SetType(proj.type);
                _modProjInstances.Remove(proj);
                _modProjInstances.Add(proj, newInst);
                return newInst;
            }
            return null;
        }

        public static ModProjectile GetModProjectile(int type)
        {
            return Registry.Get(type);
        }

        public static T GetModProjectile<T>(Projectile proj) where T : ModProjectile => GetModProjectile(proj) as T;

        public static void SetDefaults(Projectile proj)
        {
            if (proj == null) return;

            if (Registry.TryGet(proj.type, out ModProjectile template))
            {
                ModProjectile instance = template.Clone(proj);
                instance.Projectile = proj;
                instance.SetType(proj.type);
                _modProjInstances.Remove(proj);
                _modProjInstances.Add(proj, instance);
                instance.SetDefaults();
            }
        }

        public static int ProjectileType(string modName, string projName)
        {
            return Registry.GetType(modName, projName);
        }

        public static int ProjectileType(string fullName)
        {
            return Registry.GetType(fullName);
        }

        public static void ResolveProjectileLocalization(ModProjectile proj)
        {
            if (proj == null) return;
            int type = proj.Type;
            string modName = proj.Mod?.Name ?? "Fargowiltas";
            string projName = proj.Name;

            string displayName = null;
            string[] nameKeys = new[]
            {
                $"Mods.{modName}.Projectiles.{projName}.DisplayName",
                $"Mods.{modName}.ProjectileName.{projName}",
                $"ProjectileName.{type}",
                $"Mods.{modName}.{projName}"
            };

            foreach (var key in nameKeys)
            {
                if (Language.Exists(key))
                {
                    string val = Language.GetTextValue(key);
                    if (!string.IsNullOrEmpty(val) && val != key)
                    {
                        displayName = val;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = System.Text.RegularExpressions.Regex.Replace(projName, "([a-z])([A-Z])", "$1 $2");
            }
            SetDisplayName(type, displayName);
        }

        public static void SetDisplayName(int type, string name)
        {
            Registry.SetDisplayName(type, name);
            EnsureArraySizes(type);
            if (Lang._projectileNameCache != null && type < Lang._projectileNameCache.Length)
            {
                Lang._projectileNameCache[type] = new LocalizedText($"ProjectileName.{type}", name);
            }
        }

        public static string GetDisplayName(int type)
        {
            string name = Registry.GetDisplayName(type);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (Registry.TryGet(type, out ModProjectile proj))
            {
                ResolveProjectileLocalization(proj);
                return Registry.GetDisplayName(type);
            }
            return string.Empty;
        }


        public static void Clear()
        {
            ContentTextureLoader.ClearAssets(TextureAssets.Projectile, ModProjectileOffset, Registry.NextId, TextureAssets.Projectile[0]?.Value);
            Registry.Clear();
        }
    }
}
