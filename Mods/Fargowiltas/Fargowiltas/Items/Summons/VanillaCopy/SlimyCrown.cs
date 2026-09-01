using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TPML.Content;

namespace Fargowiltas.Items.Summons.VanillaCopy
{
    public class SlimyCrown : BaseSummon
    {

        public override int NPCType => NPCID.KingSlime;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Slimy Crown");
			// Tooltip.SetDefault("Summons King Slime");

			FargoSets.Items.SortingPriorityBossSpawns[Type] = FargoSets.Items.SortingPriorityBossSpawns[ItemID.SlimeCrown]; // 2
		}

        public override void AddRecipes()
        {
            CreateRecipe()
               .AddIngredient(ItemID.SlimeCrown)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
