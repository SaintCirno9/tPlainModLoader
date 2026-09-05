using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Summons
{
    public class SlimyCrown : ModItem
    {

        
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Slimy Crown");
			// Tooltip.SetDefault("Summons King Slime");
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
            return FargoSummonHelper.SummonBoss(player, NPCID.KingSlime);
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.KingSlime);

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
               .AddIngredient(ItemID.SlimeCrown)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
