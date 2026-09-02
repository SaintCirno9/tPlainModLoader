using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using TPML.Utils;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义 NPC (ModNPC) 注册与生命周期分发中心
    /// </summary>
    public static class NPCLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("NPCLoader");

        public const int ModNPCOffset = 700;
        internal static readonly ContentRegistry<ModNPC> Registry = new ContentRegistry<ModNPC>(ModNPCOffset);
        private static readonly ConditionalWeakTable<NPC, ModNPC> _modNPCInstances = new ConditionalWeakTable<NPC, ModNPC>();
        private static readonly Dictionary<string, int> _headSlots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static int NPCCount => Registry.NextId;
        public static int NextNPCID => Registry.NextId;
        public static IReadOnlyCollection<ModNPC> NPCs => Registry.Values as IReadOnlyCollection<ModNPC> ?? new List<ModNPC>(Registry.Values);

        private static volatile bool _hooksInitialized = false;
        private static readonly object _hookInitLock = new object();

        public static void InitializeHooks()
        {
            if (_hooksInitialized) return;

            lock (_hookInitLock)
            {
                if (_hooksInitialized) return;

                On_NPC.UpdateNPC += Hook_UpdateNPC;
                On_NPC.SetDefaults += Hook_SetDefaults;
                On_NPC.NewNPC += Hook_NewNPC;

                _hooksInitialized = true;
            }
        }

        private static void Hook_UpdateNPC(On_NPC.orig_UpdateNPC orig, NPC self, int i)
        {
            orig(self, i);
        }

        private static void Hook_SetDefaults(On_NPC.orig_SetDefaults orig, NPC self, int Type, NPCSpawnParams spawnparams)
        {
            orig(self, Type, spawnparams);
            if (Type >= ModNPCOffset)
            {
                SetDefaults(self);
            }
            else
            {
                foreach (var gNpc in ContentHookDispatcher.ActiveGlobalNPCs)
                {
                    try { gNpc.SetDefaults(self); } catch (Exception ex) { ModLoader.Log($"[NPCLoader] GlobalNPC.SetDefaults 异常: {ex.Message}"); }
                }
            }
        }

        private static int Hook_NewNPC(On_NPC.orig_NewNPC orig,
            Terraria.DataStructures.IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        {
            return orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
        }

        public static int Register(ModNPC npc)
        {
            if (npc == null) return 0;

            int type = Registry.ReserveNextId();
            npc.SetType(type);
            Registry.Register(npc, type);

            ModContent.RegisterNPCType(npc.GetType(), type);

            EnsureArraySizes(type);
            LoadNPCTexture(npc);

            npc.SetStaticDefaults();
            ContentHookDispatcher.RegisterHookInstances(new[] { npc });

            try
            {
                NPC sample = new NPC();
                sample.type = type;
                SetDefaults(sample);
                ContentSamples.NpcsByNetId[type] = sample;

                if (!string.IsNullOrEmpty(npc.FullName))
                {
                    ContentSamples.NpcPersistentIdsByNetIds[type] = npc.FullName;
                    ContentSamples.NpcNetIdsByPersistentIds[npc.FullName] = type;
                    if (!string.IsNullOrEmpty(npc.Name) && !ContentSamples.NpcNetIdsByPersistentIds.ContainsKey(npc.Name))
                    {
                        ContentSamples.NpcNetIdsByPersistentIds[npc.Name] = type;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"向 ContentSamples 注册 NPC [{npc.FullName}] 异常: {ex.Message}");
            }

            ModLoader.Log($"[NPCLoader] 成功注册 NPC: [{npc.FullName}] -> NPCID={type}");
            return type;
        }

        public static void EnsureArraySizes(int maxType)
        {
            int required = maxType + 64;

            if (TextureAssets.Npc != null && TextureAssets.Npc.Length <= required)
            {
                int newLen = Math.Max(required, TextureAssets.Npc.Length * 2);
                Array.Resize(ref TextureAssets.Npc, newLen);
                Texture2D fallback = TextureAssets.Npc[0]?.Value ?? TileLoader.GetFallbackTexture();
                for (int i = 0; i < TextureAssets.Npc.Length; i++)
                {
                    if (TextureAssets.Npc[i] == null)
                    {
                        TextureAssets.Npc[i] = AssetFactory.CreateLoaded(fallback, string.Empty);
                    }
                }
            }

            if (Main.npcFrameCount != null && Main.npcFrameCount.Length <= required)
            {
                int newLen = Math.Max(required, Main.npcFrameCount.Length * 2);
                int oldLen = Main.npcFrameCount.Length;
                Array.Resize(ref Main.npcFrameCount, newLen);
                for (int i = oldLen; i < newLen; i++)
                {
                    Main.npcFrameCount[i] = 1;
                }
            }

            if (Main.npcCatchable != null && Main.npcCatchable.Length <= required)
            {
                Array.Resize(ref Main.npcCatchable, Math.Max(required, Main.npcCatchable.Length * 2));
            }

            // 自动递归扩容 NPCID.Sets 及其所有嵌套类中的数组字段
            ArrayResizer.ResizeSets(typeof(NPCID.Sets), required, 500);

            // 扩容 Lang._npcNameCache
            if (Lang._npcNameCache != null && Lang._npcNameCache.Length <= required)
            {
                int newLen = Math.Max(required, Lang._npcNameCache.Length * 2);
                int oldLen = Lang._npcNameCache.Length;
                Array.Resize(ref Lang._npcNameCache, newLen);
                for (int i = oldLen; i < newLen; i++)
                {
                    Lang._npcNameCache[i] = LocalizedText.Empty;
                }
            }
        }

        public static void LoadNPCTexture(ModNPC npc)
        {
            EnsureArraySizes(npc.Type);
            ContentTextureLoader.Load(
                npc.Mod,
                npc.GetType().Assembly,
                npc.Texture,
                npc.Name,
                npc.FullName,
                npc.Type,
                asset => TextureAssets.Npc[npc.Type] = asset,
                () =>
                {
                    GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                           Main.instance?.GraphicsDevice ??
                                           Main.graphics?.GraphicsDevice;
                    return TextureAssets.Npc[0]?.Value ?? (device != null ? new Texture2D(device, 16, 16) : null);
                }
            );
        }

        public static ModNPC GetNPC(int type)
        {
            return Registry.Get(type);
        }

        public static ModNPC GetModNPC(NPC npc)
        {
            if (npc == null || npc.type < NPCID.Count) return null;
            if (_modNPCInstances.TryGetValue(npc, out ModNPC instance))
            {
                return instance;
            }
            if (Registry.TryGet(npc.type, out ModNPC template))
            {
                ModNPC newInst = template.Clone(npc);
                newInst.NPC = npc;
                newInst.SetType(npc.type);
                _modNPCInstances.Add(npc, newInst);
                return newInst;
            }
            return null;
        }

        public static ModNPC GetModNPC(int type)
        {
            return Registry.Get(type);
        }

        public static T GetModNPC<T>(NPC npc) where T : ModNPC => GetModNPC(npc) as T;

        public static void SetDefaults(NPC npc)
        {
            if (npc == null) return;

            if (Registry.TryGet(npc.type, out ModNPC template))
            {
                ModNPC instance = template.Clone(npc);
                instance.NPC = npc;
                instance.SetType(npc.type);
                _modNPCInstances.Remove(npc);
                _modNPCInstances.Add(npc, instance);
                instance.SetDefaults();

                string name = GetDisplayName(template.Type);
                if (!string.IsNullOrEmpty(name))
                {
                    npc.GivenName = name;
                }
            }

            foreach (var gNpc in ContentHookDispatcher.ActiveGlobalNPCs)
            {
                try { gNpc.SetDefaults(npc); } catch (Exception ex) { ModLoader.Log($"[NPCLoader] GlobalNPC.SetDefaults 异常: {ex.Message}"); }
            }
        }

        public static int NPCType(string modName, string npcName)
        {
            return Registry.GetType(modName, npcName);
        }

        public static int NPCType(string fullName)
        {
            return Registry.GetType(fullName);
        }

        public static void ResolveNPCLocalization(ModNPC npc)
        {
            if (npc == null) return;
            int type = npc.Type;
            string modName = npc.Mod?.Name ?? "Fargowiltas";
            string npcName = npc.Name;

            string displayName = null;
            string[] nameKeys = new[]
            {
                $"Mods.{modName}.NPCs.{npcName}.DisplayName",
                $"Mods.{modName}.NPCName.{npcName}",
                $"NPCName.{type}",
                $"Mods.{modName}.{npcName}"
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
                displayName = System.Text.RegularExpressions.Regex.Replace(npcName, "([a-z])([A-Z])", "$1 $2");
            }
            SetDisplayName(type, displayName);
        }

        public static void SetDisplayName(int type, string name)
        {
            Registry.SetDisplayName(type, name);
            EnsureArraySizes(type);
            if (Lang._npcNameCache != null && type < Lang._npcNameCache.Length)
            {
                Lang._npcNameCache[type] = new LocalizedText($"NPCName.{type}", name);
            }
        }

        public static string GetDisplayName(int type)
        {
            string name = Registry.GetDisplayName(type);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (Registry.TryGet(type, out ModNPC npc))
            {
                ResolveNPCLocalization(npc);
                return Registry.GetDisplayName(type);
            }
            return string.Empty;
        }

        private static int _nextHeadSlot = 100;

        public static int RegisterHeadSlot(string texture)
        {
            if (string.IsNullOrEmpty(texture)) return -1;
            if (_headSlots.TryGetValue(texture, out int existing)) return existing;
            int slot = _nextHeadSlot++;
            _headSlots[texture] = slot;
            return slot;
        }

        public static int GetHeadSlot(string texture)
        {
            if (string.IsNullOrEmpty(texture)) return -1;
            return _headSlots.TryGetValue(texture, out int slot) ? slot : -1;
        }

        public static void AddHeadSlot(string texture, int slot)
        {
            if (!string.IsNullOrEmpty(texture))
            {
                _headSlots[texture] = slot;
            }
        }


        public static void Clear()
        {
            ContentTextureLoader.ClearAssets(TextureAssets.Npc, ModNPCOffset, Registry.NextId, TextureAssets.Npc[0]?.Value);
            Registry.Clear();
            _headSlots.Clear();
            _nextHeadSlot = 100;
        }
    }

    /// <summary>
    /// TPML NPC 头部槽位门面类（对齐 tML NPCHeadLoader）
    /// </summary>
    public static class NPCHeadLoader
    {
        public static int GetHeadSlot(string texture) => NPCLoader.GetHeadSlot(texture);
        public static int GetBossHeadSlot(string texture) => NPCLoader.GetHeadSlot(texture);
        public static int GetNPCHeadSlot(int type)
        {
            var npc = NPCLoader.GetNPC(type);
            return npc != null ? NPCLoader.GetHeadSlot(npc.HeadTexture) : -1;
        }
    }
}
