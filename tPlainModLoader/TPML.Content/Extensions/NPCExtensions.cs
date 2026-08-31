using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// 对齐 tML 的 NPC 便捷方法（仅方法调用语法）。
    /// 作者: SaintCirno9
    /// </summary>
    public static class NPCExtensions
    {
        /// <summary>
        /// 对齐 tML <c>NPC.HasBuff(int)</c>：当前是否拥有指定 buff。
        /// </summary>
        public static bool HasBuff(this NPC npc, int type)
        {
            if (npc == null) return false;
            return npc.FindBuffIndex(type) != -1;
        }
    }
}
