using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;

namespace TPML.Content
{
    /// <summary>
    /// TPML 全局 NPC 行为修饰基类
    /// </summary>
    public abstract class GlobalNPC : ModType
    {
        public virtual bool InstancePerEntity => false;

        public virtual void SetDefaults(NPC npc)
        {
        }

        public virtual bool PreAI(NPC npc)
        {
            return true;
        }

        public virtual void AI(NPC npc)
        {
        }

        public virtual void PostAI(NPC npc)
        {
        }

        public virtual bool CanHitNPC(NPC npc, NPC target)
        {
            return true;
        }

        public virtual void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
        }

        public virtual void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
        }

        public virtual bool PreKill(NPC npc)
        {
            return true;
        }

        public virtual void OnKill(NPC npc)
        {
        }

        public virtual bool CheckDead(NPC npc)
        {
            return true;
        }

        public virtual void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
        }

        public virtual void ModifyShop(NPCShop shop)
        {
        }

        public virtual void SetupShop(int type, Chest shop, ref int nextSlot)
        {
        }

        public virtual void OnChatButtonClicked(NPC npc, bool firstButton)
        {
        }

        public virtual void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
        }

        public virtual void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
        }

        public virtual void ModifyHitNPC(NPC npc, NPC target, ref int damage, ref float knockback, ref bool crit)
        {
        }
    }
}
