using Microsoft.Xna.Framework;
using System;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 脱战 1.5s 极速复活 & 复活/进世界自动重新召唤仆从与哨兵
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_RespawnAndMinion : TPML.Content.ModPlayer
    {
        private static bool _wasDead = false;

        public override void Initialize()
        {
            _wasDead = false;
        }

        public override void SetAsActivePostfix(Terraria.IO.PlayerFileData playerFile)
        {
            if (playerFile?.Player != null)
            {
                MinionMemoryTracker.LoadForPlayer(playerFile.Player);
            }
        }

        public override void SavePlayerPrefix(Terraria.IO.PlayerFileData playerFile, bool skipMapSave)
        {
            if (playerFile?.Player != null)
            {
                MinionMemoryTracker.SaveForPlayer(playerFile.Player, force: true);
            }
        }

        public static bool AnyBossAlive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active) continue;

                if (npc.boss ||
                    npc.type == NPCID.EaterofWorldsHead ||
                    npc.type == NPCID.EaterofWorldsBody ||
                    npc.type == NPCID.EaterofWorldsTail ||
                    npc.type == NPCID.TheDestroyer ||
                    npc.type == NPCID.TheDestroyerBody ||
                    npc.type == NPCID.TheDestroyerTail ||
                    npc.type == NPCID.WallofFlesh ||
                    npc.type == NPCID.WallofFleshEye ||
                    npc.type == NPCID.MoonLordCore ||
                    npc.type == NPCID.MoonLordHead ||
                    npc.type == NPCID.MoonLordHand ||
                    npc.type == NPCID.Golem ||
                    npc.type == NPCID.GolemHead ||
                    npc.type == NPCID.GolemFistLeft ||
                    npc.type == NPCID.GolemFistRight)
                {
                    return true;
                }
            }
            return false;
        }

        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer) return;

            // 1. 死亡状态：脱战 1.5s (90帧) 快速复活
            if (This.dead)
            {
                if (QoLValSet.quickRespawn.val)
                {
                    int targetFrames = Math.Max(1, QoLValSet.quickRespawnFrames.val);
                    if (This.respawnTimer > targetFrames && !AnyBossAlive())
                    {
                        This.respawnTimer = targetFrames;
                    }
                }
            }
        }

        public override void UpdatePostfix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer) return;

            // 在 UpdatePostfix 中执行：装备、饰品、套装及作弊修改已完全结算，This.maxMinions 为真实全量上限
            if (!This.dead)
            {
                // 刚复活瞬间：触发重新召唤
                if (_wasDead)
                {
                    MinionMemoryTracker.OnRespawn(This);
                }

                // 存活期间：持续同步活跃仆从并感知进世界就绪后的恢复召唤
                MinionMemoryTracker.Update(This);
            }

            _wasDead = This.dead;
        }

        /// <summary>
        /// 进世界生命周期监听
        /// </summary>
        internal class Patch_RespawnAndMinion_Main : TPML.Content.ModSystem
        {
            public override void OnEnterWorld()
            {
                MinionMemoryTracker.OnEnterWorld();
            }
        }
    }
}
