using System.Collections.Generic;
using Instavator.Content.Logic;
using Instavator.Content.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace Instavator.Content.Items
{
    /// <summary>
    /// 地狱直通车 (Instavator)
    /// 挖掘 7 格宽直通地狱底部的通道，自动铺设黑曜石砖外壁、火把照明与中心绳索
    /// </summary>
    public class Instavator : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "地狱直通车");
            ItemLoader.SetTooltip(Type, "投掷一枚炸弹，建造一条直通地狱的通道\n自动铺设黑曜石砖护壁、照明火把与中心绳索\n不会破坏重要区域，风险自担");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 10;
        }

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.height = 32;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = 2; // Green
            Item.UseSound = SoundID.Item1;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.autoReuse = false;
            Item.channel = false;
            Item.value = Item.buyPrice(0, 0, 3, 0);
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = 1;
            Item.shootSpeed = 5f;
        }

        public override bool? CanUseItem(Player player)
        {
            return InstavatorShaftBuilder.CanUse(player);
        }

        public override void HoldItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer && !InstavatorShaftBuilder.IsBuildRunning)
            {
                InstaVisualSystem.RequestVisual(Main.MouseWorld, 7, 2000, new Color(255, 140, 40));
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Vector2 target = Main.MouseWorld;
                InstavatorShaftBuilder.BuildFullInstavator(player, target);
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Detail", "[c/ffaa44:【挖掘规格】] 宽度: 7 格 | 深度: 直通地狱底层 (maxTilesY - 40)"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.Dynamite, 50)
                .AddIngredient(ItemID.ObsidianSkinPotion, 10)
                .AddIngredient(ItemID.Torch, 99)
                .AddIngredient(ItemID.FallenStar, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
