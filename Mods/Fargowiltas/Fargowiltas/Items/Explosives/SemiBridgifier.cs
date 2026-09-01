using Fargowiltas.Items.Tiles;
using Fargowiltas.Projectiles.Explosives;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TPML.Content;

namespace Fargowiltas.Items.Explosives
{
    public class SemiBridgifier : OmniBridgifier
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Semi-Bridgifier");
            // Tooltip.SetDefault("Can be reused infinitely\nUpgrades the platform you are standing on to use Semistations\nDoes NOT build a platform!");
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<SemiBridgifierProj>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            position = player.Bottom + new Vector2(0, 8);
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<InstaBridge>())
                .AddTile(ModContent.TileType<SemistationSheet>())
                .Register();
        }
    }
}
