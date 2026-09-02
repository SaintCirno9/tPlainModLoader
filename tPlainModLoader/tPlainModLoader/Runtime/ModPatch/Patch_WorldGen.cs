#pragma warning disable CS0618
using System.Collections.Generic;
using Terraria;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// WorldGen 强类型门面调度类
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_WorldGen : ListCopy<PatchWorldGen>
    {
        private static readonly List<PatchWorldGen> mod = new List<PatchWorldGen>();
        internal static List<PatchWorldGen> ModList => mod;
        private static bool _hooksInitialized = false;

        public Patch_WorldGen() : base(mod) { }

        /// <summary>集中注册 WorldGen 强类型 HookGen 钩子</summary>
        public static void RegisterAll()
        {
            if (_hooksInitialized) return;

            On_WorldGen.Convert_int_int_int_bool_bool += Hook_Convert;
            On_WorldGen.UpdateWorld += Hook_UpdateWorld;

            _hooksInitialized = true;
        }

        private static void Hook_Convert(On_WorldGen.orig_Convert_int_int_int_bool_bool orig, int i2, int j2, int conversionType, bool tiles, bool walls)
        {
            if (!ConvertPrefix(i2, j2, conversionType, tiles, walls)) return;
            orig(i2, j2, conversionType, tiles, walls);
        }

        private static void Hook_UpdateWorld(On_WorldGen.orig_UpdateWorld orig)
        {
            UpdateWorldPrefix();
            orig();
            UpdateWorldPostfix();
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
