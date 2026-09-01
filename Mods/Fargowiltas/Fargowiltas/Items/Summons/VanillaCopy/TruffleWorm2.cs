using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace Fargowiltas.Items.Summons.VanillaCopy
{
    public class TruffleWorm2 : BaseSummon
    {

        public override int NPCType => NPCID.DukeFishron;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Truffly Worm");
			// Tooltip.SetDefault("Summons Duke Fishron without fishing");

			FargoSets.Items.SortingPriorityBossSpawns[Type] = FargoSets.Items.SortingPriorityBossSpawns[ItemID.TruffleWorm]; // 12
		}

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
               .AddIngredient(ItemID.TruffleWorm)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
