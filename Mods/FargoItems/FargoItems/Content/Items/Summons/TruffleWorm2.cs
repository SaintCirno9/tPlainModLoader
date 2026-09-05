using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace FargoItems.Content.Items.Summons
{
    public class TruffleWorm2 : ModItem
    {

        
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Truffly Worm");
			// Tooltip.SetDefault("Summons Duke Fishron without fishing");
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
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
        }

        public override bool? UseItem(Player player)
        {
            return FargoSummonHelper.SummonBoss(player, NPCID.DukeFishron);
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.DukeFishron);

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
               .AddIngredient(ItemID.TruffleWorm)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
