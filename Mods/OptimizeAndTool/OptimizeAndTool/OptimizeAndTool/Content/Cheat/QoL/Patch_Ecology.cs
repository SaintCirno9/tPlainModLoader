using HarmonyLib;
using Microsoft.Xna.Framework;
using tContentPatch;
using Terraria;
using Terraria.ID;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 生态与植被增强（草药极速生长/开花/补种、树木生长/掉果/宝石树掉宝石、移除墓地暗角与音乐）
    /// 作者: SaintCirno9
    /// </summary>
    public class Patch_Ecology : PatchMain
    {
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
        }
    }

    [HarmonyPatch]
    internal static class Patch_EcologyHarmony
    {
        #region 草药极速生长与任意时刻开花
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.GrowAlch))]
        public static bool WorldGen_GrowAlch_Prefix(int x, int y)
        {
            if (!QoLValSet.herbFastGrow.val) return true;
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return true;

            Tile tile = Main.tile[x, y];
            if (tile == null || !tile.active()) return true;

            if (tile.type == TileID.ImmatureHerbs || tile.type == TileID.MatureHerbs)
            {
                tile.type = TileID.BloomingHerbs;
                if (Main.netMode == 2) NetMessage.SendTileSquare(-1, x, y, 1);
                else if (Main.netMode == 1) NetMessage.SendTileSquare(Main.myPlayer, x, y, 1);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.IsHarvestableHerbWithSeed))]
        public static bool WorldGen_IsHarvestableHerbWithSeed_Prefix(int type, int style, ref bool __result)
        {
            if (QoLValSet.herbBloomAnytime.val)
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.IsAlchemyPlantHarvestable))]
        public static bool WorldGen_IsAlchemyPlantHarvestable_Prefix(int style, int y, ref bool __result)
        {
            if (QoLValSet.herbBloomAnytime.val)
            {
                __result = true;
                return false;
            }
            return true;
        }
        #endregion

        #region 再生法杖自动补种 & 宝石树全段掉宝石
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.KillTile))]
        public static void WorldGen_KillTile_Prefix(int i, int j, bool fail, bool effectOnly, bool noItem)
        {
            if (fail || effectOnly || noItem) return;
            if (i < 0 || i >= Main.maxTilesX || j < 0 || j >= Main.maxTilesY) return;

            Tile tile = Main.tile[i, j];
            if (tile == null || !tile.active()) return;

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
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.AttemptToGrowTreeFromSapling))]
        public static bool WorldGen_AttemptToGrowTreeFromSapling_Prefix(int x, int y, bool underground, ref bool __result)
        {
            if (QoLValSet.treeFastGrow.val)
            {
                __result = WorldGen.GrowTree(x, y);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.ShakeTree))]
        public static void WorldGen_ShakeTree_Prefix(out int __state)
        {
            // 记录执行前的摇树计数
            __state = WorldGen.numTreeShakes;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldGen), nameof(WorldGen.ShakeTree))]
        public static void WorldGen_ShakeTree_Postfix(int i, int j, int __state)
        {
            if (!QoLValSet.treeShakeGuaranteeFruit.val) return;
            if (Main.netMode == 1) return; // 仅服务端或单人负责掉落生成

            // 只有当原版 ShakeTree 真正成功执行了有效摇树（计数自增，且未被跳过）时才保证掉落水果
            if (WorldGen.numTreeShakes <= __state) return;

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
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneMetrics), "get_EnoughTilesForGraveyard")]
        public static bool SceneMetrics_get_EnoughTilesForGraveyard_Prefix(ref bool __result)
        {
            if (QoLValSet.removeGraveyardVisuals.val)
            {
                __result = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SceneMetrics), "get_GraveyardTileCount")]
        public static bool SceneMetrics_get_GraveyardTileCount_Prefix(ref int __result)
        {
            if (QoLValSet.removeGraveyardVisuals.val)
            {
                __result = 0;
                return false;
            }
            return true;
        }
        #endregion
    }
}
