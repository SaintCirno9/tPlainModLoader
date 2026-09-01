using Terraria;
using Terraria.GameContent.Personalities;
using TPML.Content;

namespace Fargowiltas.Content.Biomes
{
    public class SkyBiome : AShoppingBiome, ILoadable
    {
        public SkyBiome()
        {
            NameKey = "Mods.Fargowiltas.Biome.Sky";
        }

        public override bool IsInBiome(Player player) => player.ZoneSkyHeight;

        public void Load(Mod mod)
        {
        }

        public void Unload()
        {
        }

        public bool IsLoadingEnabled(Mod mod) => true;
    }
}
