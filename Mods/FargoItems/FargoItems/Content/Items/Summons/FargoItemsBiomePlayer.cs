using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Summons
{
    /// <summary>
    /// 当 Fargo 便携召唤物召唤的 Boss 在场时，维持玩家虚拟生物群落状态，防止脱离原生生物群落而暴怒或逃跑
    /// </summary>
    public class FargoItemsBiomePlayer : ModPlayer
    {
        public override void PostUpdateMiscEffects()
        {
            if (Player == null || !Player.active || Player.dead)
                return;

            const float bossCheckDistanceSq = 3500f * 3500f;
            Vector2 playerCenter = Player.Center;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active)
                    continue;

                if (Vector2.DistanceSquared(playerCenter, npc.Center) > bossCheckDistanceSq)
                    continue;

                switch (npc.type)
                {
                    case NPCID.EaterofWorldsHead:
                    case NPCID.EaterofWorldsBody:
                    case NPCID.EaterofWorldsTail:
                        Player.ZoneCorrupt = true;
                        break;

                    case NPCID.BrainofCthulhu:
                    case NPCID.Creeper:
                        Player.ZoneCrimson = true;
                        break;

                    case NPCID.QueenBee:
                        Player.ZoneJungle = true;
                        break;

                    case NPCID.Deerclops:
                        Player.ZoneSnow = true;
                        break;
                }
            }
        }
    }
}
