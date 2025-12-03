using Terraria;

namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchPlayer
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// <see cref="Player.Update(int)"/>前调用
        /// </summary>
        public virtual void UpdatePrefix(Player This, int playerI) { }
        /// <summary>
        /// <see cref="Player.Update(int)"/>后调用
        /// </summary>
        public virtual void UpdatePostfix(Player This, int playerI) { }
        /// <summary/>
        public virtual void UpdateArmorSetsPostfix(Player This, int playerI) { }
    }
}
