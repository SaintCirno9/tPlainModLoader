using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Summons
{
    public class WormyFood : ModItem
    {

        
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Wormy Food");
			// Tooltip.SetDefault("Summons the Eater of Worlds in any biome");
		}

        
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 20;
            Item.value = Item.sellPrice(0, 0, 2);
            Item.rare = ItemRarityID.Blue;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
        }

        public override bool? UseItem(Player player)
        {
            return FargoSummonHelper.SummonBoss(player, NPCID.EaterofWorldsHead);
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.EaterofWorldsHead);

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
               .AddIngredient(ItemID.WormFood)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
