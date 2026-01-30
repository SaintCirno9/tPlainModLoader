using System;

namespace SuspiciousPlayer.Patch
{
    internal class PatchNetMessage : tContentPatch.PatchNetMessage
    {
        public static Action<int> OnSyncConnectedPlayer = null;

        public override void SyncConnectedPlayerPrefix(int plr)
        {
            OnSyncConnectedPlayer?.Invoke(plr);
        }
    }
}
