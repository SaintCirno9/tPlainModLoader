using Fargowiltas.Content.Buffs;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TPML.Content;
using static TPML.Content.ModContent;

namespace Fargowiltas.Items.Weapons
{
    public class LumberJaxe : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("The Lumber Jaxe");
            /* Tooltip.SetDefault("Hit enemies may drop wood when killed" +
                               "\n'The former weapon of a true axe wielding hero'"); */
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 15;
            Item.melee = true;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.axe = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = 5000;
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            target.AddBuff(BuffType<WoodDrop>(), 600);
        }
    }
}
