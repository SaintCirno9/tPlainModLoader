using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UIQueryItemSlot : UIItemSlot
    {
        public bool real;
        public string emptyHintText;

        public UIQueryItemSlot(Item item) : base(item, 0.85f)
        {
        }

        public void ReplaceWithFake(int type)
        {
            if (real && item != null && !item.IsAir)
            {
                Main.LocalPlayer.QuickSpawnItem(null, item.type, item.stack);
            }
            real = false;
            item = new Item();
            if (type > 0)
            {
                item.SetDefaults(type);
            }
            if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
            if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (Main.mouseItem != null && !Main.mouseItem.IsAir)
            {
                if (real && item != null && !item.IsAir)
                {
                    Main.LocalPlayer.QuickSpawnItem(null, item.type, item.stack);
                }
                real = true;
                item = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
            else if (real && item != null && !item.IsAir)
            {
                Main.mouseItem = item.Clone();
                item.TurnToAir();
                real = false;
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
            else if (!real && item != null && !item.IsAir)
            {
                item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
        }

        public override void RightClick(UIMouseEvent evt)
        {
            if (item != null && !item.IsAir)
            {
                if (real)
                {
                    Main.LocalPlayer.QuickSpawnItem(null, item.type, item.stack);
                }
                item.TurnToAir();
                real = false;
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (item.IsAir && IsMouseHovering && !string.IsNullOrEmpty(emptyHintText))
            {
                UICommon.TooltipMouseText(emptyHintText);
            }
        }
    }

    public class UIRecipeCatalogueQueryItemSlot : UIQueryItemSlot
    {
        public int CanonicalItemType => item?.type ?? 0;

        public UIRecipeCatalogueQueryItemSlot(Item item) : base(item)
        {
        }
    }

    public class UICraftQueryItemSlot : UIQueryItemSlot
    {
        public UICraftQueryItemSlot(Item item) : base(item)
        {
        }
    }

    public class UIBestiaryQueryItemSlot : UIQueryItemSlot
    {
        public UIBestiaryQueryItemSlot(Item item) : base(item)
        {
        }
    }
}
