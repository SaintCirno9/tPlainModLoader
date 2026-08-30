using CommandHelp;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 城镇 NPC 机制与交互全面优化门控（基于 HookGen 强类型 On_ / IL_ 门控）
    /// 包含：
    /// 1. NPC 视距超远/全屏交互与对话持续保持（屏幕内点击直接对话，对话期间 NPC 停步面向玩家且不断开）；
    /// 2. NPC 夜晚/恶劣天气自动强制回房，防止在外面游荡不回；
    /// 3. 房屋管理面板一键右键召回全员 NPC 回房（带清晰 Tooltip 提示）与快捷指令；
    /// 4. NPC 快速/无视邪恶/夜晚自动入住；
    /// 5. NPC 快乐度锁定最优(75%买入/133%卖出)；
    /// 6. 旅商不离开与手动刷新商品；
    /// 7. 快捷护士一键满血。
    /// 作者: SaintCirno9
    /// </summary>
    public static class TownNPCOptimizationHooks
    {
        public static GetSetReset<bool> EnableInfiniteNPCReach = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableInstantHousingTeleport = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableNightAutoHome = new GetSetReset<bool>(false, false);
        // 默认关闭：开启会跳过 NPC 解锁前置（无条件置位 saved* 标记，含发型师/高尔夫等救援条件）
        public static GetSetReset<bool> EnableAutoHouse = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableOptimalHappiness = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableTravellingMerchantStay = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableQuickNurse = new GetSetReset<bool>(true, true);

        private static int autoHousingTimer = 0;
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            IL_WorldGen.ScoreRoom += Hook_ScoreRoom_IL;
            On_Player.Update += Hook_Player_Update;
            On_TileReachCheckSettings.GetRanges += Hook_TileReach_GetRanges;
            On_NPC.AI_007_TownEntities += Hook_NPC_AI_007_TownEntities;
            On_ShopHelper.GetShoppingSettings += Hook_ShopHelper_GetShoppingSettings;
            On_ShopHelper.GetTravelingMerchantPrices += Hook_ShopHelper_GetTravelingMerchantPrices;
            On_WorldGen.moveRoom += Hook_WorldGen_moveRoom;
            On_Main.DrawInventory += Hook_Main_DrawInventory;
            On_NPC.UpdateNPC += Hook_NPC_UpdateNPC;
            On_Main.GUIChatDrawInner += Hook_Main_GUIChatDrawInner;
            On_Player.SetTalkNPC += Hook_Player_SetTalkNPC;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            IL_WorldGen.ScoreRoom -= Hook_ScoreRoom_IL;
            On_Player.Update -= Hook_Player_Update;
            On_TileReachCheckSettings.GetRanges -= Hook_TileReach_GetRanges;
            On_NPC.AI_007_TownEntities -= Hook_NPC_AI_007_TownEntities;
            On_ShopHelper.GetShoppingSettings -= Hook_ShopHelper_GetShoppingSettings;
            On_ShopHelper.GetTravelingMerchantPrices -= Hook_ShopHelper_GetTravelingMerchantPrices;
            On_WorldGen.moveRoom -= Hook_WorldGen_moveRoom;
            On_Main.DrawInventory -= Hook_Main_DrawInventory;
            On_NPC.UpdateNPC -= Hook_NPC_UpdateNPC;
            On_Main.GUIChatDrawInner -= Hook_Main_GUIChatDrawInner;
            On_Player.SetTalkNPC -= Hook_Player_SetTalkNPC;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            CommandMethod cmdHome = new CommandMethod("townNPCHome", 0);
            cmdHome.Runing += _ => TeleportAllTownNPCsHome();

            CommandMethod cmdRefreshShop = new CommandMethod("refreshTravelShop", 0);
            cmdRefreshShop.Runing += _ => RefreshTravellingMerchantShop();

            return new List<CommandObject>
            {
                CommandBuild.get2("npcInfiniteReach", EnableInfiniteNPCReach),
                CommandBuild.get2("npcInstantHousingTeleport", EnableInstantHousingTeleport),
                CommandBuild.get2("npcNightAutoHome", EnableNightAutoHome),
                CommandBuild.get2("npcAutoHouse", EnableAutoHouse),
                CommandBuild.get2("npcOptimalHappiness", EnableOptimalHappiness),
                CommandBuild.get2("travellingMerchantStay", EnableTravellingMerchantStay),
                CommandBuild.get2("quickNurse", EnableQuickNurse),
                cmdHome,
                cmdRefreshShop
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableInfiniteNPCReach, "超远/全屏 NPC 对话交互，屏幕内点击任意城镇 NPC 即可直接开启对话，NPC 走动不会打断对话", "Images/Item_50", "NPC 视距全屏交互"),
                UIBuild.get2(EnableInstantHousingTeleport, "在房屋管理面板插旗或调整 NPC 住房分配时，立即将该 NPC 瞬移至新分配的房间与椅子上", "Images/Item_29", "换房即时瞬移"),
                UIBuild.get2(EnableNightAutoHome, "城镇 NPC 在夜晚或恶劣天气自动传送回各自房间的椅子上就座（非必要，推荐使用右键一键回房）", "Images/Item_1309", "NPC 夜间自动回房"),
                UIBuild.get2(EnableOptimalHappiness, "锁定所有城镇 NPC 快乐度为最优（75%最低买入价，133%最高卖出价）", "Images/Item_73", "NPC 快乐度锁定最优"),
                UIBuild.get2(EnableTravellingMerchantStay, "阻止旅商在傍晚离开或消失，常驻留在城镇中", "Images/Item_2274", "旅商常驻不离开"),
                UIBuild.get2(EnableQuickNurse, "右键护士直接扣费回满血并清除负面效果，跳过繁琐对话", "Images/Item_499", "快捷护士一键治疗")
            };
        }

        #region 1. NPC 快速与无视邪恶/夜晚入住 (ILContext 绕过邪恶计算)

        /// <summary>
        /// 当开启 NPC 无视邪恶入住时，将房屋评分中的邪恶图格扫描结果旁路清零
        /// </summary>
        public static int BypassEvilTileCount(int count)
        {
            if (EnableAutoHouse.val) return 0;
            return count;
        }

        private static void Hook_ScoreRoom_IL(ILContext il)
        {
            var cursor = new ILCursor(il);
            MethodInfo targetMethod = typeof(WorldGen).GetMethod("GetTileTypeCountByCategory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo bypassMethod = typeof(TownNPCOptimizationHooks).GetMethod(nameof(BypassEvilTileCount), BindingFlags.Public | BindingFlags.Static);

            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall(targetMethod)))
            {
                cursor.Emit(OpCodes.Call, bypassMethod);
            }
        }

        /// <summary>
        /// 玩家主循环：定期检测无家可归的 NPC、自动分配空余房屋、夜晚强制回房就座（可选）
        /// </summary>
        private static void Hook_Player_Update(On_Player.orig_Update orig, Player self, int i)
        {
            orig(self, i);

            if (self == null || !self.active || self.whoAmI != Main.myPlayer) return;

            autoHousingTimer++;
            if (autoHousingTimer % 60 == 0) // 每秒检测一次
            {
                // 1. 若开启了夜间自动回房，在夜晚或事件期间检查并回房
                if (EnableNightAutoHome.val)
                {
                    bool isNightOrDanger = !Main.dayTime || Main.eclipse || Main.bloodMoon || Main.pumpkinMoon || Main.snowMoon || Main.invasionType > 0;
                    if (isNightOrDanger)
                    {
                        for (int n = 0; n < Main.maxNPCs; n++)
                        {
                            NPC npc = Main.npc[n];
                            if (npc == null || !npc.active || !npc.townNPC || npc.homeless || npc.homeTileX <= 0 || npc.homeTileY <= 0) continue;

                            int distX = Math.Abs((int)(npc.position.X / 16f) - npc.homeTileX);
                            int distY = Math.Abs((int)(npc.position.Y / 16f) - npc.homeTileY);
                            if (distX > 3 || distY > 3 || (npc.ai[0] != 5f && npc.velocity.X != 0f))
                            {
                                TeleportNPCToChairOrHome(npc, playEffects: false);
                            }
                        }
                    }
                }

                // 2. 自动入住与加速生成
                if (EnableAutoHouse.val)
                {
                    if (Main.checkForSpawns < 7000)
                    {
                        Main.checkForSpawns += 25;
                    }

                    for (int n = 0; n < Main.maxNPCs; n++)
                    {
                        NPC npc = Main.npc[n];
                        if (npc != null && npc.active && npc.townNPC && npc.homeless)
                        {
                            WorldGen.QuickFindHome(npc.whoAmI);
                        }
                    }

                    if (NPC.downedBoss3) NPC.savedMech = true;
                    if (NPC.downedGoblins) NPC.savedGoblin = true;
                    if (Main.hardMode)
                    {
                        NPC.savedWizard = true;
                        NPC.savedTaxCollector = true;
                    }
                    if (NPC.downedBoss2) NPC.savedBartender = true;
                    // 注：以下三个为无条件解锁（跳过发型师/高尔夫/渔夫的救援与生成前置），属本功能的设计意图
                    NPC.savedStylist = true;
                    NPC.savedGolfer = true;
                    NPC.savedAngler = true;
                }
            }
        }

        #endregion

        #region 2. NPC 视距超远交互与对话停步

        private static void Hook_TileReach_GetRanges(On_TileReachCheckSettings.orig_GetRanges orig, ref TileReachCheckSettings self, out int x, out int y)
        {
            orig(ref self, out x, out y);

            if (EnableInfiniteNPCReach.val)
            {
                if (x < 150) x = 150;
                if (y < 150) y = 150;
            }
        }

        private static void Hook_NPC_AI_007_TownEntities(On_NPC.orig_AI_007_TownEntities orig, NPC self)
        {
            orig(self);

            if (self == null || !self.active || !self.townNPC) return;
            if (EnableInfiniteNPCReach.val && Main.LocalPlayer != null && Main.LocalPlayer.talkNPC == self.whoAmI)
            {
                self.velocity.X = 0f;
                self.direction = (Main.LocalPlayer.Center.X < self.Center.X) ? -1 : 1;
                self.spriteDirection = self.direction;
            }
        }

        #endregion

        #region 3. NPC 快乐度锁定最优

        private static ShoppingSettings Hook_ShopHelper_GetShoppingSettings(On_ShopHelper.orig_GetShoppingSettings orig, ShopHelper self, Player player, NPC npc)
        {
            ShoppingSettings result = orig(self, player, npc);

            if (EnableOptimalHappiness.val)
            {
                result.PriceAdjustment = 0.75f;
                if (GameCulture.FromCultureName(GameCulture.CultureName.Chinese).IsActive)
                {
                    result.HappinessReport = "我在这里过得非常舒心！所有商品都享有最大优惠折扣！";
                }
            }

            return result;
        }

        private static float Hook_ShopHelper_GetTravelingMerchantPrices(On_ShopHelper.orig_GetTravelingMerchantPrices orig, ShopHelper self, NPC npc)
        {
            if (EnableOptimalHappiness.val)
            {
                return 0.75f;
            }

            return orig(self, npc);
        }

        #endregion

        #region 4. 城镇 NPC 一键全员回家、插旗换房即时瞬移与房屋管理悬停提示

        public static bool TeleportNPCToChairOrHome(NPC npc, bool playEffects = false)
        {
            if (npc == null || !npc.active || !npc.townNPC || npc.homeless) return false;
            if (npc.homeTileX <= 0 || npc.homeTileY <= 0 || !WorldGen.InWorld(npc.homeTileX, npc.homeTileY)) return false;

            bool isPetOrSlime = NPCID.Sets.IsTownPet[npc.type] || NPCID.Sets.IsTownSlime[npc.type] || npc.type == 637 || npc.type == 638 || npc.type == 656;

            int homeX = npc.homeTileX;
            int homeY = npc.homeTileY;

            // 1. 调用房间检测获取准确的房间边界与内部图格泛洪映射
            bool roomValid = WorldGen.StartRoomCheck(homeX, Math.Max(10, homeY - 1));
            if (!roomValid)
            {
                roomValid = WorldGen.StartRoomCheck(homeX, homeY);
            }

            int bestChairX = -1;
            int bestChairY = -1;
            int minChairDist = int.MaxValue;

            int bestFloorX = -1;
            int bestFloorY = -1;
            int minFloorDist = int.MaxValue;

            if (roomValid && WorldGen.roomX2 >= WorldGen.roomX1 && WorldGen.roomY2 >= WorldGen.roomY1)
            {
                // 在泛洪确定的房间内部搜索椅子与安全地面
                for (int x = WorldGen.roomX1; x <= WorldGen.roomX2; x++)
                {
                    for (int y = WorldGen.roomY1; y <= WorldGen.roomY2; y++)
                    {
                        if (!WorldGen.InWorld(x, y)) continue;

                        // 必须是本房间内部的图格
                        bool inRoom = WorldGen.Housing_CheckIfInRoom(x, y) || WorldGen.Housing_CheckIfInRoom(x, y - 1);
                        if (!inRoom) continue;

                        Tile tile = Main.tile[x, y];
                        if (tile == null) continue;

                        // 搜索椅子/坐具
                        if (tile.active() && TileID.Sets.CanBeSatOnForNPCs[tile.type])
                        {
                            bool isOccupiedByOtherHuman = false;
                            for (int j = 0; j < Main.maxNPCs; j++)
                            {
                                NPC other = Main.npc[j];
                                if (other.active && other.townNPC && other.whoAmI != npc.whoAmI && other.ai[0] == 5f &&
                                    !NPCID.Sets.IsTownPet[other.type] && !NPCID.Sets.IsTownSlime[other.type] && other.type != 637 && other.type != 638 && other.type != 656)
                                {
                                    Point otherTile = (other.Bottom + Vector2.UnitY * -2f).ToTileCoordinates();
                                    if (Math.Abs(otherTile.X - x) <= 1 && Math.Abs(otherTile.Y - y) <= 2)
                                    {
                                        isOccupiedByOtherHuman = true;
                                        break;
                                    }
                                }
                            }

                            if (!isOccupiedByOtherHuman)
                            {
                                int dist = Math.Abs(x - homeX) * 2 + Math.Abs(y - homeY);
                                if (dist < minChairDist)
                                {
                                    minChairDist = dist;
                                    bestChairX = x;
                                    bestChairY = y;
                                }
                            }
                        }

                        // 搜索安全落脚地面（该图格为实心/平台，且上方有至少 2 格空间位于房间内）
                        if (IsSolidOrPlatform(tile) && !tile.inActive())
                        {
                            Tile above1 = Main.tile[x, y - 1];
                            Tile above2 = Main.tile[x, y - 2];
                            bool spaceClear = (above1 == null || !above1.active() || !Main.tileSolid[above1.type] || Main.tileSolidTop[above1.type]) &&
                                              (above2 == null || !above2.active() || !Main.tileSolid[above2.type] || Main.tileSolidTop[above2.type]);

                            if (spaceClear && (WorldGen.Housing_CheckIfInRoom(x, y - 1) || WorldGen.Housing_CheckIfInRoom(x, y - 2)))
                            {
                                int dist = Math.Abs(x - homeX) + Math.Abs(y - homeY);
                                if (dist < minFloorDist)
                                {
                                    minFloorDist = dist;
                                    bestFloorX = x;
                                    bestFloorY = y;
                                }
                            }
                        }
                    }
                }
            }

            // 回退后备方案：如果房间检测未搜到地面，向下垂直搜索实心地面
            if (bestFloorX == -1 || bestFloorY == -1)
            {
                bestFloorX = homeX;
                bestFloorY = homeY;
                while (bestFloorY < Main.maxTilesY - 20 && !IsSolidOrPlatform(Main.tile[bestFloorX, bestFloorY]))
                {
                    bestFloorY++;
                }
            }

            // 记录旧位置用于播放瞬移粒子
            Vector2 oldCenter = npc.Center;

            // 3. 处理人类城镇 NPC：传送到专属房间内的椅子就座
            if (!isPetOrSlime)
            {
                if (bestChairX != -1 && bestChairY != -1)
                {
                    Tile chairTile = Main.tile[bestChairX, bestChairY];
                    int chairFloorY = (chairTile.type == TileID.Chairs || chairTile.type == 497)
                        ? (chairTile.frameY % 40 != 0 ? bestChairY + 1 : bestChairY + 2)
                        : bestChairY + 1;

                    int chairDirection = (chairTile.frameX != 0) ? 1 : -1;
                    npc.ai[0] = 5f; // 坐姿状态
                    npc.ai[1] = 1800 + Main.rand.Next(3600);
                    npc.direction = chairDirection;
                    npc.spriteDirection = chairDirection;
                    npc.Bottom = new Vector2(bestChairX * 16 + 8 + 2 * chairDirection, chairFloorY * 16);
                    npc.velocity = Vector2.Zero;
                    npc.localAI[3] = 0f;
                    npc.netUpdate = true;

                    if (Main.netMode == 2)
                    {
                        NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
                    }

                    if (playEffects)
                    {
                        SpawnTeleportEffects(oldCenter, npc.Center);
                    }
                    return true;
                }

                // 若房间无椅子，回落至房间内部安全地面
                npc.ai[0] = 0f;
                npc.Bottom = new Vector2(bestFloorX * 16 + 8, bestFloorY * 16);
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;

                if (Main.netMode == 2)
                {
                    NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
                }

                if (playEffects)
                {
                    SpawnTeleportEffects(oldCenter, npc.Center);
                }
                return true;
            }

            // 4. 处理城镇宠物/史莱姆：不抢椅子，依附在房间椅子旁边或房间安全地面上
            if (bestChairX != -1 && bestChairY != -1)
            {
                Tile chairTile = Main.tile[bestChairX, bestChairY];
                int chairFloorY = (chairTile.type == TileID.Chairs || chairTile.type == 497)
                    ? (chairTile.frameY % 40 != 0 ? bestChairY + 1 : bestChairY + 2)
                    : bestChairY + 1;

                int chairDirection = (chairTile.frameX != 0) ? 1 : -1;

                // 尝试放在椅子朝向的正前方或后方 1 格
                int petTileX = bestChairX + (chairDirection == 1 ? 1 : -1);
                if (WorldGen.InWorld(petTileX, chairFloorY - 1) && Main.tile[petTileX, chairFloorY - 1]?.wall == 0 &&
                    WorldGen.InWorld(bestChairX - (chairDirection == 1 ? 1 : -1), chairFloorY - 1) && Main.tile[bestChairX - (chairDirection == 1 ? 1 : -1), chairFloorY - 1]?.wall > 0)
                {
                    petTileX = bestChairX - (chairDirection == 1 ? 1 : -1);
                }

                npc.ai[0] = 0f; // 宠物正常站立/趴下姿态
                npc.direction = -chairDirection; // 面朝主人
                npc.spriteDirection = npc.direction;
                npc.Bottom = new Vector2(petTileX * 16 + 8, chairFloorY * 16);
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;

                if (Main.netMode == 2)
                {
                    NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
                }

                if (playEffects)
                {
                    SpawnTeleportEffects(oldCenter, npc.Center);
                }
                return true;
            }

            // 若无椅子，宠物放置在房间内部安全地面
            npc.ai[0] = 0f;
            npc.Bottom = new Vector2(bestFloorX * 16 + 8, bestFloorY * 16);
            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;

            if (Main.netMode == 2)
            {
                NetMessage.SendData(23, -1, -1, null, npc.whoAmI);
            }

            if (playEffects)
            {
                SpawnTeleportEffects(oldCenter, npc.Center);
            }
            return true;
        }

        private static void SpawnTeleportEffects(Vector2 fromPos, Vector2 toPos)
        {
            if (Main.netMode == 2) return; // 服务器不播放图形粒子

            // 起点粒子
            for (int i = 0; i < 15; i++)
            {
                Dust d = Dust.NewDustDirect(fromPos - new Vector2(16, 24), 32, 48, DustID.JesterSparkleBlue, 0f, 0f, 100, default, 1.2f);
                d.velocity *= 0.5f;
            }

            // 终点粒子
            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustDirect(toPos - new Vector2(16, 24), 32, 48, DustID.JesterSparkleBlue, 0f, 0f, 100, default, 1.4f);
                d.velocity *= 0.7f;
            }
        }

        private static bool IsSolidOrPlatform(Tile tile)
        {
            if (tile != null && tile.active() && ((Main.tileSolid[tile.type] && !Main.tileSolidTop[tile.type]) || TileID.Sets.Platforms[tile.type]))
            {
                return !tile.inActive();
            }
            return false;
        }

        private static void Hook_WorldGen_moveRoom(On_WorldGen.orig_moveRoom orig, int x, int y, int n)
        {
            orig(x, y, n);

            if (!EnableInstantHousingTeleport.val) return;
            if (n < 0 || n >= Main.maxNPCs) return;

            NPC npc = Main.npc[n];
            if (npc != null && npc.active && npc.townNPC && !npc.homeless)
            {
                if (TeleportNPCToChairOrHome(npc, playEffects: true))
                {
                    SoundEngine.PlaySound(SoundID.Item6, npc.Center);
                }
            }
        }

        public static void TeleportAllTownNPCsHome()
        {
            int count = 0;

            // 第一轮：人类城镇 NPC 优先就座椅子
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !npc.townNPC || npc.homeless) continue;
                if (NPCID.Sets.IsTownPet[npc.type] || NPCID.Sets.IsTownSlime[npc.type] || npc.type == 637 || npc.type == 638 || npc.type == 656) continue;

                if (TeleportNPCToChairOrHome(npc, playEffects: true))
                {
                    count++;
                }
            }

            // 第二轮：城镇宠物与史莱姆归位至同房间主人身旁
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active || !npc.townNPC || npc.homeless) continue;
                if (!NPCID.Sets.IsTownPet[npc.type] && !NPCID.Sets.IsTownSlime[npc.type] && npc.type != 637 && npc.type != 638 && npc.type != 656) continue;

                if (TeleportNPCToChairOrHome(npc, playEffects: true))
                {
                    count++;
                }
            }

            if (count > 0)
            {
                SoundEngine.PlaySound(SoundID.Item6);
                Main.NewText($"[c/32FF82:已将 {count} 位城镇 NPC 与宠物召回各自房间并就座！]");
            }
            else
            {
                Main.NewText("[c/FFA500:当前没有分配了住房且存活的城镇 NPC]");
            }
        }

        private static void Hook_Main_DrawInventory(On_Main.orig_DrawInventory orig, Main self)
        {
            orig(self);

            if (Main.playerInventory && (Main.equipPageMouseOver == 1 || (Main.EquipPage == 1 && Main.mouseX >= Main.screenWidth - 70 && Main.mouseY >= 80 && Main.mouseY <= 140)))
            {
                Main.hoverItemName = (string.IsNullOrEmpty(Main.hoverItemName) ? "" : Main.hoverItemName + "\n") + "[c/32FF82:[右键点击] 立即召回全员城镇 NPC 回房]";
                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    Main.mouseRightRelease = false; // 消费右键
                    TeleportAllTownNPCsHome();
                }
            }
        }

        #endregion

        #region 5. 旅商不离开与手动刷新商品

        private static void Hook_NPC_UpdateNPC(On_NPC.orig_UpdateNPC orig, NPC self, int i)
        {
            if (self != null && self.active && EnableTravellingMerchantStay.val && self.type == NPCID.TravellingMerchant)
            {
                self.timeLeft = 7200;
                if (self.ai[0] == 1f)
                {
                    self.ai[0] = 0f;
                }
            }

            orig(self, i);
        }

        public static void RefreshTravellingMerchantShop()
        {
            Chest.SetupTravelShop();
            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.NewText("[c/00FFDD:旅商商品已重新进货刷新！]");
        }

        private static void Hook_Main_GUIChatDrawInner(On_Main.orig_GUIChatDrawInner orig, Main self)
        {
            orig(self);

            if (Main.LocalPlayer.talkNPC >= 0 && Main.npc[Main.LocalPlayer.talkNPC].type == NPCID.TravellingMerchant)
            {
                if (PlayerInput.MouseInfo.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    PlayerInput.MouseInfoOld.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
                {
                    RefreshTravellingMerchantShop();
                }
            }
        }

        #endregion

        #region 6. 快捷护士

        private static void Hook_Player_SetTalkNPC(On_Player.orig_SetTalkNPC orig, Player self, int npcIndex)
        {
            if (EnableQuickNurse.val && self != null && self.whoAmI == Main.myPlayer &&
                npcIndex >= 0 && npcIndex < Main.maxNPCs)
            {
                NPC npc = Main.npc[npcIndex];
                if (npc != null && npc.active && npc.type == NPCID.Nurse)
                {
                    int healCost = Main.GetNurseHealCost();

                    // 若满血且无异常状态，放行打开正常护士对话界面
                    if (self.statLife >= self.statLifeMax2 && healCost <= 0)
                    {
                        orig(self, npcIndex);
                        return;
                    }

                    // 检查玩家金钱是否足够
                    long totalMoney = Terraria.Utils.CoinsCount(out _, self.inventory);
                    if (totalMoney >= healCost)
                    {
                        Main.NPCChatText_DoNurseHeal(healCost);
                        Main.NewText($"[c/32FF82:护士已直接为你治愈伤势并清除负面状态！(花费 {Main.ValueToCoins(healCost)})]");
                        return; // 拦截打开护士对话
                    }
                    else
                    {
                        Main.NewText($"[c/FF4500:治疗需要 {Main.ValueToCoins(healCost)}，你身上的钱不够！]");
                        orig(self, npcIndex);
                        return;
                    }
                }
            }

            orig(self, npcIndex);
        }

        #endregion
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    public static class TownNPCOptimization
    {
        public static GetSetReset<bool> EnableInfiniteNPCReach => TownNPCOptimizationHooks.EnableInfiniteNPCReach;
        public static GetSetReset<bool> EnableInstantHousingTeleport => TownNPCOptimizationHooks.EnableInstantHousingTeleport;
        public static GetSetReset<bool> EnableNightAutoHome => TownNPCOptimizationHooks.EnableNightAutoHome;
        public static GetSetReset<bool> EnableAutoHouse => TownNPCOptimizationHooks.EnableAutoHouse;
        public static GetSetReset<bool> EnableOptimalHappiness => TownNPCOptimizationHooks.EnableOptimalHappiness;
        public static GetSetReset<bool> EnableTravellingMerchantStay => TownNPCOptimizationHooks.EnableTravellingMerchantStay;
        public static GetSetReset<bool> EnableQuickNurse => TownNPCOptimizationHooks.EnableQuickNurse;

        public static List<CommandObject> GetCO() => TownNPCOptimizationHooks.GetCO();
        public static List<UIElement> GetUI() => TownNPCOptimizationHooks.GetUI();
        public static void TeleportAllTownNPCsHome() => TownNPCOptimizationHooks.TeleportAllTownNPCsHome();
        public static void RefreshTravellingMerchantShop() => TownNPCOptimizationHooks.RefreshTravellingMerchantShop();
    }
}
