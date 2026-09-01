using Terraria;
using Terraria.ID;

namespace Fargowiltas.Items.Summons.Abom
{
    public class SpookyBranch : BaseSummon
    {
        public override int NPCType => NPCID.MourningWood;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Spooky Branch");
			/* Tooltip.SetDefault("Summons Mourning Wood" +
                               "\nOnly usable at night"); */

			FargoSets.Items.SortingPriorityBossSpawns[Type] = FargoSets.Items.SortingPriorityBossSpawns[ItemID.PumpkinMoonMedallion]; // 14
		}

        public override bool CanUseItem(Player player) => FargoUtils.ActuallyNight;
    }
}
