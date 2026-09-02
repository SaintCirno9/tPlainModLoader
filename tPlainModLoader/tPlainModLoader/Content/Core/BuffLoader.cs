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
using TPML.Content.Assets;
using TPML.Content.Core;
using TPML.Content.Engine;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义 Buff 注册、贴图加载与生命周期分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class BuffLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("BuffLoader");

        public const int ModBuffOffset = 350;
        internal static readonly ContentRegistry<ModBuff> Registry = new ContentRegistry<ModBuff>(ModBuffOffset);
        private static readonly Dictionary<int, string> _descriptions = new Dictionary<int, string>();

        public static int BuffCount => Registry.NextId;
        public static int NextBuffID => Registry.NextId;
        public static IReadOnlyCollection<ModBuff> Buffs => Registry.Values as IReadOnlyCollection<ModBuff> ?? new List<ModBuff>(Registry.Values);

        public static int Register(ModBuff buff)
        {
            if (buff == null) return 0;

            int type = Registry.ReserveNextId();
            buff.SetType(type);
            Registry.Register(buff, type);

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
                        TextureAssets.Buff[i] = AssetFactory.CreateLoaded(fallback, string.Empty);
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
            ArrayResizer.ResizeSets(typeof(BuffID.Sets), required, 300);

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
            EnsureArraySizes(buff.Type);
            ContentTextureLoader.Load(
                buff.Mod,
                buff.GetType().Assembly,
                buff.Texture,
                buff.Name,
                buff.FullName,
                buff.Type,
                asset => TextureAssets.Buff[buff.Type] = asset,
                () =>
                {
                    GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                           Main.instance?.GraphicsDevice ??
                                           Main.graphics?.GraphicsDevice;
                    return TextureAssets.Buff[0]?.Value ?? (device != null ? new Texture2D(device, 32, 32) : null);
                }
            );
        }

        public static ModBuff GetBuff(int type)
        {
            return Registry.Get(type);
        }

        public static int BuffType(string modName, string buffName)
        {
            return Registry.GetType(modName, buffName);
        }

        public static int BuffType(string fullName)
        {
            return Registry.GetType(fullName);
        }

        public static void Update(int type, Player player, ref int buffIndex)
        {
            if (Registry.TryGet(type, out ModBuff buff))
            {
                buff.Update(player, ref buffIndex);
            }
        }

        public static void Update(int type, NPC npc, ref int buffIndex)
        {
            if (Registry.TryGet(type, out ModBuff buff))
            {
                buff.Update(npc, ref buffIndex);
            }
        }

        public static bool ReApply(int type, Player player, int time, int buffIndex)
        {
            if (Registry.TryGet(type, out ModBuff buff))
            {
                return buff.ReApply(player, time, buffIndex);
            }
            return false;
        }

        public static bool ReApply(int type, NPC npc, int time, int buffIndex)
        {
            if (Registry.TryGet(type, out ModBuff buff))
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
            Registry.SetDisplayName(type, name);
            EnsureArraySizes(type);
            if (Lang._buffNameCache != null && type < Lang._buffNameCache.Length)
            {
                Lang._buffNameCache[type] = new LocalizedText($"BuffName.{type}", name);
            }
        }

        public static string GetDisplayName(int type)
        {
            string name = Registry.GetDisplayName(type);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (Registry.TryGet(type, out ModBuff buff))
            {
                ResolveBuffLocalization(buff);
                return Registry.GetDisplayName(type);
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
            if (Registry.TryGet(type, out ModBuff buff))
            {
                ResolveBuffLocalization(buff);
                if (_descriptions.TryGetValue(type, out string resolvedDesc))
                {
                    return resolvedDesc;
                }
            }
            return string.Empty;
        }


        public static void Clear()
        {
            ContentTextureLoader.ClearAssets(TextureAssets.Buff, ModBuffOffset, Registry.NextId, TextureAssets.Buff[0]?.Value);
            Registry.Clear();
            _descriptions.Clear();
        }
    }
}
