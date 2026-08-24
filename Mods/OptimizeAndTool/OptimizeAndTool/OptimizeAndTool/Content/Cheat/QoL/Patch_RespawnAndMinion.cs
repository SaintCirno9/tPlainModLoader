using Microsoft.Xna.Framework;
using System;
using tContentPatch;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 脱战 1.5s 极速复活 & 复活自动召唤仆从
    /// 作者: SaintCirno9
    /// </summary>
    internal class Patch_RespawnAndMinion : PatchPlayer
    {
        public static int LastMinionItemId = 0;
        private static bool _wasDead = false;

        public override void Initialize()
        {
            LastMinionItemId = 0;
            _wasDead = false;
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

            // 1. 存活期间追踪仆从召唤杖
            if (!This.dead)
            {
                // 检查手持物品
                if (This.itemAnimation > 0 && This.HeldItem != null && This.HeldItem.type > ItemID.None)
                {
                    Item held = This.HeldItem;
                    if (IsMinionSummonItem(held))
                    {
                        LastMinionItemId = held.type;
                    }
                }
                // 或根据当前生效的仆从 Buff 逆向推导最近使用的召唤杖
                else if (LastMinionItemId == 0)
                {
                    LastMinionItemId = FindSummonItemFromBuffs(This);
                }
            }

            // 2. 死亡状态：脱战 1.5s (90帧) 快速复活
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

            // 3. 刚复活瞬间：自动召唤仆从
            if (_wasDead && !This.dead)
            {
                if (QoLValSet.autoResummonMinions.val && LastMinionItemId > 0)
                {
                    TryResummonMinions(This, LastMinionItemId);
                }
            }

            _wasDead = This.dead;
        }

        private static bool IsMinionSummonItem(Item item)
        {
            if (item == null || item.type <= ItemID.None || item.shoot <= ProjectileID.None) return false;

            // 区分随从与固定炮台哨兵
            if (item.summon && !item.sentry)
            {
                // 排除鞭子（鞭子没有宠物 Buff）
                if (item.buffType > 0 && !Main.vanityPet[item.buffType] && !Main.lightPet[item.buffType])
                {
                    return true;
                }
            }
            else if (item.buffType > 0 && !item.sentry && !Main.vanityPet[item.buffType] && !Main.lightPet[item.buffType])
            {
                return true;
            }
            return false;
        }

        private static int FindSummonItemFromBuffs(Player player)
        {
            for (int b = 0; b < player.buffType.Length; b++)
            {
                int buff = player.buffType[b];
                if (buff <= 0) continue;
                if (Main.vanityPet[buff] || Main.lightPet[buff]) continue;

                // 在玩家背包中寻找对应 buffType 的召唤杖（排除哨兵）
                for (int i = 0; i < 50; i++)
                {
                    Item item = player.inventory[i];
                    if (item != null && item.type > ItemID.None && item.buffType == buff && item.shoot > 0 && !item.sentry)
                    {
                        return item.type;
                    }
                }
            }
            return 0;
        }

        private static void TryResummonMinions(Player player, int itemId)
        {
            Item summonItem = null;
            for (int i = 0; i < 58; i++)
            {
                if (player.inventory[i] != null && player.inventory[i].type == itemId)
                {
                    summonItem = player.inventory[i];
                    break;
                }
            }

            if (summonItem == null || summonItem.shoot <= ProjectileID.None) return;

            // 赋予仆从 Buff
            if (summonItem.buffType > 0)
            {
                player.AddBuff(summonItem.buffType, 36000);
            }

            // 召唤仆从弹幕至仆从上限（哨兵只召1个，仆从拉满）
            int shootProj = summonItem.shoot;
            int damage = player.GetWeaponDamage(summonItem);
            float knockBack = player.GetWeaponKnockback(summonItem, summonItem.knockBack);
            int countToSummon = summonItem.sentry ? 1 : Math.Max(1, player.maxMinions);

            for (int k = 0; k < countToSummon; k++)
            {
                float randX = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                float randY = (float)(Main.rand.NextDouble() * 40.0 - 20.0);
                Vector2 spawnPos = player.Center + new Vector2(randX, randY);
                Projectile.NewProjectile(player.GetProjectileSource_Item(summonItem), spawnPos.X, spawnPos.Y, 0f, -2f, shootProj, damage, knockBack, player.whoAmI);
            }

            SoundEngine.PlaySound(SoundID.Item44, player.Center);
        }
    }
}
