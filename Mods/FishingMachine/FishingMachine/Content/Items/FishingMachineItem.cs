using FishingMachine.Content.Tiles;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;

namespace FishingMachine.Content.Items
{
    /// <summary>
    /// 自动钓鱼机物品 (FishingMachine)
    /// 放置型机械物品，右键可打开专属交互界面进行全自动垂钓与战利品管理
    /// 作者: SaintCirno9
    /// </summary>
    public class FishingMachine : ModItem
    {
        public override string Texture => "FishingMachine/Resources/FishingMachine";

        public override void SetStaticDefaults()
        {
            ItemLoader.SetDisplayName(Type, "自动钓鱼机");
            ItemLoader.SetTooltip(Type, "放置在水池边，插入钓竿与鱼饵进行全自动垂钓\n右键打开机器交互界面进行钓具配置与战利品存取\n支持自动过滤筛选并向相邻宝箱输送渔获\n[c/00FFDD:可在界面中自由点击世界水域以选定钓点]");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = 99;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.rare = 3; // 橙色稀有度 (Orange)
            Item.value = Item.buyPrice(0, 1, 0, 0); // 1金币
            Item.createTile = ModContent.TileType<FishingMachineTile>();
            Item.UseSound = SoundID.Item1;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.IronBar, 8)
                .AddIngredient(ItemID.WoodFishingPole, 1)
                .AddIngredient(ItemID.Chest, 1)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe(1)
                .AddIngredient(ItemID.LeadBar, 8)
                .AddIngredient(ItemID.WoodFishingPole, 1)
                .AddIngredient(ItemID.Chest, 1)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
