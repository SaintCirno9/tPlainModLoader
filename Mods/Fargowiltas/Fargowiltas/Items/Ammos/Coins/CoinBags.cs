using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;

namespace Fargowiltas.Items.Ammos.Coins
{
    abstract class CoinBag : BaseAmmo
    {
        //public override string Texture => "Fargowiltas/Items/Placeholder";
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.notAmmo = false;
            Item.useStyle = ItemUseStyleID.None;
            Item.useTime = 0;
            Item.useAnimation = 0;
            Item.createTile = -1;
            Item.shoot = ProjectileID.None;
        }
    }

    class CopperCoinBag : CoinBag
    {
        public override int AmmunitionItem => ItemID.CopperCoin;
    }

    class SilverCoinBag : CoinBag
    {
        public override int AmmunitionItem => ItemID.SilverCoin;
    }

    class GoldCoinBag : CoinBag
    {
        public override int AmmunitionItem => ItemID.GoldCoin;
    }

    class PlatinumCoinBag : CoinBag
    {
        public override int AmmunitionItem => ItemID.PlatinumCoin;
    }
}
