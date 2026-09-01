using Fargowiltas.Items.Tiles;
using Fargowiltas.Tiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace Fargowiltas.Projectiles.Explosives
{
    public class SemiBridgifierProj : OmniBridgifierProj
    {
        protected override int TileHeight => 3;
        protected override int Placeable => ModContent.TileType<SemistationSheet>();
        protected override bool Replaceable(int TileType) => TileType == Placeable;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Semi-Bridgifier");
        }
    }
}
