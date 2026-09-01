using Terraria;
using Terraria.ID;

namespace Fargowiltas.Items.Summons.Abom
{
    public class IceKingsRemains : BaseSummon
    {
        public override int NPCType => NPCID.IceQueen;
        
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
			// DisplayName.SetDefault("Ice King's Remains");
			/* Tooltip.SetDefault("Summons Ice Queen" +
                               "\nOnly usable at night"); */

			FargoSets.Items.SortingPriorityBossSpawns[Type] = FargoSets.Items.SortingPriorityBossSpawns[ItemID.NaughtyPresent]; // 15
		}

        public override bool CanUseItem(Player player) => FargoUtils.ActuallyNight;
    }
}
