using Terraria;
using Terraria.DataStructures;

namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchNPC
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// <see cref="NPC.UpdateNPC(int)"/>前调用
        /// </summary>
        public virtual void UpdateNPCPrefix(NPC This, int i) { }
        /// <summary>
        /// <see cref="NPC.UpdateNPC(int)"/>后调用
        /// </summary>
        public virtual void UpdateNPCPostfix(NPC This, int i) { }
        /// <summary>
        /// <see cref="NPC.NewNPC(IEntitySource, int, int, int, int, float, float, float, float, int)"/>后调用
        /// </summary>
        public virtual void NewNPCPostfix(int __result, IEntitySource source,
            int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        { }
    }
}
