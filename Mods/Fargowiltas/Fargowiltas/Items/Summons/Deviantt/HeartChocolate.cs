using Fargowiltas.Common.Systems.Recipes;
using Terraria;
using Terraria.ID;
using TPML.Content;

namespace Fargowiltas.Items.Summons.Deviantt
{
    public class HeartChocolate : BaseSummon
    {
        public override int NPCType => NPCID.Nymph;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Heart Chocolate");
			/* Tooltip.SetDefault("Summons Nymph" +
                               "\nOnly usable at night or underground"); */

			FargoSets.Items.SortingPriorityBossSpawns[Type] = 0; // Places it before any other bosses
		}

        public override bool CanUseItem(Player player)
        {
            return FargoUtils.ActuallyNight || player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight || player.ZoneUnderworldHeight;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                  .AddIngredient(ItemID.LifeCrystal)
                  .AddRecipeGroup(FargoRecipeGroups.AnyFoodT2)
                  .AddTile(TileID.CookingPots)
                  .Register();
        }
    }
}
