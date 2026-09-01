using System;
using PotionSlots.Core;
using Terraria;
using TPML.Content;

namespace PotionSlots.Content.GUI
{
    public class WormholeSlot : PotionSlot
    {
        public override ref Item item => ref Main.LocalPlayer.GetModPlayer<PotionStoragePlayer>().wormholeSlot;
        public override Func<Item, bool> isValid => (Item item) => item.type == 2997;
        public override string Texture => "PotionSlots/Assets/wormhole_sprite";
        public override string TextureFilled => "PotionSlots/Assets/wormholebg";
    }
}
