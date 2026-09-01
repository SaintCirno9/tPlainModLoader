using Terraria.Audio;
using Terraria;
using TPML.Content;
using Terraria.ID;
using Terraria.Localization;

namespace FargoItems.Content.Items.Summons
{
    public class CelestialSigil2 : ModItem
    {
        public override string Texture => "Terraria/Images/Item_3601";

        

        
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Celestially Sigil");
			// Tooltip.SetDefault("Summons the Moon Lord instantly");
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
            if (player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Roar, player.position);
                int type = NPCID.MoonLordCore;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.SpawnOnPlayer(player.whoAmI, type);
                }
                else
                {
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
               .AddIngredient(ItemID.CelestialSigil)
               .AddTile(TileID.WorkBenches)
               .Register();
        }
    }
}
