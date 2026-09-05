using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace FargoItems.Content.Items.Summons
{
    public class MechSkull : ModItem
    {

        
        
        

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Some Kind of Metallic Skull");
			// Tooltip.SetDefault("Summons Skeletron Prime");
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
            return FargoSummonHelper.SummonBoss(player, NPCID.SkeletronPrime);
        }

        public override bool CanUseItem(Player player) => !Main.dayTime && !NPC.AnyNPCs(NPCID.SkeletronPrime);

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
               .AddIngredient(ItemID.MechanicalSkull)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
