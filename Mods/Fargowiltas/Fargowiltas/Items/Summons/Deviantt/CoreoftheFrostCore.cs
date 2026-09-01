using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace Fargowiltas.Items.Summons.Deviantt
{
    public class CoreoftheFrostCore : BaseSummon
    {
        public override int NPCType => NPCID.IceGolem;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Core of the Frost Core");
            // Tooltip.SetDefault("Summons Ice Golem");
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 6));
            ItemID.Sets.AnimatesAsSoul[Type] = true;

			FargoSets.Items.SortingPriorityBossSpawns[Type] = 6; // Places it right after Gelatin Crystal
		}
    }
}
