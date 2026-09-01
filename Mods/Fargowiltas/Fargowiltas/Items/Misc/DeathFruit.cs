using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using TPML.Content;

namespace Fargowiltas.Items.Misc
{
	public class DeathFruit : ModItem
	{
		public override void SetStaticDefaults()
		{
		}

		public override void SetDefaults()
		{
			Item.width = 18;
			Item.height = 18;
			Item.maxStack = 99;
			Item.rare = ItemRarityID.Blue;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.consumable = true;
			Item.UseSound = SoundID.Item1;
		}

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LifeFruit)
                .AddCondition(Condition.NearShimmer) 
                .Register();
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
		{
            if (!CanUse(player) && player.altFunctionUse != 2)
            {
                return false;
            }
            if (!CanUse(player, true) && player.altFunctionUse == 2)
            {
                return false;
            }
			return true;
		}

        public override void HoldItem(Player player)
        {
            Item.UseSound = SoundID.Item1;
        }

        public override bool? UseItem(Player player)
        {
            if (player.statLifeMax > 400)
            {
                player.statLifeMax -= 5;
                return true;
            }
            else if (player.statLifeMax > 100)
            {
                player.statLifeMax -= 20;
                return true;
            }
            return false;
        }

        private bool CanUse(Player player, bool rightClick = false)
        {
            if (!rightClick && GetLife(player) > 20)
            {
                return true;
            }
            if (rightClick && player.GetModPlayer<FargoPlayer>().DeathFruitHealth > 0)
            {
                return true;
            }
            return false;
        }

        private int GetLife(Player player) => player.statLifeMax - (((player.statLifeMax - 400) / 5) * 5);
    }
}
