//using Terraria.ID;
//using Terraria.ModLoader;

//namespace Fargowiltas.Items.Tiles
//{
//    public class OmnistationPlus : ModItem
//    {
//        public override bool Autoload(ref string name)
//        {
//            return null != null || null != null;
//        }
//        public override void SetStaticDefaults()
//        {
//            DisplayName.SetDefault("Omnistation+");
//            Tooltip.SetDefault(@"Effects of all buff stations, modded and vanilla
//Grants Honey when touched
//Right click while holding a weapon for its respective buff
//Currently only supports Thorium and Calamity");
//        }

//        public override void SetDefaults()
//        {
//            Item.width = 20;
//            Item.height = 20;
//            Item.useTurn = true;
//            Item.autoReuse = true;
//            Item.useAnimation = 15;
//            Item.useTime = 10;
//            Item.useStyle = ItemUseStyleID.Swing;
//            Item.rare = ItemRarityID.Blue;
//            Item.createTile = mod.TileType("OmnistationPlusSheet");
//        }

//        public override void AddRecipes()
//        {


//            ModRecipe recipe = new ModRecipe(mod);
//            recipe.AddRecipeGroup("Fargowiltas:AnyOmnistation", 1);
//            if (Fargowiltas.ModLoaded["ThoriumMod"])
//            {
//                Mod thorium = null;
//                recipe.AddIngredient(thorium.ItemType("Mistletoe"), 30);
//                recipe.AddIngredient(thorium.ItemType("ConductorsStand"), 5);
//                recipe.AddIngredient(thorium.ItemType("Altar"), 5);
//                recipe.AddIngredient(thorium.ItemType("NinjaRack"), 5);
//            }
//            if (Fargowiltas.ModLoaded["CalamityMod"])
//            {
//                Mod calamity = null;
//                recipe.AddIngredient(calamity.ItemType("PurpleCandle"), 5);
//                recipe.AddIngredient(calamity.ItemType("YellowCandle"), 5);
//                recipe.AddIngredient(calamity.ItemType("PinkCandle"), 5);
//                recipe.AddIngredient(calamity.ItemType("BlueCandle"), 5);

//            }

//            recipe.AddTile(TileID.MythrilAnvil);
//            recipe.SetResult(this);
//            recipe.AddRecipe();
//        }
//    }
//}
