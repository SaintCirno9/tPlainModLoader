using CommandHelp;
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
    /// 无尽药水续杯、随身增益站与怪物旗帜门控（基于 HookGen 强类型 On_ 门控）：
    /// 当背包或便携收纳中药水堆叠达到阈值（默认 30）时常驻提供对应增益；
    /// 携带篝火、心灯、巴斯特雕像、磨刀石、水晶球等增益站或怪物旗帜时常驻赋予增益；
    /// 通过挂载 SceneMetrics.Scan 从根本上解决旗帜与场景光环 Buff 闪烁问题；
    /// 支持无限增益黑名单过滤、收藏置顶管理与在绘制 Buff 栏时隐藏无尽 Buff 图标。
    /// 作者: SaintCirno9
    /// </summary>
    public static class InfinitePotionAndBuffHooks
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

        // 区域型家具（水蜡烛/和平蜡烛/暗影蜡烛）：只写 SceneMetrics 指标（光照/生成率），永不产生系统授予的真实 Buff 图标，
        // 单独成集合以免混入 ActiveInfiniteBuffs 后令“隐藏无尽图标”误伤世界放置蜡烛的原生图标
        private static readonly HashSet<int> carriedZoneMarkers = new HashSet<int>();
        private static bool carriedMonsterBanner = false;

        // 扫描节流：全量递归扫描（背包+四银行+大背包+嵌套容器）开销随容器规模增长，低频刷新即可
        private const int ScanIntervalTicks = 15;
        private static int scanCooldown = 0;

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_SceneMetrics.Scan += Hook_Scan;
            On_Player.UpdateBuffs += Hook_UpdateBuffs;
            On_Main.DrawBuffIcon += Hook_DrawBuffIcon;
            On_Main.DrawInterface_Resources_Buffs += Hook_DrawInterface_Resources_Buffs;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_SceneMetrics.Scan -= Hook_Scan;
            On_Player.UpdateBuffs -= Hook_UpdateBuffs;
            On_Main.DrawBuffIcon -= Hook_DrawBuffIcon;
            On_Main.DrawInterface_Resources_Buffs -= Hook_DrawInterface_Resources_Buffs;
            _registered = false;
        }

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

        static InfinitePotionAndBuffHooks()
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
        /// 在场景指标扫描完毕后注入随身旗帜与场景光环，彻底根除 Buff 闪烁问题；
        /// 确保怪物旗帜与场景增益站 100% 精准写入当前 SceneMetrics
        /// </summary>
        private static void Hook_Scan(On_SceneMetrics.orig_Scan orig, SceneMetrics self, SceneMetricsScanSettings settings)
        {
            orig(self, settings);

            if (self == null) return;
            if (!EnableMonsterBanners.val && !EnableBuffStations.val) return;

            Player player = settings.PerspectivePlayer ?? (Main.netMode != 2 ? Main.LocalPlayer : null);
            if (player == null || !player.active) return;

            bool isLocalScan = Main.netMode != 2 && player.whoAmI == Main.myPlayer;

            ScanSceneContainerItems(self, player.inventory, isLocalScan);
            if (player.bank?.item != null) ScanSceneContainerItems(self, player.bank.item, isLocalScan);
            if (player.bank2?.item != null) ScanSceneContainerItems(self, player.bank2.item, isLocalScan);
            if (player.bank3?.item != null) ScanSceneContainerItems(self, player.bank3.item, isLocalScan);
            if (player.bank4?.item != null) ScanSceneContainerItems(self, player.bank4.item, isLocalScan);

            // 大背包为进程级静态集合（服务端多玩家共用一份数据），联机下仅对本机玩家开放以避免串号
            if (isLocalScan && BigBagMod.EnableBigBag.val && BigBagMod.Slots != null)
            {
                ScanSceneContainerItems(self, BigBagMod.Slots, isLocalScan);
            }
        }

        private static void ScanSceneContainerItems(SceneMetrics metrics, Item[] items, bool isLocalScan)
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
                        ScanSceneContainerItems(metrics, container.Slots, isLocalScan);
                    }
                }

                // 1. 随身旗帜注入（受黑名单控制；仅本地玩家扫描时生效）
                if (isLocalScan && EnableMonsterBanners.val && !InfiniteBuffStorage.Blacklist.Contains(BuffID.MonsterBanner))
                {
                    int bannerId = ItemToBanner(item);
                    if (bannerId >= 0 && metrics.NPCBannerBuff != null && bannerId < metrics.NPCBannerBuff.Length)
                    {
                        metrics.NPCBannerBuff[bannerId] = true;
                        metrics.hasBanner = true;
                    }
                }

                // 2. 随身场景增益站与区域型标记注入
                if (EnableBuffStations.val)
                {
                    InjectSceneStationMetrics(metrics, item, isLocalScan);
                }
            }
        }

        private static void InjectSceneStationMetrics(SceneMetrics metrics, Item item, bool isLocalScan)
        {
            if (IsCampfireItem(item))
            {
                if (isLocalScan && !InfiniteBuffStorage.Blacklist.Contains(BuffID.Campfire)) metrics.HasCampfire = true;
            }
            else if (IsHeartLanternItem(item))
            {
                if (isLocalScan && !InfiniteBuffStorage.Blacklist.Contains(BuffID.HeartLamp)) metrics.HasHeartLantern = true;
            }
            else if (IsStarBottleItem(item))
            {
                if (isLocalScan && !InfiniteBuffStorage.Blacklist.Contains(BuffID.StarInBottle)) metrics.HasStarInBottle = true;
            }
            else if (IsCatBastItem(item))
            {
                if (isLocalScan && !InfiniteBuffStorage.Blacklist.Contains(BuffID.CatBast)) metrics.HasCatBast = true;
            }
            else if (IsSunflowerItem(item))
            {
                if (isLocalScan && !InfiniteBuffStorage.Blacklist.Contains(BuffID.Sunflower)) metrics.HasSunflower = true;
            }
            // 以下为区域型家具：只写 SceneMetrics 指标，随后由原版复制到各归属玩家自身的 Zone/HasGardenGnomeNearby 字段，
            // 联机下按人互不干扰，可对全部玩家安全注入；黑名单仅作用于随身携带的部分
            else if (IsWaterCandleItem(item))
            {
                if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.WaterCandle)) metrics.ZoneWaterCandle = true;
            }
            else if (IsPeaceCandleItem(item))
            {
                if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.PeaceCandle)) metrics.ZonePeaceCandle = true;
            }
            else if (IsShadowCandleItem(item))
            {
                if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.ShadowCandle)) metrics.ZoneShadowCandle = true;
            }
            else if (IsGardenGnomeItem(item))
            {
                // 花园侏儒提供的是幸运值而非任何 Buff 图标，黑名单系统基于 BuffID 无法表达，
                // 故此处不接黑名单；如需全局关闭请直接关闭“随身增益站”
                metrics.HasGardenGnome = true;
            }
        }

        /// <summary>
        /// 强制下一次调用立即重新扫描（UI 开窗等需要即时新鲜数据的场景），否则处于扫描节流期
        /// </summary>
        public static void ResetScanCache()
        {
            scanCooldown = 0;
        }

        /// <summary>
        /// 主动扫描并更新玩家背包中的可用无尽增益（UI与帧更新通用）
        /// </summary>
        public static void UpdateAvailableBuffs(Player player)
        {
            if (player == null || !player.active) return;

            if (scanCooldown > 0)
            {
                scanCooldown--;
                return; // 节流期内沿用上次扫描快照（potionCounts/carried* 各集合保持不变）
            }
            scanCooldown = ScanIntervalTicks;

            AvailableInfiniteBuffs.Clear();
            ActiveInfiniteBuffs.Clear();
            potionCounts.Clear();
            foodCounts[1] = 0;
            foodCounts[2] = 0;
            foodCounts[3] = 0;
            carriedInteractiveStations.Clear();
            carriedSceneStations.Clear();
            carriedZoneMarkers.Clear();
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

                // 区域型只进入“可用”列表供黑名单管理（关闭随身蜡烛的区域效果），但不计入已激活
                foreach (int zoneBuff in carriedZoneMarkers)
                {
                    AvailableInfiniteBuffs.Add(zoneBuff);
                }
            }

            // 3. 随身旗帜
            if (EnableMonsterBanners.val && carriedMonsterBanner)
            {
                AvailableInfiniteBuffs.Add(BuffID.MonsterBanner);
            }
        }

        private static void Hook_UpdateBuffs(On_Player.orig_UpdateBuffs orig, Player self, int i)
        {
            if (i == 0 && self != null && self.active && self.whoAmI == Main.myPlayer)
            {
                PerformUpdateBuffs(self);
            }

            orig(self, i);
        }

        /// <summary>
        /// 在玩家更新增益时扫描背包并赋予交互类增益与无尽药水（并执行黑名单拦截与清理）
        /// </summary>
        private static void PerformUpdateBuffs(Player self)
        {
            UpdateAvailableBuffs(self);

            int threshold = PotionThreshold.val;
            if (threshold <= 0) threshold = 30;

            if (EnableInfinitePotions.val)
            {
                // 食物跨槽堆叠合并判断（从高阶向低阶判定，若被黑名单禁用则尝试低阶）
                int foodToApply = 0;
                if (foodCounts[3] >= threshold && !InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed3)) foodToApply = BuffID.WellFed3;
                else if (foodCounts[2] >= threshold && !InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed2)) foodToApply = BuffID.WellFed2;
                else if (foodCounts[1] >= threshold && !InfiniteBuffStorage.Blacklist.Contains(BuffID.WellFed)) foodToApply = BuffID.WellFed;

                if (foodToApply > 0 && !HasNaturalFoodBuff(self))
                {
                    // 存在自然进食(时长>2帧)的食物 Buff 时跳过续杯：
                    // 原版 AddBuff 对食物类(IsFedState)会先清除玩家全部已存在食物再添加，
                    // 直接续杯会把刚吃下的高阶/更持久美食立刻覆盖抹除
                    self.AddBuff(foodToApply, 2, false);
                    ActiveInfiniteBuffs.Add(foodToApply);
                }

                // 仅清理无尽系统赋予的极短时间(<=2帧)食物 Buff，保留玩家正常进食获得的 Buff
                int[] foods = { BuffID.WellFed, BuffID.WellFed2, BuffID.WellFed3 };
                foreach (int f in foods)
                {
                    if (InfiniteBuffStorage.Blacklist.Contains(f))
                    {
                        ClearShortLivedBuff(self, f);
                    }
                }

                foreach (KeyValuePair<int, int> kvp in potionCounts)
                {
                    if (kvp.Value >= threshold)
                    {
                        if (!InfiniteBuffStorage.Blacklist.Contains(kvp.Key))
                        {
                            self.AddBuff(kvp.Key, 2, false);
                            ActiveInfiniteBuffs.Add(kvp.Key);
                        }
                        else
                        {
                            ClearShortLivedBuff(self, kvp.Key);
                        }
                    }
                }
            }

            // 交互类增益站生效与黑名单停止赋予
            if (EnableBuffStations.val)
            {
                foreach (int stationBuff in carriedInteractiveStations)
                {
                    if (!InfiniteBuffStorage.Blacklist.Contains(stationBuff))
                    {
                        self.AddBuff(stationBuff, 2, false);
                        ActiveInfiniteBuffs.Add(stationBuff);
                    }
                    else
                    {
                        ClearShortLivedBuff(self, stationBuff);
                    }
                }

                // 场景光环增益记录入 ActiveInfiniteBuffs
                foreach (int sceneBuff in carriedSceneStations)
                {
                    if (!InfiniteBuffStorage.Blacklist.Contains(sceneBuff))
                    {
                        ActiveInfiniteBuffs.Add(sceneBuff);
                    }
                    else
                    {
                        ClearShortLivedBuff(self, sceneBuff);
                    }
                }
            }

            // 随身旗帜在生效时记录入 ActiveInfiniteBuffs
            if (EnableMonsterBanners.val && carriedMonsterBanner)
            {
                if (!InfiniteBuffStorage.Blacklist.Contains(BuffID.MonsterBanner))
                {
                    self.AddBuff(BuffID.MonsterBanner, 2, false);
                    ActiveInfiniteBuffs.Add(BuffID.MonsterBanner);
                }
                else
                {
                    ClearShortLivedBuff(self, BuffID.MonsterBanner);
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
            // 可授予真实 Buff 的场景增益站（参与列表展示、激活记录与黑名单清理）
            if (IsCampfireItem(item))
            {
                carriedSceneStations.Add(BuffID.Campfire);
            }
            else if (IsHeartLanternItem(item))
            {
                carriedSceneStations.Add(BuffID.HeartLamp);
            }
            else if (IsStarBottleItem(item))
            {
                carriedSceneStations.Add(BuffID.StarInBottle);
            }
            else if (IsCatBastItem(item))
            {
                carriedSceneStations.Add(BuffID.CatBast);
            }
            else if (IsSunflowerItem(item))
            {
                carriedSceneStations.Add(BuffID.Sunflower);
            }
            // 区域型蜡烛单独记录（仅进“可用”列表供黑名单管理，不计入已激活，详见字段注释）
            else if (IsWaterCandleItem(item))
            {
                carriedZoneMarkers.Add(BuffID.WaterCandle);
            }
            else if (IsPeaceCandleItem(item))
            {
                carriedZoneMarkers.Add(BuffID.PeaceCandle);
            }
            else if (IsShadowCandleItem(item))
            {
                carriedZoneMarkers.Add(BuffID.ShadowCandle);
            }
        }

        #region 场景家具分类谓词（SceneMetrics 注入与随身扫描两处消费方共用，避免检测表复制漂移）

        private static bool IsCampfireItem(Item item)
        {
            return item.type == ItemID.Campfire || item.type == ItemID.Fireplace ||
                   item.createTile == TileID.Campfire || item.createTile == TileID.Fireplace;
        }

        private static bool HeartLanternMatches(Item item)
        {
            return item.type == ItemID.HeartLantern || (item.createTile == TileID.HangingLanterns && item.placeStyle == 9);
        }

        private static bool IsHeartLanternItem(Item item) => HeartLanternMatches(item);

        private static bool IsStarBottleItem(Item item)
        {
            return item.type == ItemID.StarinaBottle || (item.createTile == TileID.HangingLanterns && item.placeStyle == 7);
        }

        private static bool IsCatBastItem(Item item)
        {
            return item.type == ItemID.CatBast || item.createTile == TileID.CatBast;
        }

        private static bool IsSunflowerItem(Item item)
        {
            return item.type == ItemID.Sunflower || item.createTile == TileID.Sunflower;
        }

        private static bool IsWaterCandleItem(Item item)
        {
            return item.type == ItemID.WaterCandle || item.createTile == TileID.WaterCandle;
        }

        private static bool IsPeaceCandleItem(Item item)
        {
            return item.type == ItemID.PeaceCandle || item.createTile == TileID.PeaceCandle;
        }

        private static bool IsShadowCandleItem(Item item)
        {
            return item.type == ItemID.ShadowCandle || item.createTile == TileID.ShadowCandle;
        }

        private static bool IsGardenGnomeItem(Item item)
        {
            return item.type == ItemID.GardenGnome || item.createTile == TileID.GardenGnome;
        }

        #endregion

        private static void ClearShortLivedBuff(Player player, int buffType)
        {
            if (player?.buffType == null || player.buffTime == null) return;

            for (int i = 0; i < player.buffType.Length; i++)
            {
                if (player.buffType[i] == buffType && player.buffTime[i] <= 2)
                {
                    player.DelBuff(i);
                    break;
                }
            }
        }

        /// <summary>
        /// 是否存在自然进食获得的食物 Buff（时长 > 2 帧）。
        /// 本系统自续的食物每帧仅保留 <=2 帧，故以此阈值区分自然 buff 与系统续杯
        /// </summary>
        private static bool HasNaturalFoodBuff(Player player)
        {
            if (player?.buffType == null || player.buffTime == null) return false;

            for (int i = 0; i < player.buffType.Length; i++)
            {
                int t = player.buffType[i];
                if ((t == BuffID.WellFed || t == BuffID.WellFed2 || t == BuffID.WellFed3) && player.buffTime[i] > 2)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 绘制 Buff 栏图标时若开启隐藏无尽 Buff，则拦截对应图标的绘制
        /// </summary>
        private static int Hook_DrawBuffIcon(On_Main.orig_DrawBuffIcon orig, int drawBuffText, int buffSlotOnPlayer, int x, int y)
        {
            if (!HideEndlessBuffs.val)
            {
                return orig(drawBuffText, buffSlotOnPlayer, x, y);
            }

            Player player = Main.LocalPlayer;
            if (player == null || buffSlotOnPlayer < 0 || buffSlotOnPlayer >= player.buffType.Length)
            {
                return orig(drawBuffText, buffSlotOnPlayer, x, y);
            }

            int buffType = player.buffType[buffSlotOnPlayer];
            if (buffType > 0 && ActiveInfiniteBuffs.Contains(buffType))
            {
                // 保持原版返回值契约 —— 原方法未命中悬停时原样透传 drawBuffText（“最后悬停槽位”游标）
                return drawBuffText;
            }

            return orig(drawBuffText, buffSlotOnPlayer, x, y);
        }

        /// <summary>
        /// 隐藏无尽 Buff 时接管原版主 Buff 栏绘制（DrawInterface_Resources_Buffs）：
        /// 隐藏图标不再参与网格占位，后续图标前移补位，消除空位空洞；
        /// 未开启隐藏或本帧无可隐藏图标时走原版路径零差异。悬停提示与右键移除均基于真实槽位索引
        /// </summary>
        private static void Hook_DrawInterface_Resources_Buffs(On_Main.orig_DrawInterface_Resources_Buffs orig, Main self)
        {
            if (!HideEndlessBuffs.val)
            {
                orig(self);
                return;
            }

            Player player = Main.player[Main.myPlayer];
            if (player?.buffType == null || !HasHiddenActiveBuffs(player))
            {
                orig(self);
                return;
            }

            CompactDrawBuffBar(player);
        }

        private static bool HasHiddenActiveBuffs(Player player)
        {
            for (int i = 0; i < player.buffType.Length; i++)
            {
                if (player.buffType[i] > 0 && ActiveInfiniteBuffs.Contains(player.buffType[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 原版 DrawInterface_Resources_Buffs 的等价实现（GameSource Main.cs L43980–44029），
        /// 唯一差异：隐藏的无尽 Buff 跳过网格占位，其余行为（空槽 alpha、行列布局、悬停 tooltip、右键移除）逐行对齐
        /// </summary>
        private static void CompactDrawBuffBar(Player player)
        {
            Main.PipsUseGrid = false;

            int drawBuffText = -1; // “最后悬停槽位”游标，与原版 drawBuffText 语义一致
            int perRow = 11;
            int pos = 0;

            for (int i = 0; i < Player.maxBuffs; i++)
            {
                int buffType = player.buffType[i];
                if (buffType <= 0)
                {
                    Main.buffAlpha[i] = 0.4f;
                    continue;
                }

                // 隐藏的无尽 Buff 不占格、直接跳过布局
                if (ActiveInfiniteBuffs.Contains(buffType))
                {
                    continue;
                }

                int row = pos / perRow;
                int col = pos % perRow;
                drawBuffText = Main.DrawBuffIcon(drawBuffText, i, 32 + col * 38, 76 + row * 50);
                pos++;
            }

            if (drawBuffText < 0) return;

            int hovered = player.buffType[drawBuffText];
            if (hovered > 0)
            {
                string buffName = Lang.GetBuffName(hovered);
                string buffTooltip = Main.GetBuffTooltip(player, hovered);
                if (hovered == 147)
                {
                    Main.bannerMouseOver = true;
                }

                if (Main.meleeBuff[hovered])
                {
                    Main.instance.MouseTextHackZoom(buffName, -10, 0, buffTooltip, true);
                }
                else
                {
                    Main.instance.MouseTextHackZoom(buffName, buffTooltip, true);
                }
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    public static class InfinitePotionAndBuff
    {
        public static GetSetReset<bool> EnableInfinitePotions => InfinitePotionAndBuffHooks.EnableInfinitePotions;
        public static GetSetReset<int> PotionThreshold => InfinitePotionAndBuffHooks.PotionThreshold;
        public static GetSetReset<bool> EnableBuffStations => InfinitePotionAndBuffHooks.EnableBuffStations;
        public static GetSetReset<bool> EnableMonsterBanners => InfinitePotionAndBuffHooks.EnableMonsterBanners;
        public static GetSetReset<bool> HideEndlessBuffs => InfinitePotionAndBuffHooks.HideEndlessBuffs;

        public static HashSet<int> AvailableInfiniteBuffs => InfinitePotionAndBuffHooks.AvailableInfiniteBuffs;
        public static HashSet<int> ActiveInfiniteBuffs => InfinitePotionAndBuffHooks.ActiveInfiniteBuffs;

        public static List<CommandObject> GetCO() => InfinitePotionAndBuffHooks.GetCO();
        public static List<UIElement> GetUI() => InfinitePotionAndBuffHooks.GetUI();

        public static int ItemToBanner(Item item) => InfinitePotionAndBuffHooks.ItemToBanner(item);
        public static void ResetScanCache() => InfinitePotionAndBuffHooks.ResetScanCache();
        public static void UpdateAvailableBuffs(Player player) => InfinitePotionAndBuffHooks.UpdateAvailableBuffs(player);
        public static void UpdateBuffsPrefix(Player player) => InfinitePotionAndBuffHooks.UpdateAvailableBuffs(player);
    }
}
