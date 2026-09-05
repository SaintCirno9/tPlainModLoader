using Terraria.Audio;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace FargoItems.Content.Items.Summons
{
    public class LihzahrdPowerCell2 : ModItem
    {
        
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Lihzahrd Battery Pack");
			// Tooltip.SetDefault("Summons the Golem without an altar");
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
            return FargoSummonHelper.SummonBoss(player, NPCID.Golem);
        }

        public override bool CanUseItem(Player player)
        {
            return NPC.downedPlantBoss && !NPC.AnyNPCs(NPCID.Golem);
        }

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
               .AddIngredient(ItemID.LihzahrdPowerCell)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
