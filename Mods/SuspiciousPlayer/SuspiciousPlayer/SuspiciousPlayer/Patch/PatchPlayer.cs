using System;
using tContentPatch;
using tContentPatch.Patch;
using Terraria;
using Terraria.Localization;

namespace SuspiciousPlayer.Patch
{
    internal class PatchPlayer : Mod
    {
        public static Func<Player, bool> OnCanDropTombstone = null;

        public override void AddPatch(IAddPatch addPatch)
        {
            addPatch.AddPrefix(typeof(Player).GetMethod("DropTombstone"),
                typeof(PatchPlayer).GetMethod("CanDropTombstone"));
        }

        public static bool CanDropTombstone(Player __instance, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            return OnCanDropTombstone?.Invoke(__instance) ?? true;
        }
    }
}
