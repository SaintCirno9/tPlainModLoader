using System.Collections.Generic;
using Terraria;
using Terraria.IO;
using Terraria.Localization;

namespace tContentPatch.ModPatch
{
    /// <summary>
    /// Player 生命周期补丁列表持有类（已收敛至 PlayerLoader 统一分发）
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_Player : ListCopy<PatchPlayer>
    {
        private static readonly List<PatchPlayer> mod = new List<PatchPlayer>();
        internal static List<PatchPlayer> ModList => mod;

        public Patch_Player() : base(mod) { }

        public static void UpdatePrefix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdatePrefix(__instance, i));
        }

        public static void UpdatePostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdatePostfix(__instance, i));
        }

        public static void UpdateEquipsPrefix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdateEquipsPrefix(__instance, i));
        }

        public static void UpdateEquipsPostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdateEquipsPostfix(__instance, i));
        }

        public static void UpdateArmorSetsPostfix(Player __instance, int i)
        {
            mod.ForTry(item => item.UpdateArmorSetsPostfix(__instance, i));
        }

        public static void SavePlayerPrefix(PlayerFileData playerFile, bool skipMapSave)
        {
            mod.ForTry(item => item.SavePlayerPrefix(playerFile, skipMapSave));
        }

        public static void SavePlayerPostfix(PlayerFileData playerFile, bool skipMapSave)
        {
            mod.ForTry(item => item.SavePlayerPostfix(playerFile, skipMapSave));
        }

        public static void LoadPlayerPostfix(PlayerFileData playerFileData)
        {
            mod.ForTry(item => item.LoadPlayerPostfix(playerFileData));
        }

        public static void SetAsActivePostfix(PlayerFileData playerFileData)
        {
            mod.ForTry(item => item.SetAsActivePostfix(playerFileData));
        }

        public static bool CanDropTombstone(Player __instance, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            return mod.ForTry(item => item.CanDropTombstone(__instance, coinsOwned, deathText, hitDirection));
        }
    }
}
