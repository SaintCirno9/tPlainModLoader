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
    /// 双轨黑曜石直通车 (Double Obsidian Instavator)
    /// 挖掘 11 格宽双通道垂直矿井直通地狱底层，配备双轨绳索与双侧/中央黑曜石护壁
    /// </summary>
    public class DoubleObsidianInstavator : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "双轨黑曜石直通车");
            ItemLoader.SetTooltip(Type, "投掷一枚高级重型炸弹，建造一条 11 格宽的双轨直通地狱通道\n双通道分别铺设中心绳索与黑曜石砖护壁，每隔 10 格配备照明火把\n不会破坏重要区域，风险自担");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 10;
        }

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 34;
            Item.maxStack = 99;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = 3; // Orange
            Item.UseSound = SoundID.Item1;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.autoReuse = false;
            Item.channel = false;
            Item.value = Item.buyPrice(0, 0, 8, 0);
            Item.noUseGraphic = true;
            Item.noMelee = true;
        }

        public override bool? CanUseItem(Player player)
        {
            return InstavatorShaftBuilder.CanUse(player);
        }


        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Vector2 target = Main.MouseWorld;
                if (InstavatorShaftBuilder.TryStartDoubleObsidianInstavator(player, target))
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
            tooltips.Add(new TooltipLine(Mod, "Detail", "[c/ff77ee:【双轨规格】] 宽度: 11 格 (双通道) | 深度: 直通地狱底层 | 双轨绳索与黑曜石全隔离护壁"));
        }

        public override void AddRecipes()
        {
            // 合成配方 1: 2 个地狱直通车
            int singleInstavatorId = ModContent.ItemType<Instavator>();
            if (singleInstavatorId > 0)
            {
                CreateRecipe(1)
                    .AddIngredient(singleInstavatorId, 2)
                    .AddTile(TileID.Anvils)
                    .Register();
            }

            // 合成配方 2: 原材料直接合成
            CreateRecipe(1)
                .AddIngredient(ItemID.Dynamite, 100)
                .AddIngredient(ItemID.ObsidianSkinPotion, 20)
                .AddIngredient(ItemID.Torch, 198)
                .AddIngredient(ItemID.FallenStar, 6)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
