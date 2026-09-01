using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Vanity
{
    [AutoloadEquip(EquipType.Body)]
    public class AbominationnBody : ModItem
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Abominationn Body");
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
                .AddIngredient(ItemID.PirateShirt)
                .AddIngredient(ItemID.ChargedBlasterCannon)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}
