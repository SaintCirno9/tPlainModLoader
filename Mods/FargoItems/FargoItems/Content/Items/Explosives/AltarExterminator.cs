using FargoItems.Content.Projectiles.Explosives;
using FargoItems.Content.Systems;

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Explosives
{
    public class AltarExterminator : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Altar Exterminator");
            // Tooltip.SetDefault("Destroys all natural Demon Altars and Crimson Altars");
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
        }

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.height = 32;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item1;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.value = Item.buyPrice(0, 0, 3);
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AltarExterminatorProj>();
        }

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
                .AddIngredient(ItemID.DemoniteBar, 10)
                .AddIngredient(ItemID.ShadowScale, 5)
                .AddIngredient(ItemID.Pwnhammer)
                .AddTile(TileID.Anvils)
                .Register();

            ModRecipe.Create(Type)
                .AddIngredient(ItemID.CrimtaneBar, 10)
                .AddIngredient(ItemID.TissueSample, 5)
                .AddIngredient(ItemID.Pwnhammer)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
