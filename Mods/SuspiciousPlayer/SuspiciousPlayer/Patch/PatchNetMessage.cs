using System;
using Terraria;
using TPML.Content;

namespace SuspiciousPlayer.Patch
{
    internal class PatchNetMessage : ModSystem
    {
        public static Action<int> OnSyncConnectedPlayer = null;

        public override void Load()
        {
            On_NetMessage.SyncConnectedPlayer += Hook_SyncConnectedPlayer;
        }

        public override void Unload()
        {
            On_NetMessage.SyncConnectedPlayer -= Hook_SyncConnectedPlayer;
            OnSyncConnectedPlayer = null;
        }

        private static void Hook_SyncConnectedPlayer(On_NetMessage.orig_SyncConnectedPlayer orig, int plr)
        {
            OnSyncConnectedPlayer?.Invoke(plr);
            orig(plr);
        }
    }
}


