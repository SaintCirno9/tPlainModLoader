using Fargowiltas.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TPML.Content;

namespace Fargowiltas.Items.Summons.SwarmSummons
{
    public class OverloadTwins : SwarmSummonBase
    {
        public OverloadTwins() : base(NPCID.Retinazer, nameof(OverloadTwins), 25, "MechEye")
        {
        }

        public override void SetStaticDefaults()
        {
			// DisplayName.SetDefault("Omnifocal Lens");
			// Tooltip.SetDefault("Summons several sets of Twins\nOnly Treasure Bags will be dropped");

			FargoSets.Items.SortingPriorityBossSpawns[Type] = FargoSets.Items.SortingPriorityBossSpawns[ItemID.MechanicalEye]; // 8
		}

        public override bool CanUseItem(Player player)
        {
            return !Fargowiltas.SwarmActive && FargoUtils.ActuallyNight;
        }
    }
}
