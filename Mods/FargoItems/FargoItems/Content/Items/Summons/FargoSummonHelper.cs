using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace FargoItems.Content.Items.Summons
{
    /// <summary>
    /// FargoItems 便携 Boss 召唤统一调度类
    /// 解决原版 NPC.SpawnOnPlayer 对钓鱼浮漂（猪鲨）、蜥蜴祭坛（石巨人）的严苛限制，并支持安全坐标生成与广播
    /// </summary>
    public static class FargoSummonHelper
    {
        public const int TwinsPseudoId = -1;

        /// <summary>
        /// 召唤通用 Boss
        /// </summary>
        public static bool SummonBoss(Player player, int bossType, Vector2? customSpawnPos = null)
        {
            if (player == null || player.whoAmI != Main.myPlayer)
                return false;

            SoundEngine.PlaySound(SoundID.Roar, player.position);
            Vector2 spawnPos = customSpawnPos ?? GetDefaultBossSpawnPos(player, bossType);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnBossInternal(player, bossType, spawnPos);
            }
            else
            {
                SendSummonPacket(player.whoAmI, bossType, spawnPos);
            }

            return true;
        }

        /// <summary>
        /// 召唤双子魔眼（激光眼 + 魔焰眼）
        /// </summary>
        public static bool SummonTwins(Player player)
        {
            if (player == null || player.whoAmI != Main.myPlayer)
                return false;

            SoundEngine.PlaySound(SoundID.Roar, player.position);

            Vector2 leftPos = player.Center + new Vector2(-400f, -400f);
            Vector2 rightPos = player.Center + new Vector2(400f, -400f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnBossInternal(player, NPCID.Retinazer, leftPos, broadcast: false);
                SpawnBossInternal(player, NPCID.Spazmatism, rightPos, broadcast: false);
                BroadcastBossAwoken("LegacyMisc.48");
            }
            else
            {
                SendSummonPacket(player.whoAmI, TwinsPseudoId, player.Center);
            }

            return true;
        }

        public static Vector2 GetDefaultBossSpawnPos(Player player, int bossType)
        {
            if (bossType == NPCID.DukeFishron)
            {
                // 猪鲨：玩家左/右侧上方突进
                float xOffset = (Main.rand != null && Main.rand.NextBool()) ? -650f : 650f;
                return player.Center + new Vector2(xOffset, -200f);
            }

            if (bossType == NPCID.Golem)
            {
                // 石巨人：自玩家头顶向上探测安全空位
                Vector2 pos = player.Center;
                for (int i = 0; i < 30; i++)
                {
                    pos.Y -= 16;
                    if (pos.Y <= 0 || WorldGen.SolidTile((int)pos.X / 16, (int)pos.Y / 16))
                    {
                        pos.Y += 16;
                        break;
                    }
                }
                return pos;
            }

            if (bossType == NPCID.MoonLordCore)
            {
                return player.Center + new Vector2(0f, -150f);
            }

            // 默认偏上方空位
            float dirX = (Main.rand != null && Main.rand.NextBool()) ? -1f : 1f;
            float randOffset = Main.rand != null ? Main.rand.Next(400, 700) : 500f;
            float randHeight = Main.rand != null ? Main.rand.Next(300, 600) : 400f;
            return player.Center + new Vector2(dirX * randOffset, -randHeight);
        }

        public static void SpawnBossInternal(Player player, int bossType, Vector2 spawnPos, bool broadcast = true)
        {
            int npcIndex = NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)spawnPos.X, (int)spawnPos.Y, bossType);
            if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
            {
                Main.npc[npcIndex].target = player.whoAmI;
                Main.npc[npcIndex].targetSetFrame = Main.EverLastingTicker;
                Main.npc[npcIndex].netUpdate = true;

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: npcIndex);
                }

                if (broadcast)
                {
                    string bossName = Lang.GetNPCNameValue(bossType);
                    BroadcastBossAwokenKey("Announcement.HasAwoken", bossName);
                }
            }
        }

        public static void BroadcastBossAwoken(string textKey)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey(textKey), new Color(175, 75, 255));
            }
            else
            {
                Main.NewText(Language.GetTextValue(textKey), 175, 75, 255);
            }
        }

        public static void BroadcastBossAwokenKey(string formatKey, string arg)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey(formatKey, NetworkText.FromLiteral(arg)), new Color(175, 75, 255));
            }
            else
            {
                Main.NewText(Language.GetTextValue(formatKey, arg), 175, 75, 255);
            }
        }

        private static void SendSummonPacket(int playerIndex, int bossType, Vector2 spawnPos)
        {
            var mod = ModContent.GetInstance<FargoItemsMod>();
            if (mod == null) return;

            ModPacket packet = mod.GetPacket();
            packet.Write(FargoItemsMod.PacketId_SummonBoss);
            packet.Write((byte)playerIndex);
            packet.Write((short)bossType);
            packet.Write((int)spawnPos.X);
            packet.Write((int)spawnPos.Y);
            packet.Send();
        }
    }
}
