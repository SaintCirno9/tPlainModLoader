using System;
using Terraria;
using Terraria.DataStructures;
using TPML.Content;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的 NPC Patch 基类。请迁移至 <see cref="TPML.Content.GlobalNPC"/> 或 <see cref="TPML.Content.ModNPC"/>。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
    public abstract class PatchNPC : TPML.Content.GlobalNPC
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
        /// <summary/>
        public virtual void SetDefaultsPrefix(NPC This, int Type, NPCSpawnParams spawnparams) { }
        /// <summary/>
        public virtual void SetDefaultsPostfix(NPC This, int Type, NPCSpawnParams spawnparams) { }
        /// <summary/>
        public virtual void NewNPCPostfix(int __result, IEntitySource source,
            int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target)
        { }
    }
}
