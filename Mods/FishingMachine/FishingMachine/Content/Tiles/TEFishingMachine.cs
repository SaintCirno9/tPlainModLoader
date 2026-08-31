using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.FishDropRules;
using Terraria.ID;
using TPML.Content;
using TPML.Content.IO;

namespace FishingMachine.Content.Tiles
{
    /// <summary>
    /// 自动钓鱼机物块实体 (ModTileEntity)
    /// 负责水域探测、原版渔获表判定、自动垂钓、战利品仓储、分类与自由过滤、相邻宝箱输送
    /// 作者: SaintCirno9
    /// </summary>
    public class TEFishingMachine : ModTileEntity
    {
        public Item fishingPole = new Item();
        public Item bait = new Item();
        public Item accessory = new Item();
        public Item[] fish = new Item[40];

        // 过滤设置
        public bool CatchCrates = true;
        public bool CatchAccessories = true;
        public bool CatchTools = true;
        public bool CatchWhiteRarityCatches = true;
        public bool CatchNormalCatches = true;
        public bool AutoDeposit = false;
        public bool InfiniteBait = false;
        public List<int> ExcludedItems = new List<int>();

        // 运行时状态
        public Point16 locatePoint = Point16.NegativeOne;
        public int fishingTimer = 0;
        public int autoDepositTimer = 0;
        public string statusTip = "准备就绪";
        public int lastFishingPower = 0;
        public int waterCount = 0;
        public bool isLava = false;
        public bool isHoney = false;
        public bool isShimmer = false;

        public bool LavaFishing = false;
        public bool TackleBox = false;
        public int FishingSkill = 0;
        public float SpeedMultiplier = 1f;

        private int pondRefreshTimer = 0;
        private int biomeRefreshTimer = 0;
        private int waterScanCooldown = 0; // FM-8: 水体探测失败降频冷却
        private readonly Player _biomePlayer = new Player();

        public TEFishingMachine()
        {
            for (int i = 0; i < fish.Length; i++)
            {
                fish[i] = new Item();
            }
        }

        public static int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate)
        {
            if (Main.netMode == 1)
            {
                NetMessage.SendTileSquare(Main.myPlayer, i - 1, j - 1, 2, 2);
                NetMessage.SendData(86, -1, -1, null, ModContent.TileEntityType<TEFishingMachine>(), i - 1, j - 1);
                return -1;
            }

            int id = ModContent.GetInstance<TEFishingMachine>()?.Place(i - 1, j - 1) ?? -1;
            if (id >= 0 && TileEntity.ByID.TryGetValue(id, out var te) && te is TEFishingMachine machine)
            {
                machine.FindNearbyWater();
            }
            return id;
        }

