using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// NPC 生命周期补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_NPC : ListCopy<PatchNPC>
    {
        private static List<PatchNPC> mod = new List<PatchNPC>();

        public Patch_NPC() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var npc = typeof(NPC);

            // NPC.UpdateNPC(int)
            HookRegistry.Add(GetInstance(npc, "UpdateNPC", typeof(int)),
                (Action<Action<NPC, int>, NPC, int>)((orig, self, i) =>
                {
                    UpdateNPCPrefix(self, i);
                    orig(self, i);
                    UpdateNPCPostfix(self, i);
                }));

            // NPC.SetDefaults(int, NPCSpawnParams)
            HookRegistry.Add(GetInstance(npc, "SetDefaults", typeof(int), typeof(NPCSpawnParams)),
                (Action<Action<NPC, int, NPCSpawnParams>, NPC, int, NPCSpawnParams>)((orig, self, Type, spawnparams) =>
                {
                    SetDefaultsPrefix(self, Type, spawnparams);
                    orig(self, Type, spawnparams);
                    SetDefaultsPostfix(self, Type, spawnparams);
                }));

            // NPC.NewNPC(IEntitySource, int, int, int, int, float, float, float, float, int)（静态，返回 int）
            HookRegistry.Add(GetStatic(npc, "NewNPC",
                    typeof(IEntitySource), typeof(int), typeof(int), typeof(int), typeof(int),
                    typeof(float), typeof(float), typeof(float), typeof(float), typeof(int)),
                (Func<Func<IEntitySource, int, int, int, int, float, float, float, float, int, int>,
                    IEntitySource, int, int, int, int, float, float, float, float, int, int>)((orig, source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target) =>
                {
                    int result = orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
                    NewNPCPostfix(result, source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
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

        public static void UpdateNPCPrefix(NPC __instance, int i)
        {
            mod.ForTry(item => item.UpdateNPCPrefix(__instance, i));
        }

        public static void UpdateNPCPostfix(NPC __instance, int i)
        {
            mod.ForTry(item => item.UpdateNPCPostfix(__instance, i));
        }

        public static void SetDefaultsPrefix(NPC __instance, int Type, NPCSpawnParams spawnparams)
        {
            mod.ForTry(item => item.SetDefaultsPrefix(__instance, Type, spawnparams));
        }

        public static void SetDefaultsPostfix(NPC __instance, int Type, NPCSpawnParams spawnparams)
        {
            mod.ForTry(item => item.SetDefaultsPostfix(__instance, Type, spawnparams));
        }

        public static void NewNPCPostfix(int __result, IEntitySource source,
            int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        {
            mod.ForTry(item => item.NewNPCPostfix(__result, source,
                X, Y, Type, Start, ai0, ai1, ai2, ai3, Target));
        }
    }
}
