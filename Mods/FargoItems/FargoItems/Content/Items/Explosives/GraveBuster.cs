using FargoItems.Content.Projectiles.Explosives;
using FargoItems.Content.Systems;

using Terraria;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Explosives
{
    public class GraveBuster : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Grave Buster");
            // Tooltip.SetDefault(@"Destroys tombstones in a large area");
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 10;
        }

        public override void SetDefaults()
        {
            Item.width = 11;
            Item.height = 11;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.value = Item.buyPrice(0, 0, 1);
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.Explosives.GraveBuster>();
            Item.shootSpeed = 5f;
        }

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
                .AddIngredient(ItemID.Dynamite, 5)
                .AddIngredient(ItemID.FallenStar, 1)
                .AddIngredient(Terraria.ID.ItemID.Tombstone)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
