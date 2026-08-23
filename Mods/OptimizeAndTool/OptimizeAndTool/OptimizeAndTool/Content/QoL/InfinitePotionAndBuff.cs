using CommandHelp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 无尽药水续杯、随身增益站与怪物旗帜补丁
    /// 当背包或便携收纳中药水堆叠达到阈值（默认 30）时常驻提供对应增益；
    /// 携带篝火、心灯、巴斯特雕像、磨刀石、水晶球等增益站或怪物旗帜时常驻赋予增益；
    /// 支持在绘制 Buff 栏时隐藏无尽 Buff 图标。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class InfinitePotionAndBuff
    {
        public static GetSetReset<bool> EnableInfinitePotions = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> PotionThreshold = new GetSetReset<int>(30, 30);
        public static GetSetReset<bool> EnableBuffStations = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableMonsterBanners = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> HideEndlessBuffs = new GetSetReset<bool>(false, false);

        /// <summary>
        /// 当前激活的无尽增益 ID 集合（用于隐藏 Buff 栏图标过滤）
        /// </summary>
        public static HashSet<int> ActiveInfiniteBuffs { get; private set; } = new HashSet<int>();

        // 静态集合复用，避免每帧分配产生的 GC 压力
        private static readonly Dictionary<int, int> potionCounts = new Dictionary<int, int>(64);
        private static readonly int[] foodCounts = new int[4]; // 1: WellFed(26), 2: WellFed2(206), 3: WellFed3(207)

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("infinitePotions", EnableInfinitePotions),
                CommandBuild.get1("potionThreshold", EnableInfinitePotions, PotionThreshold, new CommandInt()),
                CommandBuild.get2("buffStations", EnableBuffStations),
                CommandBuild.get2("monsterBanners", EnableMonsterBanners),
                CommandBuild.get2("hideEndlessBuffs", HideEndlessBuffs)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get1(EnableInfinitePotions, PotionThreshold, int.Parse, "药水堆叠达到该数量时自动获得无尽续杯增益", "Images/Item_289", "无尽药水续杯"),
                UIBuild.get2(EnableBuffStations, "随身携带篝火、心灯、巴斯特雕像、磨刀石等增益家具直接生效", "Images/Item_215", "随身增益站"),
                UIBuild.get2(EnableMonsterBanners, "随身携带怪物旗帜直接获得对应怪物的旗帜增益效果", "Images/Item_1683", "随身怪物旗帜"),
                UIBuild.get2(HideEndlessBuffs, "在屏幕左上方 Buff 栏中隐藏由无尽药水和增益站提供的常驻 Buff 图标", "Images/Item_50", "隐藏无尽Buff图标")
            };
        }

        /// <summary>
        /// 每帧在玩家更新增益时扫描背包与便携收纳并赋予增益（高性能无 GC 分配）
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateBuffs))]
        [HarmonyPrefix]
        public static void UpdateBuffsPrefix(Player __instance)
        {
            if (__instance == null || !__instance.active) return;
            if (__instance.whoAmI != Main.myPlayer) return;

            ActiveInfiniteBuffs.Clear();
            potionCounts.Clear();
            foodCounts[1] = 0;
            foodCounts[2] = 0;
            foodCounts[3] = 0;

            ScanContainerItems(__instance, __instance.inventory);
            if (__instance.bank?.item != null) ScanContainerItems(__instance, __instance.bank.item);
            if (__instance.bank2?.item != null) ScanContainerItems(__instance, __instance.bank2.item);
            if (__instance.bank3?.item != null) ScanContainerItems(__instance, __instance.bank3.item);
            if (__instance.bank4?.item != null) ScanContainerItems(__instance, __instance.bank4.item);

            // 应用达标药水增益
            int threshold = PotionThreshold.val;
            if (threshold <= 0) threshold = 30;

            if (EnableInfinitePotions.val)
            {
                // 食物跨槽堆叠合并判断（从高阶向低阶判定）
                if (foodCounts[3] >= threshold)
                {
                    __instance.AddBuff(BuffID.WellFed3, 2, false);
                    ActiveInfiniteBuffs.Add(BuffID.WellFed3);
                }
                else if (foodCounts[2] >= threshold)
                {
                    __instance.AddBuff(BuffID.WellFed2, 2, false);
                    ActiveInfiniteBuffs.Add(BuffID.WellFed2);
                }
                else if (foodCounts[1] >= threshold)
                {
                    __instance.AddBuff(BuffID.WellFed, 2, false);
                    ActiveInfiniteBuffs.Add(BuffID.WellFed);
                }

                foreach (KeyValuePair<int, int> kvp in potionCounts)
                {
                    if (kvp.Value >= threshold)
                    {
                        __instance.AddBuff(kvp.Key, 2, false);
                        ActiveInfiniteBuffs.Add(kvp.Key);
                    }
                }
            }
        }

        private static void ScanContainerItems(Player player, Item[] items)
        {
            if (items == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                Item item = items[i];
                if (item == null || item.IsAir || item.stack <= 0) continue;

                // 1. 无尽药水扫描
                if (EnableInfinitePotions.val && item.buffType > 0 && item.buffTime > 0 && item.consumable)
                {
                    if (!Main.debuff[item.buffType] && !Main.lightPet[item.buffType] && !Main.vanityPet[item.buffType])
                    {
                        // 食物跨槽累计
                        if (item.buffType == BuffID.WellFed)
                        {
                            foodCounts[1] += item.stack;
                        }
                        else if (item.buffType == BuffID.WellFed2)
                        {
                            foodCounts[2] += item.stack;
                        }
                        else if (item.buffType == BuffID.WellFed3)
                        {
                            foodCounts[3] += item.stack;
                        }
                        else
                        {
                            if (potionCounts.TryGetValue(item.buffType, out int curCount))
                                potionCounts[item.buffType] = curCount + item.stack;
                            else
                                potionCounts[item.buffType] = item.stack;
                        }
                    }
                }

                // 2. 随身增益站扫描
                if (EnableBuffStations.val)
                {
                    ApplyStationBuffs(player, item);
                }

                // 3. 随身怪物旗帜扫描
                if (EnableMonsterBanners.val && item.createTile == TileID.Banners && item.placeStyle >= 0)
                {
                    if (Main.SceneMetrics?.NPCBannerBuff != null && item.placeStyle < Main.SceneMetrics.NPCBannerBuff.Length)
                    {
                        Main.SceneMetrics.NPCBannerBuff[item.placeStyle] = true;
                        Main.SceneMetrics.hasBanner = true;
                    }
                }
            }
        }

        private static void ApplyStationBuffs(Player player, Item item)
        {
            // 篝火 (Tile 215)
            if (item.createTile == TileID.Campfire || item.type == ItemID.Campfire)
            {
                player.AddBuff(BuffID.Campfire, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.Campfire);
            }
            // 心形灯笼 (Tile 42 / Item 1263)
            else if (item.type == ItemID.HeartLantern || (item.createTile == TileID.HangingLanterns && item.placeStyle == 1))
            {
                player.AddBuff(BuffID.HeartLamp, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.HeartLamp);
            }
            // 星星瓶 (Tile 42 / Item 159)
            else if (item.type == ItemID.StarinaBottle)
            {
                player.AddBuff(BuffID.StarInBottle, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.StarInBottle);
            }
            // 巴斯特雕像 (Tile 506 / Item 4274)
            else if (item.type == ItemID.CatBast || item.createTile == TileID.CatBast)
            {
                player.AddBuff(BuffID.CatBast, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.CatBast);
            }
            // 磨刀石 (Tile 377 / Item 3198)
            else if (item.type == ItemID.SharpeningStation || item.createTile == TileID.SharpeningStation)
            {
                player.AddBuff(BuffID.Sharpened, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.Sharpened);
            }
            // 水晶球 (Tile 125 / Item 487)
            else if (item.type == ItemID.CrystalBall || item.createTile == TileID.CrystalBall)
            {
                player.AddBuff(BuffID.Clairvoyance, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.Clairvoyance);
            }
            // 弹药箱 (Tile 237 / Item 2177)
            else if (item.type == ItemID.AmmoBox || item.createTile == TileID.AmmoBox)
            {
                player.AddBuff(BuffID.AmmoBox, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.AmmoBox);
            }
            // 施法桌 (Tile 349 / Item 2997)
            else if (item.type == ItemID.BewitchingTable || item.createTile == TileID.BewitchingTable)
            {
                player.AddBuff(BuffID.Bewitched, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.Bewitched);
            }
            // 蛋糕块 (Tile 491 / Item 4076)
            else if (item.type == ItemID.SliceOfCake || item.createTile == TileID.SliceOfCake)
            {
                player.AddBuff(BuffID.SugarRush, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.SugarRush);
            }
            // 战争桌旗帜 (Tile 464 / Item 3817)
            else if (item.type == ItemID.WarTableBanner || item.type == ItemID.WarTable || item.createTile == TileID.WarTable)
            {
                player.AddBuff(BuffID.WarTable, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.WarTable);
            }
            // 和平蜡烛 (Tile 409 / Item 3116)
            else if (item.type == ItemID.PeaceCandle || item.createTile == TileID.PeaceCandle)
            {
                player.AddBuff(BuffID.PeaceCandle, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.PeaceCandle);
            }
            // 水蜡烛 (Tile 49 / Item 149)
            else if (item.type == ItemID.WaterCandle || item.createTile == TileID.WaterCandle)
            {
                player.AddBuff(BuffID.WaterCandle, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.WaterCandle);
            }
            // 暗影蜡烛 (Tile 653 / Item 5343)
            else if (item.type == ItemID.ShadowCandle || item.createTile == TileID.ShadowCandle)
            {
                player.AddBuff(BuffID.ShadowCandle, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.ShadowCandle);
            }
            // 向日葵 (Tile 27 / Item 63)
            else if (item.type == ItemID.Sunflower || item.createTile == TileID.Sunflower)
            {
                player.AddBuff(BuffID.Sunflower, 2, false);
                ActiveInfiniteBuffs.Add(BuffID.Sunflower);
            }
            // 花园侏儒 (Tile 567 / Item 4389)
            else if (item.type == ItemID.GardenGnome || item.createTile == TileID.GardenGnome)
            {
                player.HasGardenGnomeNearby = true;
            }
        }

        /// <summary>
        /// 绘制 Buff 栏图标时若开启隐藏无尽 Buff，则拦截对应图标的绘制
        /// </summary>
        [HarmonyPatch(typeof(Main), nameof(Main.DrawBuffIcon))]
        [HarmonyPrefix]
        public static bool DrawBuffIconPrefix(int drawBuffText, int buffSlotOnPlayer, int x, int y)
        {
            if (!HideEndlessBuffs.val) return true;

            Player player = Main.LocalPlayer;
            if (player == null || buffSlotOnPlayer < 0 || buffSlotOnPlayer >= player.buffType.Length) return true;

            int buffType = player.buffType[buffSlotOnPlayer];
            if (buffType > 0 && ActiveInfiniteBuffs.Contains(buffType))
            {
                return false; // 拦截绘制
            }

            return true;
        }
    }
}
