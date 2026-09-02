#pragma warning disable CS0618
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// NPC 生命周期补丁列表持有类（已收敛至 NPCLoader 统一分发）
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_NPC : ListCopy<PatchNPC>
    {
        private static readonly List<PatchNPC> mod = new List<PatchNPC>();
        internal static List<PatchNPC> ModList => mod;

        public Patch_NPC() : base(mod) { }

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
