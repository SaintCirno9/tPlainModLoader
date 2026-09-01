using Terraria;
using Terraria.DataStructures;
using TPML.Content;
using Terraria.ID;

namespace Fargowiltas.Items.Summons.SwarmSummons
{
    public class Overloader : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Overloader");
            /* Tooltip.SetDefault("Used to craft swarm summons\n" +
                               "When used, all summons in the stack will be consumed\n" +
                               "The more consumed, the more bosses will spawn\n" +
                               "A Trophy will drop every 10 kills\n" +
                               "An Energizer will drop every 100 kills"); */
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 4));
            Terraria.ID.ItemID.Sets.AnimatesAsSoul[Type] = true;
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 3;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Green;
        }
    }
}
