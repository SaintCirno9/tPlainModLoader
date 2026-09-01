using System.Collections.Generic;
using Instavator.Content.Logic;
using Instavator.Content.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;
using TPML.Content.UI;

namespace Instavator.Content.Items
{
    /// <summary>
    /// 半程直通车 (Half Instavator)
    /// 挖掘 5 格宽垂直矿井至岩石层半程深度，自动放置中心绳索
    /// </summary>
    public class HalfInstavator : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "半程地狱直通车");
            ItemLoader.SetTooltip(Type, "投掷一枚炸弹，建造一条通往地下的半程矿井\n挖掘至岩石层半程深度并铺设中心绳索\n不会破坏重要区域，风险自担");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.height = 32;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = 1; // Blue
            Item.UseSound = SoundID.Item1;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.autoReuse = false;
            Item.channel = false;
            Item.value = Item.buyPrice(0, 0, 1, 50);
            Item.noUseGraphic = true;
            Item.noMelee = true;
        }

        public override bool CanUseItem(Player player)
        {
            return InstavatorShaftBuilder.CanUse(player);
        }


        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Vector2 target = Main.MouseWorld;
                if (InstavatorShaftBuilder.TryStartHalfInstavator(player, target))
                {
                    if (Item.consumable)
                    {
                        Item.stack--;
                        if (Item.stack <= 0)
                        {
                            Item.TurnToAir();
                        }
                    }
                    return true;
                }
                return false;
            }
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Detail", "[c/55ccff:【挖掘规格】] 宽度: 5 格 | 深度: 岩石层半程"));
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.Dynamite, 25)
                .AddIngredient(ItemID.ObsidianSkinPotion, 5)
                .AddIngredient(ItemID.Torch, 50)
                .AddIngredient(ItemID.FallenStar, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
