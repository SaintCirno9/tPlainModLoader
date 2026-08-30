using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PotionSlots.Core;
using PotionSlots.Core.Loaders.UILoading;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace PotionSlots.Content.GUI
{
    public class PotionSlotGui : SmartUIState
    {
        private const float BaseLeftPosition = 531f;
        private const float OffsetWithBankButtons = 62f;
        private const float OffsetWithoutBankButtons = 40f;

        private float currentLeftPosition;
        private LifeSlot life;
        private ManaSlot mana;
        private WormholeSlot wormhole;

        public override bool Visible => Main.playerInventory;

        public override int InsertionIndex(List<GameInterfaceLayer> layers)
        {
            return layers.FindIndex((GameInterfaceLayer layer) => layer.Name.Equals("Vanilla: Mouse Text"));
        }

        public override void OnInitialize()
        {
            life = new LifeSlot();
            mana = new ManaSlot();
            wormhole = new WormholeSlot();
            currentLeftPosition = BaseLeftPosition;
            if (ModCompatibility.IsBankButtonsLoaded)
            {
                currentLeftPosition += OffsetWithBankButtons;
            }
            else
            {
                currentLeftPosition += OffsetWithoutBankButtons;
            }

            SetSlotProperties(life, currentLeftPosition, 105f);
            SetSlotProperties(mana, currentLeftPosition, 138f);
            SetSlotProperties(wormhole, currentLeftPosition, 172f);
            Append(life);
            Append(mana);
            Append(wormhole);
        }

        private void SetSlotProperties(UIElement slot, float left, float top)
        {
            slot.Width.Set(31f, 0f);
            slot.Height.Set(31f, 0f);
            slot.Left.Set(left, 0f);
            slot.Top.Set(top, 0f);
        }

        public override void SafeUpdate(GameTime gameTime)
        {
            if (Main.LocalPlayer.controlHook)
            {
                RemoveAllChildren();
                OnInitialize();
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DynamicSpriteFont value = FontAssets.MouseText.Value;
            DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, value, "Potions", new Vector2(currentLeftPosition - 1f, 85f), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            base.Draw(spriteBatch);
        }
    }
}
