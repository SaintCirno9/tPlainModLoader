using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Misc
{
	public class KohaCrystal : ModItem
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
            ModRecipe.Create(Type)
                .AddIngredient(ItemID.ManaCrystal)
                .AddCondition(Condition.NearShimmer) 
                .Register();
        }

        public override void HoldItem(Player player)
        {
            Item.UseSound = SoundID.Item1;
        }

        public override bool? UseItem(Player player)
        {
            if (player.statManaMax > 20)
            {
                player.statManaMax -= 20;
                player.statManaMax2 -= 20;
                if (player.statMana > player.statManaMax2)
                    player.statMana = player.statManaMax2;
                SoundEngine.PlaySound(SoundID.Item29, player.position);
                if (Main.netMode == NetmodeID.MultiplayerClient && player.whoAmI == Main.myPlayer)
                {
                    NetMessage.SendData(42, -1, -1, null, player.whoAmI);
                }
                return true;
            }
            return false;
        }

        public override bool CanUseItem(Player player)
        {
            return player.statManaMax > 20;
        }
    }
}
