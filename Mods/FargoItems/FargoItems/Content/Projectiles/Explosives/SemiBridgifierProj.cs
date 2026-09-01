using FargoItems.Content.Items.Explosives;


using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;

namespace FargoItems.Content.Projectiles.Explosives
{
    public class SemiBridgifierProj : OmniBridgifierProj
    {
        protected override int TileHeight => 3;
        protected override int Placeable => ModContent.TileType("Fargowiltas", "SemistationSheet");
        protected override bool Replaceable(int TileType) => TileType == Placeable;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Semi-Bridgifier");
        }
    }
}
