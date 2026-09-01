using FargoItems.Content.Projectiles.Explosives;
using FargoItems.Content.Systems;
using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Explosives
{
    public class BoomShuriken : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Boom Shuriken");
            // Tooltip.SetDefault("Rapid firing explosives\nUses your pickaxe's mining power\n'The fastest way to dig through anything is always to blow it up!'");
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 999;
        }

        public override void SetDefaults()
        {
            Item.width = 11;
            Item.height = 11;
            Item.damage = 10;
            Item.noMelee = true;
            Item.consumable = true;
            Item.noUseGraphic = true;
            Item.scale = 0.75f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.knockBack = 3f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Blue;
            Item.shoot = ModContent.ProjectileType<ShurikenProj>();
            Item.shootSpeed = 11f;
        }
        public override void HoldItem(Player player)
        {
            Item bestPick = player.GetBestPickaxe();
            if (bestPick != null)
            {
                float pickSpeed = bestPick.useTime;
                float playerSpeed = player.pickSpeed;
                float itemTime = pickSpeed * playerSpeed;
                if (itemTime <= 0)
                    itemTime = 1;
                int finalTime = (int)MathHelper.Clamp(itemTime, 1, 40);
                Item.useTime = finalTime;
                Item.useAnimation = finalTime;
            }
            else
            {
                Item.useTime = 40;
                Item.useAnimation = 40;
            }
        }
        public override void AddRecipes()
        {
            ModRecipe.Create(Type, 10)
                .AddIngredient(ItemID.Shuriken, 10)
                .AddIngredient(ItemID.Dynamite, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
