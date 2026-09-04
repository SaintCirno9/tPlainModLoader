using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.Fishing
{
    /// <summary>
    /// 钓鱼补给与鱼饵自动化系统门控（基于 HookGen 强类型 On_ 门控）
    /// 包含：无限鱼饵、跨容器寻饵与消耗统计、自动药水续杯、自动投掷鱼饵桶打窝与渔夫时装加成
    /// 作者: SaintCirno9
    /// </summary>
    internal class AutoFishingSuppliesHooks
    {
        public static GetSetReset<bool> EnableInfiniteBait = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableAutoDrinkFishingBuffs = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableAutoChumBuckets = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableAnglerArmorVanityBonus = new GetSetReset<bool>(true, true);

        private static readonly Dictionary<int, int> chumCooldowns = new Dictionary<int, int>();
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.Update += Hook_PlayerUpdate;
            On_Player.Fishing_GetBait += Hook_Fishing_GetBait;
            On_Player.UpdateEquips += Hook_UpdateEquips;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.Update -= Hook_PlayerUpdate;
            On_Player.Fishing_GetBait -= Hook_Fishing_GetBait;
            On_Player.UpdateEquips -= Hook_UpdateEquips;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("infiniteBait", EnableInfiniteBait),
                CommandBuild.get2("autoDrinkFishingBuffs", EnableAutoDrinkFishingBuffs),
                CommandBuild.get2("autoChumBuckets", EnableAutoChumBuckets),
                CommandBuild.get2("anglerArmorVanityBonus", EnableAnglerArmorVanityBonus)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableInfiniteBait, "钓鱼时完全不消耗鱼饵（即便背包无鱼饵也可抛竿垂钓）", "Images/Item_2676", "无限鱼饵"),
                UIBuild.get2(EnableAutoDrinkFishingBuffs, "手持钓竿时自动消耗或维持钓鱼/宝匣/声呐药水效果", "Images/Item_2354", "自动钓鱼药水"),
                UIBuild.get2(EnableAutoChumBuckets, "检测浮标所在水域并自动投掷鱼饵桶打窝（补满 3 桶满级加成）", "Images/Item_4608", "自动打窝"),
                UIBuild.get2(EnableAnglerArmorVanityBonus, "渔夫套装放置在时装栏或背包中时依然提供渔力加成", "Images/Item_2361", "渔夫时装/背包生效")
            };
        }

        #region 1. 跨容器寻饵与消耗

        /// <summary>
        /// 检查玩家全容器是否持有有效鱼饵
        /// </summary>
        public static bool HasBaitAvailable(Player player)
        {
            if (EnableInfiniteBait.val)
                return true;

            return FindBait(player) != null;
        }

        /// <summary>
        /// 统计玩家全容器中可用的鱼饵总堆叠数
        /// </summary>
        public static int CountAllBait(Player player)
        {
            if (EnableInfiniteBait.val)
                return 9999;

            if (player == null || !player.active)
                return 0;

            int count = 0;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && item.bait > 0)
                    count += item.stack;
            }

            if (player.useVoidBag())
            {
                for (int i = 0; i < player.bank4.item.Length; i++)
                {
                    Item item = player.bank4.item[i];
                    if (item != null && item.stack > 0 && item.bait > 0)
                        count += item.stack;
                }
            }

            count += CountBaitInBank(player.bank.item);
            count += CountBaitInBank(player.bank2.item);
            count += CountBaitInBank(player.bank3.item);

            return count;
        }

        private static int CountBaitInBank(Item[] bank)
        {
            if (bank == null) return 0;
            int c = 0;
            for (int i = 0; i < bank.Length; i++)
            {
                Item item = bank[i];
                if (item != null && item.stack > 0 && item.bait > 0)
                    c += item.stack;
            }
            return c;
        }

        /// <summary>
        /// 在背包、虚空袋、猪猪储蓄罐、保险箱、护卫熔炉中寻找鱼饵
        /// </summary>
        public static Item FindBait(Player player)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && item.bait > 0)
                    return item;
            }

            if (player.useVoidBag())
            {
                for (int i = 0; i < player.bank4.item.Length; i++)
                {
                    Item item = player.bank4.item[i];
                    if (item != null && item.stack > 0 && item.bait > 0)
                        return item;
                }
            }

            Item bankBait = FindBaitInBank(player.bank.item);
            if (bankBait != null) return bankBait;
            bankBait = FindBaitInBank(player.bank2.item);
            if (bankBait != null) return bankBait;
            return FindBaitInBank(player.bank3.item);
        }

        private static Item FindBaitInBank(Item[] bank)
        {
            if (bank == null) return null;
            for (int i = 0; i < bank.Length; i++)
            {
                Item item = bank[i];
                if (item != null && item.stack > 0 && item.bait > 0)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// 尝试消耗一个鱼饵（按原版概率，并跨容器扣除）
        /// </summary>
        public static void TryConsumeBait(Player player, int baitType)
        {
            if (EnableInfiniteBait.val)
                return;

            Item bait = FindItemOfType(player, baitType) ?? FindBait(player);
            if (bait == null)
                return;

            float chance = 1f + bait.bait / 6f;
            if (chance < 1f)
                chance = 1f;
            if (player.accTackleBox)
                chance += 1f;

            bool consume = Main.rand.NextFloat() * chance < 1f;
            if (bait.type == ItemID.GoldWorm)
                consume = Main.rand.Next(20) == 0;
            if (bait.type == ItemID.TruffleWorm)
                consume = true;

            if (consume)
            {
                if (bait.type == ItemID.LadyBug || bait.type == ItemID.GoldLadyBug)
                {
                    NPC.LadyBugKilled(player.Center, bait.type == ItemID.GoldLadyBug);
                }
                bait.stack--;
                if (bait.stack <= 0)
                    bait.TurnToAir();
            }
        }

        /// <summary>
        /// 在背包与虚空袋中寻找指定物品
        /// </summary>
        public static Item FindItemInInventoryOrVoidBag(Player player, int type)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && item.type == type)
                    return item;
            }

            if (player.useVoidBag())
            {
                for (int i = 0; i < player.bank4.item.Length; i++)
                {
                    Item item = player.bank4.item[i];
                    if (item != null && item.stack > 0 && item.type == type)
                        return item;
                }
            }

            return null;
        }

        private static Item FindItemOfType(Player player, int type)
        {
            Item item = FindItemInInventoryOrVoidBag(player, type);
            if (item != null)
                return item;

            item = FindItemInBank(player.bank.item, type);
            if (item != null) return item;
            item = FindItemInBank(player.bank2.item, type);
            if (item != null) return item;
            return FindItemInBank(player.bank3.item, type);
        }

        private static Item FindItemInBank(Item[] bank, int type)
        {
            if (bank == null) return null;
            for (int i = 0; i < bank.Length; i++)
            {
                Item item = bank[i];
                if (item != null && item.stack > 0 && item.type == type)
                    return item;
            }
            return null;
        }

        #endregion

        #region 2. 药水、打窝与渔夫套装

        private static void Hook_PlayerUpdate(On_Player.orig_Update orig, Player self, int i)
        {
            orig(self, i);

            if (self.whoAmI != Main.myPlayer || !self.active || self.dead)
                return;

            bool isFishing = HasActiveBobber(self);
            if (EnableAutoDrinkFishingBuffs.val && isFishing)
                MaintainBuffs(self);

            if (EnableAutoChumBuckets.val && isFishing)
                MaintainChum(self);
        }

        /// <summary>
        /// 跨容器寻找鱼饵：原版钓鱼条件只查背包，这里把虚空袋和随身银行也纳入
        /// </summary>
        private static void Hook_Fishing_GetBait(On_Player.orig_Fishing_GetBait orig, Player self, out int baitPower, out int baitType)
        {
            orig(self, out baitPower, out baitType);

            if (!self.active)
                return;

            if (baitPower <= 0 && EnableInfiniteBait.val)
            {
                baitPower = 50;
                baitType = ItemID.MasterBait;
                return;
            }

            if (baitPower > 0)
                return;

            Item bait = FindBait(self);
            if (bait != null)
            {
                baitPower = bait.bait;
                baitType = bait.type;
            }
        }

        /// <summary>
        /// 渔夫套装放在时装栏/背包时也给予对应的渔力加成
        /// </summary>
        private static void Hook_UpdateEquips(On_Player.orig_UpdateEquips orig, Player self, int i)
        {
            orig(self, i);

            if (!self.active || !EnableAnglerArmorVanityBonus.val)
                return;

            for (int k = 0; k < 3; k++)
            {
                int pieceId = k + ItemID.AnglerHat;
                if (self.armor[k].type == pieceId)
                    continue;

                Item candidate = null;
                if (self.armor[k + 10].type == pieceId)
                {
                    candidate = self.armor[k + 10];
                }
                else
                {
                    candidate = FindItemInInventoryOrVoidBag(self, pieceId);
                }

                if (candidate != null)
                {
                    self.GrantArmorBenefits(candidate);
                }
            }
        }

        private static bool HasActiveBobber(Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.bobber && p.wet)
                    return true;
            }
            return false;
        }

        private static void MaintainBuffs(Player player)
        {
            TryUseBuffPotion(player, ItemID.FishingPotion, BuffID.Fishing);
            TryUseBuffPotion(player, ItemID.CratePotion, BuffID.Crate);
            TryUseBuffPotion(player, ItemID.SonarPotion, BuffID.Sonar);
            if (player.FindBuffIndex(BuffID.Tipsy) == -1)
            {
                TryUseBuffPotion(player, ItemID.Sake, BuffID.Tipsy);
                if (player.FindBuffIndex(BuffID.Tipsy) == -1)
                {
                    TryUseBuffPotion(player, ItemID.Ale, BuffID.Tipsy);
                }
            }
        }

        private static void TryUseBuffPotion(Player player, int itemType, int buffId)
        {
            if (player.FindBuffIndex(buffId) != -1)
                return;

            Item potion = FindItemOfType(player, itemType);
            if (potion == null)
                return;

            player.AddBuff(potion.buffType, potion.buffTime);
            potion.stack--;
            if (potion.stack <= 0)
                potion.TurnToAir();
        }

        private static void MaintainChum(Player player)
        {
            if (FindItemOfType(player, ItemID.ChumBucket) == null)
                return;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile bobber = Main.projectile[i];
                if (!bobber.active || bobber.owner != player.whoAmI || !bobber.bobber || !bobber.wet)
                    continue;

                int key = bobber.whoAmI;
                if (chumCooldowns.TryGetValue(key, out int cooldown) && cooldown > 0)
                {
                    chumCooldowns[key] = cooldown - 1;
                    continue;
                }

                int x = (int)(bobber.Center.X / 16f);
                int y = (int)(bobber.Center.Y / 16f);
                Projectile.GetFishingPondState(x, y, out _, out _, out _, out int chumCount);

                int need = 3 - chumCount;
                if (need <= 0)
                {
                    chumCooldowns[key] = 10;
                    continue;
                }

                for (int k = 0; k < need; k++)
                {
                    Item bucket = FindItemOfType(player, ItemID.ChumBucket);
                    if (bucket == null)
                        break;

                    IEntitySource source = player.GetItemSource_Item(bucket);
                    Projectile.NewProjectile(source, bobber.Bottom, Vector2.UnitY * 8f, ProjectileID.ChumBucket, 0, 0f, player.whoAmI);
                    bucket.stack--;
                    if (bucket.stack <= 0)
                        bucket.TurnToAir();
                }

                chumCooldowns[key] = 120;
            }
        }

        /// <summary>
        /// 兼容旧调用：在抛竿时立即维护药水与打窝
        /// </summary>
        public static void MaintainSupplies(Player player)
        {
            if (EnableAutoDrinkFishingBuffs.val)
                MaintainBuffs(player);
        }

        #endregion
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal class AutoFishingSupplies : AutoFishingSuppliesHooks
    {
    }
}