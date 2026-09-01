using FargoItems.Content.Projectiles.Explosives;
using FargoItems.Content.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Explosives
{
    public class AutoHouse : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("InstaHouse");
            // Tooltip.SetDefault("Places an NPC house instantly");
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
            Item.UseSound = SoundID.Item14;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.value = Item.buyPrice(0, 0, 3);
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<AutoHouseProj>();
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Vector2 mouse = Main.MouseWorld + Vector2.UnitY * 16;
                InstaVisual.DrawOrigin origin = mouse.X - player.Center.X > 0 ? InstaVisual.DrawOrigin.BottomLeft : InstaVisual.DrawOrigin.BottomRight;
                InstaVisual.DrawInstaVisual(player, mouse, new(10, 6), origin);
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 mouse = Main.MouseWorld;
            var logger = TPML.Core.Logging.LogManager.GetLogger("AutoHouse");
            logger.Info($"[AutoHouse] 玩家 [{player.name}] 触发 Shoot, 鼠标目标: ({mouse.X:F1}, {mouse.Y:F1}), 弹幕类型: {type}");
            Projectile.NewProjectile(player.GetSource_ItemUse(source.Item), mouse, Vector2.Zero, type, 0, 0, player.whoAmI);
            return false;
        }

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
                .AddRecipeGroup("Wood", 50)
                .AddIngredient(ItemID.Torch)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
