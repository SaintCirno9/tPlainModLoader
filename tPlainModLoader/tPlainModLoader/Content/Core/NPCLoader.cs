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
using TPML.Content.Engine;
using TPML.Core.Logging;
using tContentPatch.ModPatch;
using tContentPatch.Utils;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义 NPC (ModNPC) 注册与生命周期分发中心
    /// </summary>
    public static class NPCLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("NPCLoader");

        public const int ModNPCOffset = 700;
        private static int _nextNPCID = ModNPCOffset;
        private static readonly Dictionary<int, ModNPC> _npcsByType = new Dictionary<int, ModNPC>();
        private static readonly ConditionalWeakTable<NPC, ModNPC> _modNPCInstances = new ConditionalWeakTable<NPC, ModNPC>();
        private static readonly Dictionary<int, string> _displayNames = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> _npcsByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _headSlots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);


        public static int NPCCount => _nextNPCID;
        public static int NextNPCID => _nextNPCID;
        public static IReadOnlyCollection<ModNPC> NPCs => _npcsByType.Values;

        private static bool _hooksInitialized = false;

        public static void InitializeHooks()
        {
            if (_hooksInitialized) return;

            On_NPC.UpdateNPC += Hook_UpdateNPC;
            On_NPC.SetDefaults += Hook_SetDefaults;
            On_NPC.NewNPC += Hook_NewNPC;

            _hooksInitialized = true;
        }

        private static void Hook_UpdateNPC(On_NPC.orig_UpdateNPC orig, NPC self, int i)
        {
            tContentPatch.ModPatch.Patch_NPC.ModList.ForTry(item => item.UpdateNPCPrefix(self, i));
            orig(self, i);
            tContentPatch.ModPatch.Patch_NPC.ModList.ForTry(item => item.UpdateNPCPostfix(self, i));
        }

        private static void Hook_SetDefaults(On_NPC.orig_SetDefaults orig, NPC self, int Type, NPCSpawnParams spawnparams)
        {
            tContentPatch.ModPatch.Patch_NPC.ModList.ForTry(item => item.SetDefaultsPrefix(self, Type, spawnparams));
            orig(self, Type, spawnparams);
            tContentPatch.ModPatch.Patch_NPC.ModList.ForTry(item => item.SetDefaultsPostfix(self, Type, spawnparams));
        }

        private static int Hook_NewNPC(On_NPC.orig_NewNPC orig,
            Terraria.DataStructures.IEntitySource source, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        {
            int result = orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
            tContentPatch.ModPatch.Patch_NPC.ModList.ForTry(item => item.NewNPCPostfix(result, source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target));
            return result;
        }

        public static int Register(ModNPC npc)
        {
            if (npc == null) return 0;

            int type = _nextNPCID++;
            npc.SetType(type);
            _npcsByType[type] = npc;
            _npcsByName[npc.FullName] = type;
            _npcsByName[npc.Name] = type;

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
            try
            {
                EnsureArraySizes(npc.Type);
                GraphicsDevice device = Main.spriteBatch?.GraphicsDevice ??
                                       Main.instance?.GraphicsDevice ??
                                       Main.graphics?.GraphicsDevice;

                if (device == null) return;

                Texture2D texture = null;
                string texPath = npc.Texture;

                Assembly asm = npc.GetType().Assembly;
                string[] resNames = asm.GetManifestResourceNames();
                string targetRes = null;
                string normalizedTex = texPath?.Replace('/', '.')?.Replace('\\', '.');

                foreach (var res in resNames)
                {
                    if ((!string.IsNullOrEmpty(normalizedTex) && (res.Equals(normalizedTex, StringComparison.OrdinalIgnoreCase) || res.Equals(normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex + ".png", StringComparison.OrdinalIgnoreCase) || res.EndsWith("." + normalizedTex, StringComparison.OrdinalIgnoreCase))) ||
                        res.Equals($"{npc.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{npc.Name}.png", StringComparison.OrdinalIgnoreCase) ||
                        res.Equals($"{npc.Name}.rawimg", StringComparison.OrdinalIgnoreCase) ||
                        res.EndsWith($".{npc.Name}.rawimg", StringComparison.OrdinalIgnoreCase))
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

                if (texture == null && npc.Mod != null && !string.IsNullOrEmpty(texPath))
                {
                    string cleanPath = texPath.Replace('\\', '/');
                    if (npc.Mod.HasAsset(cleanPath + ".png"))
                    {
                        using (Stream s = npc.Mod.GetFileStream(cleanPath + ".png"))
                        {
                            if (s != null) texture = Texture2D.FromStream(device, s);
                        }
                    }
                }

                if (texture == null)
                {
                    texture = TextureAssets.Npc[0]?.Value ?? new Texture2D(device, 16, 16);
                }

                TextureAssets.Npc[npc.Type] = AssetFactory.CreateLoaded(texture, npc.FullName);
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[NPCLoader] 为 NPC [{npc.FullName}] 加载贴图异常: {ex.Message}");
            }
        }

        public static ModNPC GetNPC(int type)
        {
            _npcsByType.TryGetValue(type, out ModNPC npc);
            return npc;
        }

        public static ModNPC GetModNPC(NPC npc)
        {
            if (npc == null || npc.type < NPCID.Count) return null;
            if (_modNPCInstances.TryGetValue(npc, out ModNPC instance))
            {
                return instance;
            }
            if (_npcsByType.TryGetValue(npc.type, out ModNPC template))
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
            _npcsByType.TryGetValue(type, out ModNPC npc);
            return npc;
        }

        public static T GetModNPC<T>(NPC npc) where T : ModNPC => GetModNPC(npc) as T;

        public static void SetDefaults(NPC npc)
        {
            if (npc == null) return;

            if (_npcsByType.TryGetValue(npc.type, out ModNPC template))
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
            if (string.IsNullOrEmpty(npcName)) return 0;
            if (!string.IsNullOrEmpty(modName) && _npcsByName.TryGetValue($"{modName}/{npcName}", out int type))
            {
                return type;
            }
            if (_npcsByName.TryGetValue(npcName, out int fallbackType))
            {
                return fallbackType;
            }
            return 0;
        }

        public static int NPCType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            if (_npcsByName.TryGetValue(fullName, out int type)) return type;
            int idx = fullName.IndexOf('/');
            if (idx >= 0 && idx < fullName.Length - 1)
            {
                string shortName = fullName.Substring(idx + 1);
                if (_npcsByName.TryGetValue(shortName, out int shortType)) return shortType;
            }
            return 0;
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
            _displayNames[type] = name;
            EnsureArraySizes(type);
            if (Lang._npcNameCache != null && type < Lang._npcNameCache.Length)
            {
                Lang._npcNameCache[type] = new LocalizedText($"NPCName.{type}", name);
            }
        }

        public static string GetDisplayName(int type)
        {
            if (_displayNames.TryGetValue(type, out string name) && !string.IsNullOrEmpty(name))
            {
                return name;
            }
            if (_npcsByType.TryGetValue(type, out ModNPC npc))
            {
                ResolveNPCLocalization(npc);
                if (_displayNames.TryGetValue(type, out string resolvedName))
                {
                    return resolvedName;
                }
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
            _npcsByType.Clear();
            _displayNames.Clear();
            _npcsByName.Clear();
            _headSlots.Clear();
            _nextNPCID = ModNPCOffset;
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
