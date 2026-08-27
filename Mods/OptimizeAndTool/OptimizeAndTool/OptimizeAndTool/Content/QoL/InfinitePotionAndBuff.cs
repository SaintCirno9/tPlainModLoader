using CommandHelp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using BigBagMod = OptimizeAndTool.Content.BigBag.BigBag;
using OptimizeAndTool.Content.QoL.InfiniteBuff;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 无尽药水续杯、随身增益站与怪物旗帜补丁
    /// 当背包或便携收纳中药水堆叠达到阈值（默认 30）时常驻提供对应增益；
    /// 携带篝火、心灯、巴斯特雕像、磨刀石、水晶球等增益站或怪物旗帜时常驻赋予增益；
    /// 通过挂载 SceneMetrics.Scan 从根本上解决旗帜与场景光环 Buff 闪烁问题；
    /// 支持无限增益黑名单过滤、收藏置顶管理与在绘制 Buff 栏时隐藏无尽 Buff 图标。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    public static class InfinitePotionAndBuff
    {
        public static GetSetReset<bool> EnableInfinitePotions = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> PotionThreshold = new GetSetReset<int>(30, 30);
        public static GetSetReset<bool> EnableBuffStations = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableMonsterBanners = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> HideEndlessBuffs = new GetSetReset<bool>(false, false);

        /// <summary>
        /// 当前背包达标/随身携带的所有可用无尽增益 ID 集合（供 UI 展示与过滤）
        /// </summary>
        public static HashSet<int> AvailableInfiniteBuffs { get; private set; } = new HashSet<int>();

        /// <summary>
        /// 当前已激活并赋予玩家的无尽增益 ID 集合（排除黑名单后的实际生效增益）
        /// </summary>
        public static HashSet<int> ActiveInfiniteBuffs { get; private set; } = new HashSet<int>();

        // 静态集合复用，避免每帧分配产生的 GC 压力
        private static readonly Dictionary<int, int> potionCounts = new Dictionary<int, int>(64);
        private static readonly int[] foodCounts = new int[4]; // 1: WellFed(26), 2: WellFed2(206), 3: WellFed3(207)
        private static readonly HashSet<int> carriedInteractiveStations = new HashSet<int>();
        private static readonly HashSet<int> carriedSceneStations = new HashSet<int>();
        private static bool carriedMonsterBanner = false;

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

        // 静态映射表：ItemId -> BannerId（O(1) 极速查找，100% 精准对齐原版 BannerSystem）
        private static readonly Dictionary<int, int> itemToBanner = new Dictionary<int, int>(300);

        static InfinitePotionAndBuff()
        {
            InitBannerMapping();
        }

        private static void InitBannerMapping()
        {
            itemToBanner.Clear();
            for (int bannerId = 1; bannerId < BannerSystem.MaxBannerTypes; bannerId++)
            {
                int itemId = BannerSystem.BannerToItem(bannerId);
                if (itemId > 0 && ItemID.Sets.BannerStrength.IndexInRange(itemId) && ItemID.Sets.BannerStrength[itemId].Enabled)
                {
                    itemToBanner[itemId] = bannerId;
                }
            }
        }

        /// <summary>
        /// 将物品精准转换为旗帜 ID（100% 精确对齐原版 BannerSystem）
        /// </summary>
        public static int ItemToBanner(Item item)
        {
            if (item == null || item.IsAir || item.stack <= 0) return -1;

            if (itemToBanner.TryGetValue(item.type, out int bannerId))
            {
                return bannerId;
            }

            return -1;
        }

        /// <summary>
        /// 在场景指标扫描完毕后注入随身旗帜与场景光环，彻底根除 Buff 闪烁问题
        /// </summary>
        [HarmonyPatch(typeof(SceneMetrics), nameof(SceneMetrics.Scan))]
        [HarmonyPostfix]
        public static void SceneMetricsScanPostfix(SceneMetrics __instance)
        {
            if (__instance == null || Main.netMode == 2) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            if (!EnableMonsterBanners.val && !EnableBuffStations.val) return;

            ScanSceneContainerItems(__instance, player.inventory);
            if (player.bank?.item != null) ScanSceneContainerItems(__instance, player.bank.item);
            if (player.bank2?.item != null) ScanSceneContainerItems(__instance, player.bank2.item);
            if (player.bank3?.item != null) ScanSceneContainerItems(__instance, player.bank3.item);
            if (player.bank4?.item != null) ScanSceneContainerItems(__instance, player.bank4.item);

            if (BigBagMod.EnableBigBag.val && BigBagMod.Slots != null)
            {
                ScanSceneContainerItems(__instance, BigBagMod.Slots);
            }
        }

        private static void ScanSceneContainerItems(SceneMetrics metrics, Item[] items)
        {
            if (items == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                Item item = items[i];
                if (item == null || item.IsAir || item.stack <= 0) continue;

                // 递归扫描随身收纳容器内部物品（如旗帜盒）
                if (item.type >= ItemID.Count)
                {
                    var container = ItemLoader.GetModItem(item) as OptimizeAndTool.Content.Storage.ItemContainer.IItemContainer;
                    if (container?.Slots != null)
                    {
                        ScanSceneContainerItems(metrics, container.Slots);
                    }
                }

                // 1. 随身旗帜注入（受黑名单控制）
                if (EnableMonsterBanners.val && !InfiniteBuffStorage.Blacklist.Contains(BuffID.MonsterBanner))
                {
                    int bannerId = ItemToBanner(item);
                    if (bannerId >= 0 && metrics.NPCBannerBuff != null && bannerId < metrics.NPCBannerBuff.Length)
                    {
                        metrics.NPCBannerBuff[bannerId] = true;
                        metrics.hasBanner = true;
                    }
                }

                // 2. 随身场景增益站注入（原生驱动，无闪烁，受黑名单控制）
                if (EnableBuffStations.val)
                {
                    // 篝火 / 壁炉
                    if (item.type == ItemID.Campfire || item.type == ItemID.Fireplace ||
                        item.createTile == TileID.Campfire || item.createTile == TileID.Fireplace)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.Campfire)) metrics.HasCampfire = true;
                    }
                    // 心形灯笼 (Tile 42, Style 9)
                    else if (item.type == ItemID.HeartLantern || (item.createTile == TileID.HangingLanterns && item.placeStyle == 9))
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.HeartLamp)) metrics.HasHeartLantern = true;
                    }
                    // 星星瓶 (Tile 42, Style 7)
                    else if (item.type == ItemID.StarinaBottle || (item.createTile == TileID.HangingLanterns && item.placeStyle == 7))
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.StarInBottle)) metrics.HasStarInBottle = true;
                    }
                    // 巴斯特雕像
                    else if (item.type == ItemID.CatBast || item.createTile == TileID.CatBast)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.CatBast)) metrics.HasCatBast = true;
                    }
                    // 向日葵
                    else if (item.type == ItemID.Sunflower || item.createTile == TileID.Sunflower)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.Sunflower)) metrics.HasSunflower = true;
                    }
                    // 水蜡烛
                    else if (item.type == ItemID.WaterCandle || item.createTile == TileID.WaterCandle)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.WaterCandle)) metrics.ZoneWaterCandle = true;
                    }
                    // 和平蜡烛
                    else if (item.type == ItemID.PeaceCandle || item.createTile == TileID.PeaceCandle)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.PeaceCandle)) metrics.ZonePeaceCandle = true;
                    }
                    // 暗影蜡烛
                    else if (item.type == ItemID.ShadowCandle || item.createTile == TileID.ShadowCandle)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.ShadowCandle)) metrics.ZoneShadowCandle = true;
                    }
                    // 花园侏儒
                    else if (item.type == ItemID.GardenGnome || item.createTile == TileID.GardenGnome)
                    {
                        metrics.HasGardenGnome = true;
                    }
                }
            }
        }

        /// <summary>
        /// 主动扫描并更新玩家背包中的可用无尽增益（UI与帧更新通用）
        /// </summary>
        public static void UpdateAvailableBuffs(Player player)
        {
            if (player == null || !player.active) return;

            AvailableInfiniteBuffs.Clear();
            ActiveInfiniteBuffs.Clear();
            potionCounts.Clear();
            foodCounts[1] = 0;
            foodCounts[2] = 0;
            foodCounts[3] = 0;
            carriedInteractiveStations.Clear();
            carriedSceneStations.Clear();
            carriedMonsterBanner = false;

            ScanPlayerBuffItems(player, player.inventory);
            if (player.bank?.item != null) ScanPlayerBuffItems(player, player.bank.item);
            if (player.bank2?.item != null) ScanPlayerBuffItems(player, player.bank2.item);
            if (player.bank3?.item != null) ScanPlayerBuffItems(player, player.bank3.item);
            if (player.bank4?.item != null) ScanPlayerBuffItems(player, player.bank4.item);

            if (BigBagMod.EnableBigBag.val && BigBagMod.Slots != null)
            {
                ScanPlayerBuffItems(player, BigBagMod.Slots);
            }

            int threshold = PotionThreshold.val;
            if (threshold <= 0) threshold = 30;

            // 1. 无尽药水与食物
            if (EnableInfinitePotions.val)
            {
                if (foodCounts[3] >= threshold) AvailableInfiniteBuffs.Add(BuffID.WellFed3);
                if (foodCounts[2] >= threshold) AvailableInfiniteBuffs.Add(BuffID.WellFed2);
                if (foodCounts[1] >= threshold) AvailableInfiniteBuffs.Add(BuffID.WellFed);

                foreach (KeyValuePair<int, int> kvp in potionCounts)
                {
                    if (kvp.Value >= threshold)
                    {
                        AvailableInfiniteBuffs.Add(kvp.Key);
                    }
                }
            }

            // 2. 随身交互类增益站
            if (EnableBuffStations.val)
            {
                foreach (int stationBuff in carriedInteractiveStations)
                {
                    AvailableInfiniteBuffs.Add(stationBuff);
                }

                foreach (int sceneBuff in carriedSceneStations)
                {
                    AvailableInfiniteBuffs.Add(sceneBuff);
                }
            }

            // 3. 随身旗帜
            if (EnableMonsterBanners.val && carriedMonsterBanner)
            {
                AvailableInfiniteBuffs.Add(BuffID.MonsterBanner);
            }
        }

        /// <summary>
        /// 每帧在玩家更新增益时扫描背包并赋予交互类增益与无尽药水（并执行黑名单拦截与清理）
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateBuffs))]
        [HarmonyPrefix]
        public static void UpdateBuffsPrefix(Player __instance)
        {
            if (__instance == null || !__instance.active) return;
            if (__instance.whoAmI != Main.myPlayer) return;

            UpdateAvailableBuffs(__instance);

            int threshold = PotionThreshold.val;
            if (threshold <= 0) threshold = 30;

            if (EnableInfinitePotions.val)
            {
                // 食物跨槽堆叠合并判断（从高阶向低阶判定，若被黑名单禁用则尝试低阶）
                int foodToApply = 0;
                if (foodCounts[3] >= threshold && !InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed3)) foodToApply = BuffID.WellFed3;
                else if (foodCounts[2] >= threshold && !InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed2)) foodToApply = BuffID.WellFed2;
                else if (foodCounts[1] >= threshold && !InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed)) foodToApply = BuffID.WellFed;

                if (foodToApply > 0)
                {
                    __instance.AddBuff(foodToApply, 2, false);
                    ActiveInfiniteBuffs.Add(foodToApply);
                }

                // 清理被禁用的食物 Buff
                if (foodCounts[3] >= threshold && InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed3)) __instance.ClearBuff(BuffID.WellFed3);
                if (foodCounts[2] >= threshold && InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed2)) __instance.ClearBuff(BuffID.WellFed2);
                if (foodCounts[1] >= threshold && InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed)) __instance.ClearBuff(BuffID.WellFed);

                foreach (KeyValuePair<int, int> kvp in potionCounts)
                {
                    if (kvp.Value >= threshold)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(kvp.Key))
                        {
                            __instance.AddBuff(kvp.Key, 2, false);
                            ActiveInfiniteBuffs.Add(kvp.Key);
                        }
                        else
                        {
                            __instance.ClearBuff(kvp.Key);
                        }
                    }
                }
            }

            // 交互类增益站生效与黑名单清理
            if (EnableBuffStations.val)
            {
                foreach (int stationBuff in carriedInteractiveStations)
                {
                    if (!InfiniteBuffStorage.Blacklist.Contains(stationBuff))
                    {
                        __instance.AddBuff(stationBuff, 2, false);
                        ActiveInfiniteBuffs.Add(stationBuff);
                    }
                    else
                    {
                        __instance.ClearBuff(stationBuff);
                    }
                }

                // 场景光环增益记录入 ActiveInfiniteBuffs 或从黑名单中清除
                foreach (int sceneBuff in carriedSceneStations)
                {
                    if (!InfiniteBuffStorage.Blacklist.Contains(sceneBuff))
                    {
                        ActiveInfiniteBuffs.Add(sceneBuff);
                    }
                    else
                    {
                        __instance.ClearBuff(sceneBuff);
                    }
                }
            }

            // 随身旗帜在生效时记录入 ActiveInfiniteBuffs 或从黑名单清除
            if (EnableMonsterBanners.val && carriedMonsterBanner)
            {
                if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.MonsterBanner))
                {
                    __instance.AddBuff(BuffID.MonsterBanner, 2, false);
                    ActiveInfiniteBuffs.Add(BuffID.MonsterBanner);
                }
                else
                {
                    __instance.ClearBuff(BuffID.MonsterBanner);
                }
            }
        }

        private static void ScanPlayerBuffItems(Player player, Item[] items)
        {
            if (items == null) return;

            for (int i = 0; i < items.Length; i++)
            {
                Item item = items[i];
                if (item == null || item.IsAir || item.stack <= 0) continue;

                // 递归扫描随身收纳容器内部物品（如药水袋、旗帜盒）
                if (item.type >= ItemID.Count)
                {
                    var container = ItemLoader.GetModItem(item) as OptimizeAndTool.Content.Storage.ItemContainer.IItemContainer;
                    if (container?.Slots != null)
                    {
                        ScanPlayerBuffItems(player, container.Slots);
                    }
                }

                // 1. 无尽药水扫描
                if (EnableInfinitePotions.val && item.buffType > 0 && item.consumable)
                {
                    if (!Main.debuff[item.buffType] && !Main.lightPet[item.buffType] && !Main.vanityPet[item.buffType] && !Main.buffNoTimeDisplay[item.buffType])
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

                // 2. 交互类增益家具扫描（磨刀石、水晶球、弹药箱、施法桌、蛋糕块、战争桌等）
                if (EnableBuffStations.val)
                {
                    CheckInteractiveStationBuffs(item);
                    CheckSceneStationBuffs(item);
                }

                // 3. 随身旗帜扫描
                if (EnableMonsterBanners.val)
                {
                    if (ItemToBanner(item) >= 0)
                    {
                        carriedMonsterBanner = true;
                    }
                }
            }
        }

        private static void CheckInteractiveStationBuffs(Item item)
        {
            // 磨刀石 (Tile 377) -> 锋利 (Buff 159)
            if (item.type == ItemID.SharpeningStation || item.createTile == TileID.SharpeningStation)
            {
                carriedInteractiveStations.Add(BuffID.Sharpened);
            }
            // 水晶球 (Tile 125) -> 灵视 (Buff 29)
            else if (item.type == ItemID.CrystalBall || item.createTile == TileID.CrystalBall)
            {
                carriedInteractiveStations.Add(BuffID.Clairvoyance);
            }
            // 弹药箱 (Tile 287) -> 弹药箱 (Buff 93)
            else if (item.type == ItemID.AmmoBox || item.createTile == TileID.AmmoBox)
            {
                carriedInteractiveStations.Add(BuffID.AmmoBox);
            }
            // 施法桌 (Tile 354) -> 著魔 (Buff 150)
            else if (item.type == ItemID.BewitchingTable || item.createTile == TileID.BewitchingTable)
            {
                carriedInteractiveStations.Add(BuffID.Bewitched);
            }
            // 蛋糕块 (Tile 621) -> 糖果冲刺 (Buff 192)
            else if (item.type == ItemID.SliceOfCake || item.createTile == TileID.SliceOfCake)
            {
                carriedInteractiveStations.Add(BuffID.SugarRush);
            }
            // 战争桌 / 战争桌旗帜 (Tile 464) -> 战争桌 (Buff 348)
            else if (item.type == ItemID.WarTableBanner || item.type == ItemID.WarTable || item.createTile == TileID.WarTable || item.createTile == TileID.WarTableBanner)
            {
                carriedInteractiveStations.Add(BuffID.WarTable);
            }
        }

        private static void CheckSceneStationBuffs(Item item)
        {
            // 篝火 / 壁炉
            if (item.type == ItemID.Campfire || item.type == ItemID.Fireplace ||
                item.createTile == TileID.Campfire || item.createTile == TileID.Fireplace)
            {
                carriedSceneStations.Add(BuffID.Campfire);
            }
            // 心形灯笼 (Tile 42, Style 9)
            else if (item.type == ItemID.HeartLantern || (item.createTile == TileID.HangingLanterns && item.placeStyle == 9))
            {
                carriedSceneStations.Add(BuffID.HeartLamp);
            }
            // 星星瓶 (Tile 42, Style 7)
            else if (item.type == ItemID.StarinaBottle || (item.createTile == TileID.HangingLanterns && item.placeStyle == 7))
            {
                carriedSceneStations.Add(BuffID.StarInBottle);
            }
            // 巴斯特雕像
            else if (item.type == ItemID.CatBast || item.createTile == TileID.CatBast)
            {
                carriedSceneStations.Add(BuffID.CatBast);
            }
            // 向日葵
            else if (item.type == ItemID.Sunflower || item.createTile == TileID.Sunflower)
            {
                carriedSceneStations.Add(BuffID.Sunflower);
            }
            // 水蜡烛
            else if (item.type == ItemID.WaterCandle || item.createTile == TileID.WaterCandle)
            {
                carriedSceneStations.Add(BuffID.WaterCandle);
            }
            // 和平蜡烛
            else if (item.type == ItemID.PeaceCandle || item.createTile == TileID.PeaceCandle)
            {
                carriedSceneStations.Add(BuffID.PeaceCandle);
            }
            // 暗影蜡烛
            else if (item.type == ItemID.ShadowCandle || item.createTile == TileID.ShadowCandle)
            {
                carriedSceneStations.Add(BuffID.ShadowCandle);
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
