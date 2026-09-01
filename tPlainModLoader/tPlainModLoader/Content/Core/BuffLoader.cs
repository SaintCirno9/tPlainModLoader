using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content.Engine;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义 Buff 注册、贴图加载与生命周期分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class BuffLoader
    {
        public const int ModBuffOffset = 350;
        private static int _nextBuffID = ModBuffOffset;
        private static readonly Dictionary<int, ModBuff> _buffsByType = new Dictionary<int, ModBuff>();
        private static readonly Dictionary<int, string> _displayNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> _descriptions = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> _buffsByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static readonly FieldInfo _assetValueField = typeof(Asset<Texture2D>).GetField("<Value>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetStateField = typeof(Asset<Texture2D>).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _assetNameField = typeof(Asset<Texture2D>).GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int BuffCount => _nextBuffID;
        public static int NextBuffID => _nextBuffID;
        public static IReadOnlyCollection<ModBuff> Buffs => _buffsByType.Values;

        public static int Register(ModBuff buff)
        {
            if (buff == null) return 0;

            int type = _nextBuffID++;
            buff.SetType(type);
            _buffsByType[type] = buff;
            _buffsByName[buff.FullName] = type;
            _buffsByName[buff.Name] = type;

            ModContent.RegisterBuffType(buff.GetType(), type);

            EnsureArraySizes(type);
            LoadBuffTexture(buff);

            buff.SetStaticDefaults();
            ContentHookDispatcher.RegisterHookInstances(new[] { buff });

            ModLoader.Log($"[BuffLoader] 成功注册 Buff: [{buff.FullName}] -> BuffID={type}");
            return type;
        }

        public static void EnsureArraySizes(int maxType)
        {
            int required = maxType + 64;

            if (TextureAssets.Buff != null && TextureAssets.Buff.Length <= required)
            {
                int newLen = Math.Max(required, TextureAssets.Buff.Length * 2);
                Array.Resize(ref TextureAssets.Buff, newLen);
                Texture2D fallback = TextureAssets.Buff[0]?.Value ?? TileLoader.GetFallbackTexture();
                for (int i = 0; i < TextureAssets.Buff.Length; i++)
                {
                    if (TextureAssets.Buff[i] == null)
                    {
                        var emptyAsset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                        _assetNameField?.SetValue(emptyAsset, string.Empty);
                        _assetValueField?.SetValue(emptyAsset, fallback);
                        _assetStateField?.SetValue(emptyAsset, AssetState.Loaded);
                        TextureAssets.Buff[i] = emptyAsset;
                    }
                }
            }

            if (Main.debuff != null && Main.debuff.Length <= required)
            {
                Array.Resize(ref Main.debuff, Math.Max(required, Main.debuff.Length * 2));
            }
            if (Main.buffNoSave != null && Main.buffNoSave.Length <= required)
            {
                Array.Resize(ref Main.buffNoSave, Math.Max(required, Main.buffNoSave.Length * 2));
            }
            if (Main.buffNoTimeDisplay != null && Main.buffNoTimeDisplay.Length <= required)
            {
                Array.Resize(ref Main.buffNoTimeDisplay, Math.Max(required, Main.buffNoTimeDisplay.Length * 2));
            }
            if (Main.lightPet != null && Main.lightPet.Length <= required)
            {
                Array.Resize(ref Main.lightPet, Math.Max(required, Main.lightPet.Length * 2));
            }
            if (Main.vanityPet != null && Main.vanityPet.Length <= required)
            {
                Array.Resize(ref Main.vanityPet, Math.Max(required, Main.vanityPet.Length * 2));
            }
            if (Main.pvpBuff != null && Main.pvpBuff.Length <= required)
            {
                Array.Resize(ref Main.pvpBuff, Math.Max(required, Main.pvpBuff.Length * 2));
            }

            // 自动递归扩容 BuffID.Sets
            ResizeSetsClass(typeof(BuffID.Sets), required, 300);

            // 扩容 Lang._buffNameCache / Lang._buffDescriptionCache
            if (Lang._buffNameCache != null && Lang._buffNameCache.Length <= required)
            {
                int newLen = Math.Max(required, Lang._buffNameCache.Length * 2);
                int oldLen = Lang._buffNameCache.Length;
                Array.Resize(ref Lang._buffNameCache, newLen);
                for (int i = oldLen; i < newLen; i++)
                {
                    Lang._buffNameCache[i] = LocalizedText.Empty;
                }
            }
            if (Lang._buffDescriptionCache != null && Lang._buffDescriptionCache.Length <= required)
            {
                int newLen = Math.Max(required, Lang._buffDescriptionCache.Length * 2);
                int oldLen = Lang._buffDescriptionCache.Length;
                Array.Resize(ref Lang._buffDescriptionCache, newLen);
                for (int i = oldLen; i < newLen; i++)
                {
                    Lang._buffDescriptionCache[i] = LocalizedText.Empty;
                }
            }
        }

        public static void LoadBuffTexture(ModBuff buff)
        {
            try
            {
                EnsureArraySizes(buff.Type);
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;

                if (device == null) return;

                Texture2D texture = null;
                string texPath = buff.Texture;

                Assembly asm = buff.GetType().Assembly;
                string[] resNames = asm.GetManifestResourceNames();
                string targetRes = null;
                string normalizedTex = texPath?.Replace('/', '.')?.Replace('\\', '.');

                foreach (var res in resNames)
                {
                    if ((!string.IsNullOrEmpty(normalizedTex) && (res.Equals(normalizedTex, StringComparison.OrdinalIgnoreCase) || res.Equals(normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex, StringComparison.OrdinalIgnoreCase))) ||
                        res.Equals($"{buff.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{buff.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.Equals($"{buff.Name}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{buff.Name}.rawimg", StringComparison.OrdinalIgnoreCase))
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

                if (texture == null && buff.Mod != null && !string.IsNullOrEmpty(texPath))
                {
                    string cleanPath = texPath.Replace('\\', '/');
                    if (buff.Mod.HasAsset(cleanPath + ".png"))
                    {
                        using (Stream s = buff.Mod.GetFileStream(cleanPath + ".png"))
                        {
                            if (s != null) texture = Texture2D.FromStream(device, s);
                        }
                    }
                }

                if (texture == null)
                {
                    texture = TextureAssets.Buff[0]?.Value ?? new Texture2D(device, 32, 32);
                }

                var asset = (Asset<Texture2D>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Asset<Texture2D>));
                _assetNameField?.SetValue(asset, buff.FullName);
                _assetValueField?.SetValue(asset, texture);
                _assetStateField?.SetValue(asset, AssetState.Loaded);
                TextureAssets.Buff[buff.Type] = asset;
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[BuffLoader] 为 Buff [{buff.FullName}] 加载贴图异常: {ex.Message}");
            }
        }

        public static ModBuff GetBuff(int type)
        {
            _buffsByType.TryGetValue(type, out ModBuff buff);
            return buff;
        }

        public static int BuffType(string modName, string buffName)
        {
            if (string.IsNullOrEmpty(buffName)) return 0;
            if (!string.IsNullOrEmpty(modName) && _buffsByName.TryGetValue($"{modName}/{buffName}", out int type))
            {
                return type;
            }
            if (_buffsByName.TryGetValue(buffName, out int fallbackType))
            {
                return fallbackType;
            }
            return 0;
        }

        public static int BuffType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            if (_buffsByName.TryGetValue(fullName, out int type)) return type;
            int idx = fullName.IndexOf('/');
            if (idx >= 0 && idx < fullName.Length - 1)
            {
                string shortName = fullName.Substring(idx + 1);
                if (_buffsByName.TryGetValue(shortName, out int shortType)) return shortType;
            }
            return 0;
        }

        public static void Update(int type, Player player, ref int buffIndex)
        {
            if (_buffsByType.TryGetValue(type, out ModBuff buff))
            {
                buff.Update(player, ref buffIndex);
            }
        }

        public static void Update(int type, NPC npc, ref int buffIndex)
        {
            if (_buffsByType.TryGetValue(type, out ModBuff buff))
            {
                buff.Update(npc, ref buffIndex);
            }
        }

        public static bool ReApply(int type, Player player, int time, int buffIndex)
        {
            if (_buffsByType.TryGetValue(type, out ModBuff buff))
            {
                return buff.ReApply(player, time, buffIndex);
            }
            return false;
        }

        public static bool ReApply(int type, NPC npc, int time, int buffIndex)
        {
            if (_buffsByType.TryGetValue(type, out ModBuff buff))
            {
                return buff.ReApply(npc, time, buffIndex);
            }
            return false;
        }

        public static void ResolveBuffLocalization(ModBuff buff)
        {
            if (buff == null) return;
            int type = buff.Type;
            string modName = buff.Mod?.Name ?? "Fargowiltas";
            string buffName = buff.Name;

            string displayName = null;
            string[] nameKeys = new[]
            {
                $"Mods.{modName}.Buffs.{buffName}.DisplayName",
                $"Mods.{modName}.BuffName.{buffName}",
                $"BuffName.{type}",
                $"Mods.{modName}.{buffName}"
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
                displayName = System.Text.RegularExpressions.Regex.Replace(buffName, "([a-z])([A-Z])", "$1 $2");
            }
            SetDisplayName(type, displayName);

            string desc = null;
            string[] descKeys = new[]
            {
                $"Mods.{modName}.Buffs.{buffName}.Description",
                $"Mods.{modName}.BuffDescription.{buffName}",
                $"BuffDescription.{type}"
            };

            foreach (var key in descKeys)
            {
                if (Language.Exists(key))
                {
                    string val = Language.GetTextValue(key);
                    if (!string.IsNullOrEmpty(val) && val != key)
                    {
                        desc = val;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(desc))
            {
                SetDescription(type, desc);
            }
        }

        public static void SetDisplayName(int type, string name)
        {
            _displayNames[type] = name;
            EnsureArraySizes(type);
            if (Lang._buffNameCache != null && type < Lang._buffNameCache.Length)
            {
                Lang._buffNameCache[type] = new LocalizedText($"BuffName.{type}", name);
            }
        }

        public static string GetDisplayName(int type)
        {
            if (_displayNames.TryGetValue(type, out string name) && !string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (_buffsByType.TryGetValue(type, out ModBuff buff))
            {
                ResolveBuffLocalization(buff);
                if (_displayNames.TryGetValue(type, out string resolvedName))
                {
                    return resolvedName;
                }
            }
            return string.Empty;
        }

        public static void SetDescription(int type, string desc)
        {
            _descriptions[type] = desc;
            EnsureArraySizes(type);
            if (Lang._buffDescriptionCache != null && type < Lang._buffDescriptionCache.Length)
            {
                Lang._buffDescriptionCache[type] = new LocalizedText($"BuffDescription.{type}", desc);
            }
        }

        public static string GetDescription(int type)
        {
            if (_descriptions.TryGetValue(type, out string desc) && !string.IsNullOrEmpty(desc))
            {
                return desc;
            }
            if (_buffsByType.TryGetValue(type, out ModBuff buff))
            {
                ResolveBuffLocalization(buff);
                if (_descriptions.TryGetValue(type, out string resolvedDesc))
                {
                    return resolvedDesc;
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
            _buffsByType.Clear();
            _displayNames.Clear();
            _descriptions.Clear();
            _buffsByName.Clear();
            _nextBuffID = ModBuffOffset;
        }
    }
}
