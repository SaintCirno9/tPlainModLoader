using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace Fargowiltas.Items.Summons.VanillaCopy
{
    public class GoreySpine : BaseSummon
    {

        public override int NPCType => NPCID.BrainofCthulhu;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Red Stained Spine");
			// Tooltip.SetDefault("Summons the Brain of Cthulhu in any biome");

			FargoSets.Items.SortingPriorityBossSpawns[Type] = FargoSets.Items.SortingPriorityBossSpawns[ItemID.BloodySpine]; // 3
		}

        public override void AddRecipes()
        {
            CreateRecipe()
               .AddIngredient(ItemID.BloodySpine)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
