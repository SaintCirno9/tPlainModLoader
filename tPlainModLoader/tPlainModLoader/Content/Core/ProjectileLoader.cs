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
using TPML.Content.Engine;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义弹幕注册、贴图加载与生命周期分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class ProjectileLoader
    {
        public const int ModProjectileOffset = 1100;
        private static int _nextProjID = ModProjectileOffset;
        private static readonly Dictionary<int, ModProjectile> _projsByType = new Dictionary<int, ModProjectile>();
        private static readonly ConditionalWeakTable<Projectile, ModProjectile> _modProjInstances = new ConditionalWeakTable<Projectile, ModProjectile>();
        private static readonly Dictionary<int, string> _displayNames = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> _projsByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly FieldInfo _assetValueField = typeof(Asset<Texture2D>).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetStateField = typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetNameField = typeof(Asset<Texture2D>).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int ProjectileCount => _nextProjID;
        public static int NextProjID => _nextProjID;
        public static IReadOnlyCollection<ModProjectile> Projectiles => _projsByType.Values;

        private static bool _hooksInitialized = false;

        public static void InitializeHooks()
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

            int type = _nextProjID++;
            proj.SetType(type);
            _projsByType[type] = proj;
            _projsByName[proj.FullName] = type;
            _projsByName[proj.Name] = type;

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
            catch { }

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
                        var emptyAsset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                        _assetNameField?.SetValue(emptyAsset, string.Empty);
                        _assetValueField?.SetValue(emptyAsset, fallback);
                        _assetStateField?.SetValue(emptyAsset, AssetState.Loaded);
                        TextureAssets.Projectile[i] = emptyAsset;
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
            ResizeSetsClass(typeof(ProjectileID.Sets), required, 500);

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
            try
            {
                EnsureArraySizes(proj.Type);
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;

                if (device == null) return;

                Texture2D texture = null;
                string texPath = proj.Texture;

                Assembly asm = proj.GetType().Assembly;
                string[] resNames = asm.GetManifestResourceNames();
                string targetRes = null;
                string normalizedTex = texPath?.Replace('/', '.')?.Replace('\\', '.');

                foreach (var res in resNames)
                {
                    if ((!string.IsNullOrEmpty(normalizedTex) && (res.Equals(normalizedTex, StringComparison.OrdinalIgnoreCase) || res.Equals(normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex, StringComparison.OrdinalIgnoreCase))) ||
                        res.Equals($"{proj.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{proj.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.Equals($"{proj.Name}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{proj.Name}.rawimg", StringComparison.OrdinalIgnoreCase))
                    {
                        targetRes = res;
                        break;
                    }
                }

                if (targetRes != null)
                {
                    using (Stream stream = asm.GetManifestResourceStream(targetRes))
                    {
                        if (stream != null)
                        {
                            texture = Texture2D.FromStream(device, stream);
                        }
                    }
                }

                if (texture == null && proj.Mod != null && !string.IsNullOrEmpty(texPath))
                {
                    string cleanPath = texPath.Replace('\\', '/');
                    if (proj.Mod.HasAsset(cleanPath + ".png"))
                    {
                        using (Stream s = proj.Mod.GetFileStream(cleanPath + ".png"))
                        {
                            if (s != null) texture = Texture2D.FromStream(device, s);
                        }
                    }
                }

                if (texture == null)
                {
                    texture = TextureAssets.Projectile[0]?.Value ?? new Texture2D(device, 16, 16);
                }

                var asset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                _assetNameField?.SetValue(asset, proj.FullName);
                _assetValueField?.SetValue(asset, texture);
                _assetStateField?.SetValue(asset, AssetState.Loaded);
                TextureAssets.Projectile[proj.Type] = asset;
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[ProjectileLoader] 为弹幕 [{proj.FullName}] 加载贴图异常: {ex.Message}");
            }
        }

        public static ModProjectile GetProjectile(int type)
        {
            _projsByType.TryGetValue(type, out ModProjectile proj);
            return proj;
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
            if (proj.type >= ModProjectileOffset && _projsByType.TryGetValue(proj.type, out ModProjectile template))
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
            _projsByType.TryGetValue(type, out ModProjectile proj);
            return proj;
        }

        public static T GetModProjectile<T>(Projectile proj) where T : ModProjectile => GetModProjectile(proj) as T;

        public static void SetDefaults(Projectile proj)
        {
            if (proj == null) return;

            if (_projsByType.TryGetValue(proj.type, out ModProjectile template))
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
            if (string.IsNullOrEmpty(projName)) return 0;
            if (!string.IsNullOrEmpty(modName) && _projsByName.TryGetValue($"{modName}/{projName}", out int type))
            {
                return type;
            }
            if (_projsByName.TryGetValue(projName, out int fallbackType))
            {
                return fallbackType;
            }
            return 0;
        }

        public static int ProjectileType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            if (_projsByName.TryGetValue(fullName, out int type)) return type;
            int idx = fullName.IndexOf('/');
            if (idx >= 0 && idx < fullName.Length - 1)
            {
                string shortName = fullName.Substring(idx + 1);
                if (_projsByName.TryGetValue(shortName, out int shortType)) return shortType;
            }
            return 0;
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
            _displayNames[type] = name;
            EnsureArraySizes(type);
            if (Lang._projectileNameCache != null && type < Lang._projectileNameCache.Length)
            {
                Lang._projectileNameCache[type] = new LocalizedText($"ProjectileName.{type}", name);
            }
        }

        public static string GetDisplayName(int type)
        {
            if (_displayNames.TryGetValue(type, out string name) && !string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (_projsByType.TryGetValue(type, out ModProjectile proj))
            {
                ResolveProjectileLocalization(proj);
                if (_displayNames.TryGetValue(type, out string resolvedName))
                {
                    return resolvedName;
                }
            }
            return string.Empty;
        }

        private static void ResizeSetsClass(Type type, int required, int minMatchLen)
        {
            if (type == null) return;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (field.FieldType.IsArray && field.FieldType.GetArrayRank() == 1)
                {
                    Array arr = field.GetValue(null) as Array;
                    if (arr != null && arr.Length >= minMatchLen && arr.Length <= required)
                    {
                        int newLen = Math.Max(required, arr.Length * 2);
                        Array newArr = Array.CreateInstance(field.FieldType.GetElementType(), newLen);
                        Array.Copy(arr, newArr, arr.Length);
                        field.SetValue(null, newArr);
                    }
                }
            }

            foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                ResizeSetsClass(nested, required, minMatchLen);
            }
        }

        public static void Clear()
        {
            _projsByType.Clear();
            _displayNames.Clear();
            _projsByName.Clear();
            _nextProjID = ModProjectileOffset;
        }
    }
}
