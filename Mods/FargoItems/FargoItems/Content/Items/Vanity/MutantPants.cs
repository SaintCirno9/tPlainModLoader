using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Vanity
{
    [AutoloadEquip(EquipType.Legs)]
    public class MutantPants : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Mutant Pants");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
                .AddIngredient(ItemID.BeeMask)
                .AddIngredient(ItemID.PlanteraMask)
                .AddIngredient(ItemID.KingSlimeMask)
                .AddIngredient(ItemID.QueenSlimeMask)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}
