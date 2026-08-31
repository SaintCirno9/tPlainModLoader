using System;
using System.Collections.Generic;
using Terraria;
using TPML.Content.Engine;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// WorldGen 补丁（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class Patch_WorldGen : ListCopy<PatchWorldGen>
    {
        private static List<PatchWorldGen> mod = new List<PatchWorldGen>();

        public Patch_WorldGen() : base(mod) { }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            var worldGen = typeof(WorldGen);

            // WorldGen.Convert(int, int, int, bool, bool)（静态 void，prefix 返回 false 跳过）
            HookRegistry.Add(MethodLookup.Static(worldGen, "Convert", typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool)),
                (Action<Action<int, int, int, bool, bool>, int, int, int, bool, bool>)((orig, i2, j2, conversionType, tiles, walls) =>
                {
                    if (!ConvertPrefix(i2, j2, conversionType, tiles, walls)) return;
                    orig(i2, j2, conversionType, tiles, walls);
                }));

            // WorldGen.UpdateWorld()（静态）
            HookRegistry.Add(worldGen.GetMethod("UpdateWorld", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic),
                (Action<Action>)(orig =>
                {
                    UpdateWorldPrefix();
                    orig();
                    UpdateWorldPostfix();
                }));
        }

        public static bool ConvertPrefix(int i2, int j2, int conversionType, bool tiles, bool walls)
        {
            return mod.ForTry(item => item.CanConvert(i2, j2, conversionType, tiles, walls));
        }

        public static void UpdateWorldPrefix()
        {
            mod.ForTry(item => item.UpdateWorldPrefix());
        }

        public static void UpdateWorldPostfix()
        {
            mod.ForTry(item => item.UpdateWorldPostfix());
        }
    }
}