        public override void Update()
        {
            EnsureSlots();

            if (fishingPole == null || fishingPole.IsAir || fishingPole.fishingPole <= 0)
            {
                statusTip = "请放入钓竿";
                lastFishingPower = 0;
                return;
            }

            if (!InfiniteBait && (bait == null || bait.IsAir || bait.stack <= 0 || bait.bait <= 0))
            {
                statusTip = "请放入鱼饵";
                lastFishingPower = 0;
                return;
            }

            if (locatePoint.X < 0 || locatePoint.Y < 0 || Framing.GetTileSafely(locatePoint.X, locatePoint.Y).liquid == 0)
            {
                if (waterScanCooldown <= 0)
                {
                    FindNearbyWater();
                    if (locatePoint.X < 0)
                    {
                        waterScanCooldown = 60; // 失败后 60 tick 冷却，避免每帧全区域重扫 (FM-8)
                        statusTip = "未在周围 20 格内探测到有效水体";
                        lastFishingPower = 0;
                        return;
                    }
                }
                else
                {
                    waterScanCooldown--;
                    statusTip = "未在周围 20 格内探测到有效水体";
                    lastFishingPower = 0;
                    return;
                }
            }

            if (pondRefreshTimer <= 0)
            {
                RefreshPond();
                pondRefreshTimer = 360;
            }
            else
            {
                pondRefreshTimer--;
            }

            FishingAttempt fisher = GetFisher();
            lastFishingPower = fisher.fishingLevel;

            if (waterCount < 75)
            {
                statusTip = $"水体不足 ({waterCount}/75 格)，至少需要 75 格相连液体";
                return;
            }

            if (isShimmer)
            {
                statusTip = "微光水域无法正常垂钓";
                return;
            }

            if (fisher.inLava && !fisher.CanFishInLava)
            {
                statusTip = "需要在熔岩中垂钓的特殊装备或鱼饵";
                return;
            }

            if (fisher.fishingLevel <= 0)
            {
                statusTip = "有效渔力为 0，缺少有效的钓竿、鱼饵或饰品";
                return;
            }

            if (waterCount < fisher.waterNeededToFish)
            {
                statusTip = $"正常垂钓中 (渔力: {fisher.fishingLevel}% | 水域不足 -{(int)Math.Round(fisher.waterQuality * 100f)}%)";
            }
            else
            {
                statusTip = $"正常垂钓中 (渔力: {fisher.fishingLevel}% | 水域: {waterCount}格)";
            }

            // 垂钓计时器累加
            if (Main.rand.Next(300) < fisher.fishingLevel)
            {
                fishingTimer += Main.rand.Next(1, 3);
            }
            fishingTimer += fisher.fishingLevel / 30;
            fishingTimer += Main.rand.Next(1, 3);
            if (Main.rand.Next(60) == 0)
            {
                fishingTimer += 60;
            }

            // 综合速度计算
            float speed = SpeedMultiplier;
            int bassStack = CountBassStack();
            speed += Math.Min(bassStack * 0.05f, 5f);
            if (Main.hardMode)
            {
                speed += 2f;
            }

            if (fishingTimer > 2200f / Math.Max(speed, 0.1f))
            {
                fishingTimer = 0;
                ExecuteFishingCheck(fisher);
            }

            if (AutoDeposit)
            {
                autoDepositTimer++;
                if (autoDepositTimer > 180)
                {
                    autoDepositTimer = 0;
                    AutoDepositManipulation();
                }
            }
        }

        public void FindNearbyWater()
        {
            int originX = Position.X;
            int originY = Position.Y;

            for (int r = 1; r <= 20; r++)
            {
                for (int x = originX - r; x <= originX + r; x++)
                {
                    for (int y = originY - r; y <= originY + r; y++)
                    {
                        if (x < 10 || x >= Main.maxTilesX - 10 || y < 10 || y >= Main.maxTilesY - 10) continue;

                        Tile t = Framing.GetTileSafely(x, y);
                        if (t.liquid <= 0 || WorldGen.SolidTile(x, y)) continue;

                        locatePoint = new Point16(x, y);
                        RefreshPond();
                        pondRefreshTimer = 360;
                        return;
                    }
                }
            }
            locatePoint = Point16.NegativeOne;
        }

        public void RefreshPond()
        {
            if (locatePoint.X < 0 || locatePoint.Y < 0) return;

            isLava = false;
            isHoney = false;
            isShimmer = false;
            waterCount = 0;

            int minX = Position.X + 1 - 50;
            int maxX = minX + 100;
            int minY = Position.Y + 1 - 30;
            int maxY = minY + 60;

            Stack<Point> pending = new Stack<Point>();
            bool[,] checkedTile = new bool[101, 61];
            pending.Push(new Point(locatePoint.X, locatePoint.Y));

            while (pending.Count > 0)
            {
                Point p = pending.Pop();
                if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY) continue;

                int ox = p.X - minX;
                int oy = p.Y - minY;
                if (checkedTile[ox, oy]) continue;

                Tile tile = Framing.GetTileSafely(p.X, p.Y);
                if (tile.liquid <= 0 || WorldGen.SolidTile(p.X, p.Y)) continue;

                checkedTile[ox, oy] = true;
                waterCount++;
                if (tile.lava()) isLava = true;
                if (tile.honey()) isHoney = true;
                if (tile.shimmer()) isShimmer = true;

                pending.Push(new Point(p.X - 1, p.Y));
                pending.Push(new Point(p.X + 1, p.Y));
                pending.Push(new Point(p.X, p.Y - 1));
                pending.Push(new Point(p.X, p.Y + 1));
            }

