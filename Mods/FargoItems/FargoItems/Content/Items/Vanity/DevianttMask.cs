using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Items.Vanity
{
    [AutoloadEquip(EquipType.Head)]
    public class DevianttMask : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Deviantt Mask");
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Blue;
            Item.vanity = true;
        }

        public override void AddRecipes()
        {
            ModRecipe.Create(Type)
                .AddIngredient(ItemID.RuneHat)
                .AddIngredient(ItemID.MetalDetector)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}
