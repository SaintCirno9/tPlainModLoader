using Terraria;
using TPML.Content;

namespace Fargowiltas.Content.Buffs
{
    public class WoodDrop : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }
    }
}
