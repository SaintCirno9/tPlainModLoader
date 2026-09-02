using Microsoft.Xna.Framework;
using TPML;
using Terraria;
using Terraria.ID;
using OptimizeAndTool.Content.QoL;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 生态与植被增强门控（草药极速生长/开花/补种、全树种生长/掉果/宝石树掉宝石、移除墓地暗角与音乐，基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    public class EcologyHooks : TPML.Content.ModSystem
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_WorldGen.GrowAlch += Hook_GrowAlch;
            On_WorldGen.IsHarvestableHerbWithSeed += Hook_IsHarvestableHerbWithSeed;
            On_WorldGen.IsAlchemyPlantHarvestable += Hook_IsAlchemyPlantHarvestable;
            On_WorldGen.KillTile += Hook_KillTile;
            On_WorldGen.AttemptToGrowTreeFromSapling += Hook_AttemptToGrowTreeFromSapling;
            On_WorldGen.ShakeTree += Hook_ShakeTree;
            On_SceneMetrics.CalculateZones += Hook_CalculateZones;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_WorldGen.GrowAlch -= Hook_GrowAlch;
            On_WorldGen.IsHarvestableHerbWithSeed -= Hook_IsHarvestableHerbWithSeed;
            On_WorldGen.IsAlchemyPlantHarvestable -= Hook_IsAlchemyPlantHarvestable;
            On_WorldGen.KillTile -= Hook_KillTile;
            On_WorldGen.AttemptToGrowTreeFromSapling -= Hook_AttemptToGrowTreeFromSapling;
            On_WorldGen.ShakeTree -= Hook_ShakeTree;
            On_SceneMetrics.CalculateZones -= Hook_CalculateZones;
            _registered = false;
        }

        private static int _growthTimer = 0;

        public override void UpdatePrefix(GameTime gameTime)
        {
            if (QoLValSet.removeGraveyardVisuals.val)
            {
                Main.GraveyardVisualIntensity = 0f;
                if (Main.curMusic == MusicID.Graveyard)
                {
                    Main.curMusic = Main.newMusic;
                }
            }

            UpdateActiveEcoGrowth();
        }

        private static void UpdateActiveEcoGrowth()
        {
            // 仅在单人模式或服务端执行生态加速，避免客户端脱节
            if (Main.netMode == 1) return;

            // 每 10 帧触发一次平滑抽样推进（约 5~10 秒内平滑长成）
            _growthTimer++;
            if (_growthTimer % 10 != 0) return;

            bool growTree = QoLValSet.treeFastGrow.val;
            bool growHerb = QoLValSet.herbFastGrow.val;
            bool growPumpkin = EcoGrowthHooks.EnablePumpkinFastGrow.val;

            if (!growTree && !growHerb && !growPumpkin) return;

            for (int p = 0; p < Main.maxPlayers; p++)
            {
                Player player = Main.player[p];
                if (!player.active || player.dead) continue;

                int playerTileX = (int)(player.Center.X / 16f);
                int playerTileY = (int)(player.Center.Y / 16f);

                // 视野缓冲范围内随机采样 50 个坐标
                for (int k = 0; k < 50; k++)
                {
                    int rx = playerTileX + Main.rand.Next(-70, 71);
                    int ry = playerTileY + Main.rand.Next(-50, 51);

                    if (rx < 5 || rx >= Main.maxTilesX - 5 || ry < 5 || ry >= Main.maxTilesY - 5) continue;
                    Tile tile = Main.tile[rx, ry];
                    if (tile == null || !tile.active()) continue;

                    // 1. 树苗生长（支持森林/针叶/丛林/腐化/猩红/神圣/棕榈/灰烬/樱花/黄柳/7种宝石树）
                    if (growTree && (tile.type == TileID.Saplings || tile.type == TileID.GemSaplings || tile.type == TileID.VanityTreeSakuraSaplings || tile.type == TileID.VanityTreeWillowSaplings))
                    {
                        if (Main.rand.Next(4) == 0)
                        {
                            bool isUnderground = (double)ry > Main.worldSurface;
                            WorldGen.AttemptToGrowTreeFromSapling(rx, ry, isUnderground);
                        }
                    }
                    // 2. 草药平滑两阶段生长（幼苗 -> 成熟 -> 开花）
                    else if (growHerb && (tile.type == TileID.ImmatureHerbs || tile.type == TileID.MatureHerbs))
                    {
                        if (Main.rand.Next(3) == 0)
                        {
                            if (tile.type == TileID.ImmatureHerbs)
                            {
                                tile.type = TileID.MatureHerbs;
                            }
                            else if (tile.type == TileID.MatureHerbs)
                            {
                                tile.type = TileID.BloomingHerbs;
                            }
                            WorldGen.SquareTileFrame(rx, ry);
                            if (Main.netMode == 2) NetMessage.SendTileSquare(-1, rx, ry, 1);
                        }
                    }
                    // 3. 南瓜平滑生长
                    else if (growPumpkin && tile.type == TileID.Pumpkins)
                    {
                        if (Main.rand.Next(3) == 0)
                        {
                            WorldGen.GrowPumpkin(rx, ry, tile.type);
                        }
                    }
                    // 4. 仙人掌平滑生长
                    else if (growHerb && tile.type == TileID.Cactus)
                    {
                        if (Main.rand.Next(6) == 0)
                        {
                            WorldGen.GrowCactus(rx, ry);
                        }
                    }
                    // 5. 竹子平滑生长
                    else if (growHerb && (tile.type == 571 || tile.type == 572))
                    {
                        if (Main.rand.Next(6) == 0 && (!Main.tile[rx, ry - 1].active() || Main.tile[rx, ry - 1].type == 61 || Main.tile[rx, ry - 1].type == 74))
                        {
                            WorldGen.PlaceBamboo(rx, ry - 1);
                        }
                    }
                    // 6. 巨型发光蘑菇生长（地下发光蘑菇草皮上的蘑菇）
                    else if (growTree && tile.type == TileID.MushroomPlants && (double)ry > Main.worldSurface)
                    {
                        if (Main.rand.Next(8) == 0)
                        {
                            WorldGen.GrowEpicTree(rx, ry);
                        }
                    }
                }
            }
        }

        #region 草药极速生长与任意时刻开花

        private static void Hook_GrowAlch(On_WorldGen.orig_GrowAlch orig, int x, int y)
        {
            if (!QoLValSet.herbFastGrow.val)
            {
                orig(x, y);
                return;
            }

            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
            {
                orig(x, y);
                return;
            }

            Tile tile = Main.tile[x, y];
            if (tile == null || !tile.active())
            {
                orig(x, y);
                return;
            }

            if (tile.type == TileID.ImmatureHerbs)
            {
                tile.type = TileID.MatureHerbs;
                WorldGen.SquareTileFrame(x, y);
                if (Main.netMode == 2) NetMessage.SendTileSquare(-1, x, y, 1);
                else if (Main.netMode == 1) NetMessage.SendTileSquare(Main.myPlayer, x, y, 1);
                return;
            }
            else if (tile.type == TileID.MatureHerbs)
            {
                tile.type = TileID.BloomingHerbs;
                WorldGen.SquareTileFrame(x, y);
                if (Main.netMode == 2) NetMessage.SendTileSquare(-1, x, y, 1);
                else if (Main.netMode == 1) NetMessage.SendTileSquare(Main.myPlayer, x, y, 1);
                return;
            }

            orig(x, y);
        }

        private static bool Hook_IsHarvestableHerbWithSeed(On_WorldGen.orig_IsHarvestableHerbWithSeed orig, int type, int style, int y)
        {
            if (QoLValSet.herbBloomAnytime.val)
            {
                return true;
            }
            return orig(type, style, y);
        }

        private static bool Hook_IsAlchemyPlantHarvestable(On_WorldGen.orig_IsAlchemyPlantHarvestable orig, int style, int y)
        {
            if (QoLValSet.herbBloomAnytime.val)
            {
                return true;
            }
            return orig(style, y);
        }

        #endregion

        #region 再生法杖自动补种 & 宝石树全段掉宝石

        private static void Hook_KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
        {
            if (!fail && !effectOnly && !noItem && i >= 0 && i < Main.maxTilesX && j >= 0 && j < Main.maxTilesY)
            {
                Tile tile = Main.tile[i, j];
                if (tile != null && tile.active())
                {
                    // 1. 宝石树全段必掉宝石
                    if (QoLValSet.gemTreeFullGemDrops.val && Main.netMode != 1)
                    {
                        int gemItem = GetGemTreeGemItem(tile.type);
                        if (gemItem > 0)
                        {
                            int gemCount = Main.rand.Next(1, 3);
                            Vector2 dropPos = new Vector2(i * 16 + 8, j * 16 + 8);
                            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), dropPos, gemItem, gemCount);
                        }
                    }

                    // 2. 再生法杖/再生之斧自动原地补种（需检查下方物块是否有效）
                    if (QoLValSet.staffOfRegenAutoReplant.val)
                    {
                        if (tile.type == TileID.ImmatureHerbs || tile.type == TileID.MatureHerbs || tile.type == TileID.BloomingHerbs)
                        {
                            Player player = Main.LocalPlayer;
                            if (player != null && (player.HeldItem.type == ItemID.StaffofRegrowth || player.HeldItem.type == ItemID.AcornAxe))
                            {
                                int style = tile.frameX / 18;
                                int targetX = i;
                                int targetY = j;

                                Main.QueueMainThreadAction(() =>
                                {
                                    if (!Main.tile[targetX, targetY].active() &&
                                        Main.tile[targetX, targetY + 1] != null &&
                                        Main.tile[targetX, targetY + 1].active())
                                    {
                                        WorldGen.PlaceTile(targetX, targetY, TileID.ImmatureHerbs, mute: true, forced: true, style: style);
                                        if (Main.netMode == 1)
                                        {
                                            NetMessage.SendTileSquare(player.whoAmI, targetX, targetY, 1);
                                        }
                                    }
                                });
                            }
                        }
                    }
                }
            }

            orig(i, j, fail, effectOnly, noItem);
        }

        private static int GetGemTreeGemItem(int tileType)
        {
            switch (tileType)
            {
                case TileID.TreeTopaz: return ItemID.Topaz;
                case TileID.TreeAmethyst: return ItemID.Amethyst;
                case TileID.TreeSapphire: return ItemID.Sapphire;
                case TileID.TreeEmerald: return ItemID.Emerald;
                case TileID.TreeRuby: return ItemID.Ruby;
                case TileID.TreeDiamond: return ItemID.Diamond;
                case TileID.TreeAmber: return ItemID.Amber;
                default: return 0;
            }
        }

        #endregion

        #region 树木极速生长 & 摇树必掉水果

        private static bool Hook_AttemptToGrowTreeFromSapling(On_WorldGen.orig_AttemptToGrowTreeFromSapling orig, int x, int y, bool underground, int treeHeightAddon, bool ignoreWalls)
        {
            // 原版 AttemptToGrowTreeFromSapling 内部已包含 13+ 种树木（森林/针叶/丛林/腐化/猩红/神圣/棕榈/灰烬/樱花/黄柳/7种宝石树）的精确路由分发与粒子音效
            return orig(x, y, underground, treeHeightAddon, ignoreWalls);
        }

        private static void Hook_ShakeTree(On_WorldGen.orig_ShakeTree orig, int i, int j)
        {
            int beforeShakes = WorldGen.numTreeShakes;
            orig(i, j);

            if (!QoLValSet.treeShakeGuaranteeFruit.val) return;
            if (Main.netMode == 1) return; // 仅服务端或单人负责掉落生成

            // 只有当原版 ShakeTree 真正成功执行了有效摇树（计数自增，且未被跳过）时才保证掉落水果
            if (WorldGen.numTreeShakes <= beforeShakes) return;

            int fruitItem = GetTreeFruitItem(i, j);
            if (fruitItem > 0)
            {
                Vector2 dropPos = new Vector2(i * 16 + 8, j * 16 + 8);
                Item.NewItem(WorldGen.GetItemSource_FromTreeShake(i, j), dropPos, fruitItem, 1);
            }
        }

        private static int GetTreeFruitItem(int x, int y)
        {
            // 向下探测树根物块
            int groundY = y;
            while (groundY < Main.maxTilesY - 10 && Main.tile[x, groundY] != null && Main.tile[x, groundY].active() && TileID.Sets.IsShakeable[Main.tile[x, groundY].type])
            {
                groundY++;
            }
            Tile baseTile = Main.tile[x, groundY];
            if (baseTile == null || !baseTile.active()) return ItemID.Apple;

            int soilType = baseTile.type;
            if (soilType == TileID.SnowBlock || soilType == TileID.IceBlock)
            {
                return Main.rand.Next(2) == 0 ? ItemID.Cherry : ItemID.Plum;
            }
            if (soilType == TileID.Sand || soilType == TileID.Ebonsand || soilType == TileID.Crimsand || soilType == TileID.Pearlsand)
            {
                return Main.rand.Next(2) == 0 ? ItemID.Banana : ItemID.Coconut;
            }
            if (soilType == TileID.JungleGrass)
            {
                return Main.rand.Next(2) == 0 ? ItemID.Mango : ItemID.Pineapple;
            }
            if (soilType == TileID.CorruptGrass)
            {
                return Main.rand.Next(2) == 0 ? ItemID.Elderberry : ItemID.BlackCurrant;
            }
            if (soilType == TileID.CrimsonGrass)
            {
                return Main.rand.Next(2) == 0 ? ItemID.BloodOrange : ItemID.Rambutan;
            }
            if (soilType == TileID.HallowedGrass)
            {
                return Main.rand.Next(2) == 0 ? ItemID.Dragonfruit : ItemID.Starfruit;
            }
            if (soilType == TileID.AshGrass || soilType == TileID.Ash)
            {
                return Main.rand.Next(2) == 0 ? ItemID.SpicyPepper : ItemID.Pomegranate;
            }

            // 普通纯净森林树木
            int r = Main.rand.Next(5);
            switch (r)
            {
                case 0: return ItemID.Apple;
                case 1: return ItemID.Peach;
                case 2: return ItemID.Apricot;
                case 3: return ItemID.Grapefruit;
                default: return ItemID.Lemon;
            }
        }

        #endregion

        #region 移除墓地环境视觉与音乐

        private static void Hook_CalculateZones(On_SceneMetrics.orig_CalculateZones orig, SceneMetrics self)
        {
            orig(self);

            if (QoLValSet.removeGraveyardVisuals.val)
            {
                self.GraveyardTileCount = 0;
                self.ZoneGraveyard = false;
            }
        }

        #endregion
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    public class Patch_Ecology : EcologyHooks
    {
    }
}
