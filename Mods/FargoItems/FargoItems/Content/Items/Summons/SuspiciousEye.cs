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
    public class SuspiciousEye : ModItem
    {
       // public override string Texture => "Terraria/Images/Item_43";

        
        
        

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Eye That Could Be Seen As Suspicious");
			// Tooltip.SetDefault("Summons the Eye of Cthulhu");
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
            return FargoSummonHelper.SummonBoss(player, NPCID.EyeofCthulhu);
        }

        public override bool CanUseItem(Player player) => !Main.dayTime && !NPC.AnyNPCs(NPCID.EyeofCthulhu);

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
               .AddIngredient(ItemID.SuspiciousLookingEye)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
