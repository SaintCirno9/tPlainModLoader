using Fargowiltas.Common.Systems.Recipes;
using Terraria;
using Terraria.ID;
using TPML.Content;

namespace Fargowiltas.Items.Summons.Deviantt
{
    public class HemoclawCrab : BaseSummon
    {
        public override int NPCType => NPCID.GoblinShark;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Hemoclaw Crab");
			/* Tooltip.SetDefault("Summons Hemogoblin Shark" +
                               "\nOnly usable during Blood Moon"); */

			FargoSets.Items.SortingPriorityBossSpawns[Type] = FargoSets.Items.SortingPriorityBossSpawns[ItemID.BloodMoonStarter]; // 18
		}

        public override bool CanUseItem(Player player)
        {
            return FargoUtils.ActuallyNight && Main.bloodMoon;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.BloodMoonStarter)
                .AddIngredient(ItemID.DeepRedPaint)
                .AddRecipeGroup(FargoRecipeGroups.AnyFoodT3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}
