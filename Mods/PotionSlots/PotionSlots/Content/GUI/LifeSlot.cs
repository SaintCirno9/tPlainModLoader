using System;
using PotionSlots.Core;
using Terraria;
using Terraria.ModLoader;

namespace PotionSlots.Content.GUI
{
    public class LifeSlot : PotionSlot
    {
        public override ref Item item => ref Main.LocalPlayer.GetModPlayer<PotionStoragePlayer>().lifeSlot;
        public override Func<Item, bool> isValid => (Item item) => item.healLife > 0;
        public override string Texture => "PotionSlots/Assets/healing_sprite";
        public override string TextureFilled => "PotionSlots/Assets/healingbg";
    }
}
