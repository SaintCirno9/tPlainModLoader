using System;
using tContentPatch;
using Terraria;
using Terraria.Localization;

namespace SuspiciousPlayer.Patch
{
    internal class PatchPlayer : Mod
    {
        public static Func<Player, bool> OnCanDropTombstone = null;

        // M2：弃用 IAddPatch，改用 MonoMod.HookGen 的 On_ 门面（tML 标准做法）
        public override void Load()
        {
            On_Player.DropTombstone += (orig, self, coinsOwned, deathText, hitDirection) =>
            {
                if (!(OnCanDropTombstone?.Invoke(self) ?? true)) return;
                orig(self, coinsOwned, deathText, hitDirection);
            };
        }

        public static bool CanDropTombstone(Player __instance, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            return OnCanDropTombstone?.Invoke(__instance) ?? true;
        }
    }
}
