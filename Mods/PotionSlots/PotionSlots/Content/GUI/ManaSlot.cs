using System;
using PotionSlots.Core;
using Terraria;
using TPML.Content;

namespace PotionSlots.Content.GUI
{
    public class ManaSlot : PotionSlot
    {
        public override ref Item item => ref Main.LocalPlayer.GetModPlayer<PotionStoragePlayer>().manaSlot;
        public override Func<Item, bool> isValid => (Item item) => item.healMana > 0;
        public override string Texture => "PotionSlots/Assets/mana_sprite";
        public override string TextureFilled => "PotionSlots/Assets/manabg";
    }
}
