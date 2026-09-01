using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace TPML.Content
{
    /// <summary>
    /// 对齐 tML 的 NPC 便捷方法（仅方法调用语法）。
    /// 作者: SaintCirno9
    /// </summary>
    public static class NPCExtensions
    {
        private static readonly ConditionalWeakTable<NPC, ConcurrentDictionary<Type, object>> _globalInstances = new ConditionalWeakTable<NPC, ConcurrentDictionary<Type, object>>();

        /// <summary>
        /// 对齐 tML <c>NPC.HasBuff(int)</c>：当前是否拥有指定 buff。
        /// </summary>
        public static bool HasBuff(this NPC npc, int type)
        {
            if (npc == null) return false;
            return npc.FindBuffIndex(type) != -1;
        }

        /// <summary>
        /// 对齐 tML <c>NPC.GetGlobalNPC&lt;T&gt;()</c>：获取挂载在 NPC 上的 GlobalNPC 实例。
        /// </summary>
        public static T GetGlobalNPC<T>(this NPC npc) where T : class, new()
        {
            if (npc == null) return null;
            var dict = _globalInstances.GetOrCreateValue(npc);
            return (T)dict.GetOrAdd(typeof(T), _ => new T());
        }

        /// <summary>
        /// 对齐 tML <c>NPC.GetSource_Loot()</c>
        /// </summary>
        public static IEntitySource GetSource_Loot(this NPC npc)
        {
            return new EntitySource_Misc("Loot");
        }

        /// <summary>
        /// 对齐 tML <c>NPC.GetSource_Death()</c>
        /// </summary>
        public static IEntitySource GetSource_Death(this NPC npc)
        {
            return new EntitySource_Misc("Death");
        }

        /// <summary>
        /// 对齐 tML <c>NPC.DropItemInstanced()</c>
        /// </summary>
        public static void DropItemInstanced(this NPC npc, Vector2 position, Vector2 size, int itemType, int itemStack = 1, bool interactionRequired = true)
        {
#pragma warning disable CS0618
            Item.NewItem(new EntitySource_Misc("DropItemInstanced"), (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, itemType, itemStack);
#pragma warning restore CS0618
        }

        /// <summary>
        /// 对齐 tML <c>NPC.DropItemInstanced()</c> 矩形重载
        /// </summary>
        public static void DropItemInstanced(this NPC npc, Rectangle rect, int itemType, int itemStack = 1, bool interactionRequired = true)
        {
#pragma warning disable CS0618
            Item.NewItem(new EntitySource_Misc("DropItemInstanced"), rect.X, rect.Y, rect.Width, rect.Height, itemType, itemStack);
#pragma warning restore CS0618
        }

        /// <summary>
        /// 对齐 tML <c>NPC.Happiness</c> 的方法形式
        /// </summary>
        public static NPCHappiness Happiness(this NPC npc) => NPCHappiness.Get(npc?.type ?? 0);

        /// <summary>
        /// 对齐 tML <c>NPC.ModNPC</c> 的方法形式
        /// </summary>
        public static ModNPC ModNPC(this NPC npc) => npc.GetModNPC();

        /// <summary>
        /// 获取绑定在此 NPC 实例上的 ModNPC
        /// </summary>
        public static ModNPC GetModNPC(this NPC npc) => NPCLoader.GetModNPC(npc);

        /// <summary>
        /// 获取绑定在此 NPC 实例上的泛型 ModNPC
        /// </summary>
        public static T GetModNPC<T>(this NPC npc) where T : ModNPC => NPCLoader.GetModNPC<T>(npc);

        /// <summary>
        /// 对齐 tML <c>NPC.GetSource_FromThis()</c>
        /// </summary>
        public static IEntitySource GetSource_FromThis(this NPC npc, string context = null)
        {
            return new EntitySource_Misc(context ?? "NPC");
        }

        /// <summary>
        /// 对齐 tML <c>NPC.CloneDefaults</c>
        /// </summary>
        public static void CloneDefaults(this NPC npc, int typeToClone)
        {
            if (npc == null) return;
            int originalType = npc.type;
            npc.SetDefaults(typeToClone);
            npc.type = originalType;
        }

        /// <summary>
        /// 对齐 tML <c>NPC.StrikeInstantKill()</c>
        /// </summary>
        public static void StrikeInstantKill(this NPC npc)
        {
            if (npc != null && npc.active)
            {
                npc.StrikeNPC(npc.lifeMax * 10, 0f, 0, true, true);
            }
        }

        /// <summary>
        /// 对齐 tML <c>NPC.AddDebuffImmunities</c>
        /// </summary>
        public static void AddDebuffImmunities(this NPC npc, List<int> debuffs)
        {
            // 原版暂无通用免疫列表，直接记录或空实现
        }

        /// <summary>
        /// 对齐 tML <c>NPC.SimpleStrikeNPC()</c>
        /// </summary>
        public static double SimpleStrikeNPC(this NPC npc, int damage, int hitDirection, bool crit = false, float knockBack = 0f, DamageClass damageClass = null, bool quiet = false, int cooldownCounter = 0, bool noPlayerDamage = false)
        {
            if (npc == null) return 0;
            return npc.StrikeNPC(damage, knockBack, hitDirection, crit, quiet, 0);
        }

        public static string getNewNPCName(this NPC npc) => npc != null ? NPC.getNewNPCName(npc.type) : string.Empty;
        public static bool netUpdate2(this NPC npc) => npc?.netUpdate ?? false;
        public static void netUpdate2(this NPC npc, bool val) { if (npc != null) npc.netUpdate = val; }
    }
}