            if (isHoney)
            {
                waterCount = (int)(waterCount * 1.5);
            }
        }

        public FishingAttempt GetFisher()
        {
            if (biomeRefreshTimer <= 0)
            {
                RefreshBiome();
                biomeRefreshTimer = 120;
            }
            else
            {
                biomeRefreshTimer--;
            }

            ResetStats();

            PlayerFishingConditions conditions = GetFishingConditions();
            FishingAttempt fisher = new FishingAttempt
            {
                X = locatePoint.X,
                Y = locatePoint.Y,
                bobberType = fishingPole.shoot,
                playerFishingConditions = conditions,
                inLava = isLava,
                inHoney = isHoney,
                waterTilesCount = waterCount,
                chumsInWater = 0,
                fishingLevel = conditions.FinalFishingLevel
            };

            fisher.CanFishInLava =
                ItemID.Sets.CanFishInLava[conditions.PoleItemType] ||
                ItemID.Sets.IsLavaBait[conditions.BaitItemType] ||
                LavaFishing;

            if (waterCount < 75) return fisher;
            if (fisher.fishingLevel <= 0) return fisher;

            float num = (float)Main.maxTilesX / 4200f;
            num *= num;
            fisher.atmo = (float)((fisher.Y - (60f + 10f * num)) / (Main.worldSurface / 6.0));
            if (fisher.atmo < 0.25f) fisher.atmo = 0.25f;
            if (fisher.atmo > 1f) fisher.atmo = 1f;

            fisher.waterNeededToFish = Math.Max(1, (int)(300f * fisher.atmo));
            float quality = (float)waterCount / fisher.waterNeededToFish;
            if (quality < 1f)
            {
                fisher.fishingLevel = (int)(fisher.fishingLevel * quality);
            }
            fisher.waterQuality = 1f - quality;

            Player player = _biomePlayer;
            if (player.luck < 0f)
            {
                if (Main.rand.NextFloat() < -player.luck)
                {
                    fisher.fishingLevel = (int)(fisher.fishingLevel * (0.9 - Main.rand.NextFloat() * 0.3));
                }
            }
            else if (Main.rand.NextFloat() < player.luck)
            {
                fisher.fishingLevel = (int)(fisher.fishingLevel * (1.1 + Main.rand.NextFloat() * 0.3));
            }

            // FM-4: 折算后增加 <= 0 截断防御，消除 150 / fishingLevel 除零异常
            if (fisher.fishingLevel <= 0) return fisher;

            fisher.heightLevel = 0;
            if (Main.remixWorld)
            {
                if (fisher.Y < Main.worldSurface * 0.5) fisher.heightLevel = 0;
                else if (fisher.Y < Main.worldSurface) fisher.heightLevel = 1;
                else if (fisher.Y < Main.rockLayer) fisher.heightLevel = 3;
                else if (fisher.Y < Main.maxTilesY - 300) fisher.heightLevel = 2;
                else fisher.heightLevel = 4;
                if (fisher.heightLevel == 2 && Main.rand.Next(2) == 0) fisher.heightLevel = 1;
            }
            else if (fisher.Y < Main.worldSurface * 0.5)
            {
                fisher.heightLevel = 0;
            }
            else if (fisher.Y < Main.worldSurface)
            {
                fisher.heightLevel = 1;
            }
            else if (fisher.Y < Main.rockLayer)
            {
                fisher.heightLevel = 2;
            }
            else if (fisher.Y < Main.maxTilesY - 300)
            {
                fisher.heightLevel = 3;
            }
            else
            {
                fisher.heightLevel = 4;
            }

            fisher.junk =
                Main.rand.Next(50) > fisher.fishingLevel &&
                Main.rand.Next(50) > fisher.fishingLevel &&
                waterCount < fisher.waterNeededToFish;

            FishingCheck_RollDropLevels(fisher.fishingLevel, player,
                out fisher.common, out fisher.uncommon, out fisher.rare,
                out fisher.veryrare, out fisher.legendary, out fisher.crate);

            ProbeForQuestFish(ref fisher, player);
            return fisher;
        }

        public void ExecuteFishingCheck(FishingAttempt fisher)
        {
            if (fisher.playerFishingConditions.BaitItemType == ItemID.TruffleWorm)
            {
                if ((fisher.X < 380 || fisher.X > Main.maxTilesX - 380) && fisher.waterTilesCount > 1000 && NPC.CountNPCS(NPCID.DukeFishron) < 3)
                {
                    if (Main.rand.Next(5) == 0)
                    {
                        int npcIndex = NPC.NewNPC(new EntitySource_FishedOut(Main.LocalPlayer), fisher.X * 16, fisher.Y * 16, NPCID.DukeFishron);
                        if (npcIndex < 200)
                        {
                            Main.npc[npcIndex].target = Main.myPlayer;
                            string name = Main.npc[npcIndex].TypeName;
                            Main.NewText($"[c/FF5555:松露虫引来了强大的生物: {name}！]");
                            TryConsumeBait();
                            statusTip = $"引出了 {name}！";
                            return;
                        }
                    }
                }
            }

            int biteChance = (fisher.fishingLevel + 75) / 2;
            if (Main.rand.Next(100) > biteChance)
            {
                statusTip = "浮标轻微晃动，未咬钩";
                return;
            }

            RollEnemySpawn(ref fisher, _biomePlayer);
            if (fisher.rolledEnemySpawn > 0)
            {
                statusTip = "钓到了血月敌怪，已自动驱逐";
                TryConsumeBait();
                return;
            }

            FishingContext context = BuildFishingContext(fisher);
            int caughtType = Main.FishDropsDB.TryGetItemDropType(context);
            if (caughtType <= 0)
            {
                statusTip = "浮标空竿，继续等待";
                return;
            }

            if (!ShouldAcceptCatch(caughtType))
            {
                statusTip = $"已按过滤规则丢弃: {Lang.GetItemNameValue(caughtType)}";
                TryConsumeBait();
                return;
            }

            int stack = BuildCatchStack(caughtType, fisher.fishingLevel);
            int addedStack = AddItemToInventory(caughtType, stack);
            if (addedStack <= 0)
            {
                statusTip = "战利品仓已满，本次渔获已丢失";
                return;
            }

            TryConsumeBait();
            statusTip = $"捕获: {Lang.GetItemNameValue(caughtType)} x{addedStack}";
            SoundPlayHelper.PlayCatchSound();

            Vector2 waterPos = new Vector2(locatePoint.X * 16 + 8, locatePoint.Y * 16 + 8);
            Vector2 machinePos = new Vector2(Position.X * 16 + 16, Position.Y * 16 + 16);
            TriggerChestTransferVisual(waterPos, machinePos, caughtType);
        }

        private void RefreshBiome()
        {
            _biomePlayer.width = 20;
            _biomePlayer.height = 24;
            _biomePlayer.position = new Vector2(Position.X * 16f - 8f, Position.Y * 16f - 16f);
            _biomePlayer.UpdateSceneMetrics();
            _biomePlayer.UpdateBiomes();

            // FM-2: 同步本地玩家的幸运值与药水加成
            Player local = Main.LocalPlayer;
            if (local != null && local.active)
            {
                _biomePlayer.luck = local.luck;
                _biomePlayer.cratePotion = local.cratePotion;
            }
        }

        private PlayerFishingConditions GetFishingConditions()
        {
            PlayerFishingConditions result = default(PlayerFishingConditions);
            result.PolePower = fishingPole != null && !fishingPole.IsAir ? fishingPole.fishingPole : 0;
            result.PoleItemType = fishingPole != null && !fishingPole.IsAir ? fishingPole.type : 0;

            if (InfiniteBait)
            {
                result.BaitPower = bait != null && !bait.IsAir && bait.bait > 0 ? bait.bait : 50;
                result.BaitItemType = bait != null && !bait.IsAir && bait.bait > 0 ? bait.type : ItemID.MasterBait;
            }
            else
            {
                result.BaitPower = bait != null && !bait.IsAir ? bait.bait : 0;
                result.BaitItemType = bait != null && !bait.IsAir ? bait.type : 0;
            }

            result.LevelMultipliers = Fishing_GetPowerMultiplier();
            result.FinalFishingLevel = (int)((result.BaitPower + result.PolePower + FishingSkill) * result.LevelMultipliers);
            return result;
        }

        private static float Fishing_GetPowerMultiplier()
        {
            float num = 1f;
            if (Main.raining) num *= 1.2f;
            if (Main.cloudBGAlpha > 0f) num *= 1.1f;
            if (Main.dayTime && (Main.time < 5400.0 || Main.time > 48600.0)) num *= 1.3f;
            if (Main.dayTime && Main.time > 16200.0 && Main.time < 37800.0) num *= 0.8f;
            if (!Main.dayTime && Main.time > 6480.0 && Main.time < 25920.0) num *= 0.8f;

            switch (Main.moonPhase)
            {
                case 0: num *= 1.1f; break;
                case 1:
                case 7: num *= 1.05f; break;
                case 3:
                case 5: num *= 0.95f; break;
                case 4: num *= 0.9f; break;
            }
            if (Main.bloodMoon) num *= 1.1f;
            return num;
        }

        private void ResetStats()
        {
            LavaFishing = false;
            TackleBox = false;
            FishingSkill = 0;
            SpeedMultiplier = 1f;

            if (accessory == null || accessory.IsAir) return;

            if (accessory.type == ItemID.TackleBox)
            {
                TackleBox = true;
                SpeedMultiplier = 1.5f;
            }
            else if (accessory.type == ItemID.AnglerEarring)
            {
                FishingSkill += 10;
                SpeedMultiplier = 2f;
            }
            else if (accessory.type == ItemID.AnglerTackleBag)
            {
                FishingSkill += 10;
                SpeedMultiplier = 2.5f;
                TackleBox = true;
            }
            else if (accessory.type == ItemID.LavaFishingHook)
            {
                LavaFishing = true;
                SpeedMultiplier = 1.5f;
            }
            else if (accessory.type == ItemID.LavaproofTackleBag)
            {
                FishingSkill += 10;
                SpeedMultiplier = 3f;
                TackleBox = true;
                LavaFishing = true;
            }
        }

        private void RollEnemySpawn(ref FishingAttempt fisher, Player player)
        {
            if (fisher.inLava || fisher.inHoney || !Main.bloodMoon || Main.dayTime) return;

            int maxValue = 6;
            if (fisher.playerFishingConditions.PoleItemType == ItemID.BloodFishingRod)
            {
                maxValue = 3;
            }

            if (Main.rand.Next(maxValue) != 0) return;

            if (!NPC.unlockedSlimeRedSpawn && Main.rand.Next(5) == 0)
            {
                fisher.rolledEnemySpawn = NPCID.TownSlimeRed;
            }
            else if (Main.hardMode)
            {
                fisher.rolledEnemySpawn = Utils.SelectRandom(Main.rand,
                    new short[] { NPCID.GoblinShark, NPCID.BloodEelHead, NPCID.ZombieMerman, NPCID.EyeballFlyingFish });
                if (Main.rand.Next(10) == 0)
                {
                    fisher.rolledEnemySpawn = NPCID.BloodNautilus;
                }
            }
            else
            {
                fisher.rolledEnemySpawn = Utils.SelectRandom(Main.rand,
                    new short[] { NPCID.ZombieMerman, NPCID.EyeballFlyingFish });
            }
        }

        private static void FishingCheck_RollDropLevels(int fishingLevel, Player player,
            out bool common, out bool uncommon, out bool rare, out bool veryrare, out bool legendary, out bool crate)
        {
            int safeLevel = Math.Max(1, fishingLevel);
            int num = 150 / safeLevel;
            int num2 = 150 * 2 / safeLevel;
            int num3 = 150 * 7 / safeLevel;
            int num4 = 150 * 15 / safeLevel;
            int num5 = 150 * 30 / safeLevel;
            int num6 = 10;
            if (player.cratePotion) num6 += 15;

            if (num < 2) num = 2;
            if (num2 < 3) num2 = 3;
            if (num3 < 4) num3 = 4;
            if (num4 < 5) num4 = 5;
            if (num5 < 6) num5 = 6;

            common = Main.rand.Next(num) == 0;
            uncommon = Main.rand.Next(num2) == 0;
            rare = Main.rand.Next(num3) == 0;
            veryrare = Main.rand.Next(num4) == 0;
            legendary = Main.rand.Next(num5) == 0;
            crate = Main.rand.Next(100) < num6;
        }

        private static void ProbeForQuestFish(ref FishingAttempt fisher, Player player)
        {
            fisher.questFish = -1;
            if (Main.anglerQuest < 0 || Main.anglerQuest >= Main.anglerQuestItemNetIDs.Length) return;
            if (Main.anglerQuestFinished || !NPC.AnyNPCs(NPCID.Angler)) return;

            int quest = Main.anglerQuestItemNetIDs[Main.anglerQuest];
            Player local = Main.LocalPlayer;
            if (local != null && !local.HasItem(quest))
            {
                fisher.questFish = quest;
            }
        }

        private FishingContext BuildFishingContext(FishingAttempt fisher)
        {
            Player player = _biomePlayer;

            bool corrupt = player.ZoneCorrupt;
            bool crimson = player.ZoneCrimson;
            bool jungle = player.ZoneJungle;
            bool snow = player.ZoneSnow;
            bool dungeon = player.ZoneDungeon;
            if (!NPC.downedBoss3) dungeon = false;
            if (Main.notTheBeesWorld && !Main.remixWorld && Main.rand.Next(2) == 0) jungle = false;
            if (Main.remixWorld && fisher.heightLevel == 0)
            {
                corrupt = false;
                crimson = false;
            }
            else if (corrupt & crimson)
            {
                if (Main.rand.Next(2) == 0) crimson = false;
                else corrupt = false;
            }

            if ((snow & jungle) && Main.rand.Next(2) == 0) snow = false;

            bool desert = player.ZoneDesert;
            if (dungeon) desert = false;
            bool remixOcean = Main.remixWorld && fisher.heightLevel == 1 &&
                               fisher.Y >= Main.rockLayer && Main.rand.Next(3) == 0;

            return new FishingContext
            {
                Random = Main.rand,
                Fisher = fisher,
                Player = player,
                RolledCorruption = corrupt,
                RolledCrimson = crimson,
                RolledJungle = jungle,
                RolledSnow = snow,
                RolledDesert = desert,
                RolledInfectedDesert = desert && Main.rand.Next(2) == 0,
                RolledRemixOcean = remixOcean
            };
        }

        private bool ShouldAcceptCatch(int itemType)
        {
            if (ExcludedItems.Contains(itemType)) return false;

            if (ItemID.Sets.IsFishingCrate[itemType] || ItemID.Sets.IsFishingCrateHardmode[itemType])
                return CatchCrates;

            Item sample = new Item();
            sample.SetDefaults(itemType);

            if (sample.accessory) return CatchAccessories;
            if (sample.damage > 0 && !sample.accessory) return CatchTools;
            if (sample.rare <= 0) return CatchWhiteRarityCatches;
            return CatchNormalCatches;
        }

        private int BuildCatchStack(int itemType, int fishingLevel)
        {
            if (itemType == ItemID.BombFish)
            {
                int min = (fishingLevel / 20 + 3) / 2;
                int max = (fishingLevel / 10 + 6) / 2;

                if (Main.rand.Next(50) < fishingLevel) max++;
                if (Main.rand.Next(100) < fishingLevel) max++;
                if (Main.rand.Next(150) < fishingLevel) max++;
                if (Main.rand.Next(200) < fishingLevel) max++;
                return Main.rand.Next(min, max + 1);
            }
            if (itemType == ItemID.FrostDaggerfish)
            {
                int min = (fishingLevel / 4 + 15) / 2;
                int max = (fishingLevel / 2 + 30) / 2;

                if (Main.rand.Next(50) < fishingLevel) max += 4;
                if (Main.rand.Next(100) < fishingLevel) max += 4;
                if (Main.rand.Next(150) < fishingLevel) max += 4;
                if (Main.rand.Next(200) < fishingLevel) max += 4;
                return Main.rand.Next(min, max + 1);
            }
            return 1;
        }

        /// <summary>
        /// 将钓起的物品存入机器背包，返回实际入库的数量 (FM-5)
        /// </summary>
        public int AddItemToInventory(int itemType, int stack)
        {
            if (itemType <= 0 || stack <= 0) return 0;
            int initialStack = stack;

            for (int i = 0; i < fish.Length; i++)
            {
                if (fish[i] != null && !fish[i].IsAir && fish[i].type == itemType && fish[i].stack < fish[i].maxStack)
                {
                    int add = Math.Min(stack, fish[i].maxStack - fish[i].stack);
                    fish[i].stack += add;
                    stack -= add;
                    if (stack <= 0) return initialStack;
                }
            }

            for (int i = 0; i < fish.Length; i++)
            {
                if (fish[i] == null || fish[i].IsAir)
                {
                    fish[i] = new Item();
                    fish[i].SetDefaults(itemType);
                    fish[i].stack = stack;
                    return initialStack;
                }
            }

            return initialStack - stack;
        }

        public void LootAll(Player player)
        {
            for (int i = 0; i < fish.Length; i++)
            {
                if (fish[i] == null || fish[i].IsAir || fish[i].favorited) continue;

                Item left = player.GetItem(fish[i], GetItemSettings.LootAllFromChest);
                if (left.stack <= 0)
                {
                    fish[i] = new Item();
                }
                else
                {
                    fish[i] = left;
                }
            }
            Recipe.UpdateRecipeList();
        }

        private void AutoDepositManipulation()
        {
            List<int> nearby = new List<int>();
            Vector2 center = new Vector2(Position.X + 1, Position.Y + 1);

            for (int c = 0; c < Main.maxChests; c++)
            {
                Chest chest = Main.chest[c];
                if (chest == null || chest.x < 0 || chest.y < 0 || Chest.IsLocked(chest.x, chest.y)) continue;

                Vector2 chestPos = new Vector2(chest.x, chest.y);
                if (Vector2.DistanceSquared(center, chestPos) < 900f)
                {
                    nearby.Add(c);
                }
            }

            if (nearby.Count == 0) return;

            nearby.Sort((a, b) =>
            {
                Chest ca = Main.chest[a];
                Chest cb = Main.chest[b];
                float da = Vector2.DistanceSquared(center, new Vector2(ca.x, ca.y));
                float db = Vector2.DistanceSquared(center, new Vector2(cb.x, cb.y));
                return da.CompareTo(db);
            });

            Vector2 machineWorld = new Vector2(Position.X * 16 + 16, Position.Y * 16 + 16);

            foreach (int chestIndex in nearby)
            {
                Chest chest = Main.chest[chestIndex];
                Vector2 chestWorld = new Vector2(chest.x * 16 + 16, chest.y * 16 + 16);
                bool allStored = true;

                for (int i = 0; i < fish.Length; i++)
                {
                    if (fish[i] == null || fish[i].IsAir || fish[i].favorited) continue;

                    int itemType = fish[i].type;
                    int stackBefore = fish[i].stack;
                    bool moved = false;

                    for (int j = 0; j < chest.item.Length; j++)
                    {
                        Item cItem = chest.item[j];
                        if (cItem != null && !cItem.IsAir && cItem.type == fish[i].type && cItem.stack < cItem.maxStack)
                        {
                            int add = Math.Min(fish[i].stack, cItem.maxStack - cItem.stack);
                            cItem.stack += add;
                            fish[i].stack -= add;
                            if (fish[i].stack <= 0)
                            {
                                fish[i].TurnToAir();
                                moved = true;
                                break;
                            }
                        }
                    }

                    if (fish[i].stack > 0)
                    {
                        for (int j = 0; j < chest.item.Length; j++)
                        {
                            Item cItem = chest.item[j];
                            if (cItem == null || cItem.IsAir)
                            {
                                chest.item[j] = fish[i].Clone();
                                fish[i].TurnToAir();
                                moved = true;
                                break;
                            }
                        }
                    }

                    if (moved || fish[i].stack != stackBefore)
                    {
                        TriggerChestTransferVisual(machineWorld, chestWorld, itemType);
                    }

                    if (!fish[i].IsAir)
                    {
                        allStored = false;
                    }
                }

                if (allStored) break;
            }
        }

        private static void TriggerChestTransferVisual(Vector2 from, Vector2 to, int itemType)
        {
            Chest.ItemTransferVisualizationSettings settings = new Chest.ItemTransferVisualizationSettings
            {
                TransitionIn = true,
                Fullbright = true
            };
            Chest.VisualizeChestTransfer(from, to, itemType, settings);
        }

        private void EnsureSlots()
        {
            if (fishingPole == null) fishingPole = new Item();
            if (bait == null) bait = new Item();
            if (accessory == null) accessory = new Item();

            for (int i = 0; i < fish.Length; i++)
            {
                if (fish[i] == null) fish[i] = new Item();
            }
        }

        private int CountBassStack()
        {
            int count = 0;
            for (int i = 0; i < fish.Length; i++)
            {
                if (fish[i] != null && fish[i].type == ItemID.Bass)
                {
                    count += fish[i].stack;
                }
            }
            return count;
        }

        private bool TryConsumeBait()
        {
            if (InfiniteBait || bait == null || bait.IsAir) return false;

            float num = 1f + (float)bait.bait / 6f;
            if (num < 1f) num = 1f;
            if (TackleBox) num++;
            num *= 2.5f;

            if (Main.rand.NextFloat() * num < 1f)
            {
                if (bait.type == ItemID.LadyBug || bait.type == ItemID.GoldLadyBug)
                {
                    NPC.LadyBugKilled(new Vector2(Position.X * 16 + 16, Position.Y * 16 + 16), bait.type == ItemID.GoldLadyBug);
                }

                bait.stack--;
                if (bait.stack <= 0) bait.TurnToAir();
                return true;
            }
            return false;
        }

        public bool ToggleExcludedItem(int itemType)
        {
            if (itemType <= 0) return false;

            if (ExcludedItems.Contains(itemType))
            {
                ExcludedItems.Remove(itemType);
                return false;
            }

            ExcludedItems.Add(itemType);
            return true;
        }

        public void DropContents()
        {
            IEntitySource src = new EntitySource_TileBreak(Position.X, Position.Y);
            Vector2 worldPos = new Vector2(Position.X * 16 + 16, Position.Y * 16 + 16);

            SpawnDrop(src, worldPos, fishingPole);
            SpawnDrop(src, worldPos, bait);
            SpawnDrop(src, worldPos, accessory);

            for (int i = 0; i < fish.Length; i++)
            {
                if (fish[i] != null && !fish[i].IsAir)
                {
                    SpawnDrop(src, worldPos, fish[i]);
                    fish[i].TurnToAir();
                }
            }
        }

        private static void SpawnDrop(IEntitySource src, Vector2 pos, Item item)
        {
            if (item == null || item.IsAir) return;
            int idx = Item.NewItem(src, pos, item.type, item.stack);
            if (item.prefix > 0 && idx >= 0 && idx < Main.maxItems)
            {
                Main.item[idx].Prefix(item.prefix);
            }
        }

        #region Sidecar TagCompound 持久化

        public override void SaveData(TagCompound tag)
        {
            tag["fishingPole"] = fishingPole ?? new Item();
            tag["bait"] = bait ?? new Item();
            tag["accessory"] = accessory ?? new Item();

            var fishEntries = ModItemSidecarEngine.SerializeSlots(fish);
            tag["fish"] = Newtonsoft.Json.JsonConvert.SerializeObject(fishEntries);

            tag["CatchCrates"] = CatchCrates;
            tag["CatchAccessories"] = CatchAccessories;
            tag["CatchTools"] = CatchTools;
            tag["CatchWhiteRarityCatches"] = CatchWhiteRarityCatches;
            tag["CatchNormalCatches"] = CatchNormalCatches;
            tag["AutoDeposit"] = AutoDeposit;
            tag["InfiniteBait"] = InfiniteBait;
            tag["ExcludedItems"] = ExcludedItems;

            tag["locatePointX"] = locatePoint.X;
            tag["locatePointY"] = locatePoint.Y;
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("fishingPole")) fishingPole = tag.Get<Item>("fishingPole") ?? new Item();
            if (tag.ContainsKey("bait")) bait = tag.Get<Item>("bait") ?? new Item();
            if (tag.ContainsKey("accessory")) accessory = tag.Get<Item>("accessory") ?? new Item();

            if (tag.ContainsKey("fish"))
            {
                try
                {
                    string json = tag.GetString("fish");
                    var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ContainerSlotEntry>>(json);
                    ModItemSidecarEngine.DeserializeSlots(entries, fish);
                }
                catch { }
            }

            if (tag.ContainsKey("CatchCrates")) CatchCrates = tag.GetBool("CatchCrates");
            if (tag.ContainsKey("CatchAccessories")) CatchAccessories = tag.GetBool("CatchAccessories");
            if (tag.ContainsKey("CatchTools")) CatchTools = tag.GetBool("CatchTools");
            if (tag.ContainsKey("CatchWhiteRarityCatches")) CatchWhiteRarityCatches = tag.GetBool("CatchWhiteRarityCatches");
            if (tag.ContainsKey("CatchNormalCatches")) CatchNormalCatches = tag.GetBool("CatchNormalCatches");
            if (tag.ContainsKey("AutoDeposit")) AutoDeposit = tag.GetBool("AutoDeposit");
            if (tag.ContainsKey("InfiniteBait")) InfiniteBait = tag.GetBool("InfiniteBait");
            if (tag.ContainsKey("ExcludedItems")) ExcludedItems = tag.Get<List<int>>("ExcludedItems") ?? new List<int>();

            if (tag.ContainsKey("locatePointX") && tag.ContainsKey("locatePointY"))
            {
                locatePoint = new Point16(tag.GetInt("locatePointX"), tag.GetInt("locatePointY"));
            }
        }

        #endregion
    }

    internal static class SoundPlayHelper
    {
        public static void PlayCatchSound()
        {
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }
}