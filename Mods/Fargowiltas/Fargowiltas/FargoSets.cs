using Fargowiltas.Items.Misc;
using Fargowiltas.Items.Tiles;
using Fargowiltas.NPCs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using TPML.Content;
using static Fargowiltas.Items.FargoGlobalItem;
using static TPML.Content.ModContent;

namespace Fargowiltas
{
    public class FargoSets : ModSystem
    {
        public class Items
        {
            public static bool[] MechanicalAccessory;
            public static bool[] InfoAccessory;
            public static bool[] SquirrelSellsDirectly;

            public static bool[] NonBuffPotion;
            public static bool[] PotionCannotBeInfinite;
            public static bool[] BuffStation;
            public static List<ShopTooltip>[] RegisteredShopTooltips;
            public static int[] SortingPriorityBossSpawns = new int[8000];
        }
        public class Tiles
        {
            public static bool[] InstaCannotDestroy;
            public static bool[] DungeonTile;
            public static bool[] HardmodeOre;
            public static bool[] EvilAltars;
        }
        public class Walls
        {
            public static bool[] InstaCannotDestroy;
            public static bool[] DungeonWall;
        }
        public class NPCs
        {
            public static int[] SwarmHealth;
            public static Dictionary<int, bool>[] SpecificDebuffImmunity = new Dictionary<int, bool>[8000];
        }

        public override void PostSetupContent()
        {
            #region Items
            SetFactory itemFactory = ItemID.Sets.Factory;

            Items.MechanicalAccessory = itemFactory.CreateBoolSet(false,
                ItemID.MechanicalLens,
                ItemID.WireKite,
                //ItemID.Ruler,
                ItemID.LaserRuler,
                ItemID.PaintSprayer,
                ItemID.ArchitectGizmoPack,
                ItemID.HandOfCreation,
                ItemID.ActuationAccessory,
                ItemID.EncumberingStone,
                ItemID.DontHurtCrittersBook,
                ItemID.DontHurtComboBook,
                ItemID.DontHurtNatureBook,
                ItemID.LucyTheAxe);

            Items.InfoAccessory = itemFactory.CreateBoolSet(false,
                ItemID.CopperWatch,
                ItemID.TinWatch,
                ItemID.SilverWatch,
                ItemID.TungstenWatch,
                ItemID.GoldWatch,
                ItemID.PlatinumWatch,
                ItemID.Compass,
                ItemID.DepthMeter,
                ItemID.GPS,
                ItemID.PDA,
                ItemID.CellPhone,
                5358,
                5359,
                5360,
                5361,
                ItemID.GoblinTech,
                ItemID.DPSMeter,
                ItemID.MetalDetector,
                ItemID.Stopwatch,
                ItemID.LifeformAnalyzer,
                ItemID.FishermansGuide,
                ItemID.WeatherRadio,
                ItemID.Sextant,
                ItemID.Radar,
                ItemID.TallyCounter,
                ItemID.FishFinder,
                ItemID.REK);

            Items.SquirrelSellsDirectly = CreateSafeBoolSet(false,
                ItemID.CellPhone,
                ItemID.Shellphone,
                ItemID.ShellphoneDummy,
                ItemID.ShellphoneHell,
                ItemID.ShellphoneOcean,
                ItemID.ShellphoneSpawn,
                ItemID.AnkhShield,
                ItemID.RodofDiscord,
                ItemID.TerrasparkBoots,
                ItemID.TorchGodsFavor,
                ItemID.HandOfCreation,
                ItemID.Zenith,
                ItemType<Omnistation>(),
                ItemType<Omnistation2>(),
                ItemType<CrucibleCosmos>(),
                ItemType<ElementalAssembler>(),
                ItemType<MultitaskCenter>(),
                ItemType<PortableSundial>(),
                ItemType<BattleCry>());

            Items.NonBuffPotion = CreateSafeBoolSet(false,
                ItemID.RecallPotion,
                ItemID.PotionOfReturn,
                ItemID.WormholePotion,
                ItemID.TeleportationPotion,
                ItemType<BigSuckPotion>());

            Items.PotionCannotBeInfinite = itemFactory.CreateBoolSet(false,
                ItemID.BottledHoney);

            Items.BuffStation = itemFactory.CreateBoolSet(false,
                ItemID.SharpeningStation,
                ItemID.AmmoBox,
                ItemID.CrystalBall,
                ItemID.BewitchingTable,
                ItemID.WarTable);

            Items.RegisteredShopTooltips = itemFactory.CreateCustomSet<List<ShopTooltip>>(null);
            #endregion
            #region Tiles
            SetFactory tileFactory = TileID.Sets.Factory;

            Tiles.InstaCannotDestroy = tileFactory.CreateBoolSet(false);

            Tiles.DungeonTile = tileFactory.CreateBoolSet(false,
                TileID.BlueDungeonBrick,
                TileID.GreenDungeonBrick,
                TileID.PinkDungeonBrick);

            Tiles.HardmodeOre = tileFactory.CreateBoolSet(false,
                TileID.Cobalt,
                TileID.Palladium,
                TileID.Mythril,
                TileID.Orichalcum,
                TileID.Adamantite,
                TileID.Titanium);

            Tiles.EvilAltars = tileFactory.CreateBoolSet(false, 
                TileID.DemonAltar);
            #endregion
            #region Walls
            SetFactory wallFactory = WallID.Sets.Factory;

            Walls.InstaCannotDestroy = wallFactory.CreateBoolSet(false);

            Walls.DungeonWall = wallFactory.CreateBoolSet(false,
                WallID.BlueDungeonSlabUnsafe, 
                WallID.BlueDungeonTileUnsafe, 
                WallID.BlueDungeonUnsafe, 
                WallID.GreenDungeonSlabUnsafe, 
                WallID.GreenDungeonTileUnsafe, 
                WallID.GreenDungeonUnsafe, 
                WallID.PinkDungeonSlabUnsafe, 
                WallID.PinkDungeonTileUnsafe, 
                WallID.PinkDungeonUnsafe);
            #endregion
            #region NPCs
            SetFactory npcFactory = NPCID.Sets.Factory;

            NPCs.SwarmHealth = npcFactory.CreateIntSet(0);
            #endregion
        }

        private static bool[] CreateSafeBoolSet(bool defaultState, params int[] types)
        {
            int max = 8000;
            if (types != null && types.Length > 0)
            {
                int maxT = types.Max();
                if (maxT >= max) max = maxT + 100;
            }
            bool[] set = new bool[max];
            if (defaultState)
            {
                for (int i = 0; i < set.Length; i++) set[i] = true;
            }
            if (types != null)
            {
                foreach (int t in types)
                {
                    if (t >= 0 && t < set.Length)
                        set[t] = !defaultState;
                }
            }
            return set;
        }
    }
}
