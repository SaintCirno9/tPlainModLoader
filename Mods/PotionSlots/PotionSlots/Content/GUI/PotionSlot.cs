using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PotionSlots.Core.Loaders.UILoading;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TPML.Content;
using Terraria.UI;

namespace PotionSlots.Content.GUI
{
    public abstract class PotionSlot : SmartUIElement
    {
        public abstract ref Item item { get; }
        public abstract Func<Item, bool> isValid { get; }
        public abstract string Texture { get; }
        public abstract string TextureFilled { get; }

        public bool NoItem
        {
            get
            {
                if (item != null)
                {
                    return item.IsAir;
                }
                return true;
            }
        }

        public PotionSlot()
        {
            Width.Set(42f, 0f);
            Height.Set(42f, 0f);
        }

        public override void SafeClick(UIMouseEvent evt)
        {
            if (!Main.mouseItem.IsAir && isValid(Main.mouseItem) && NoItem)
            {
                item = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else if (Main.mouseItem.IsAir && !NoItem)
            {
                Main.mouseItem = item.Clone();
                item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else
            {
                if (Main.mouseItem.IsAir || !isValid(Main.mouseItem) || NoItem)
                {
                    return;
                }
                if (Main.mouseItem.type != item.type)
                {
                    Item mouseItem = item.Clone();
                    item = Main.mouseItem.Clone();
                    Main.mouseItem = mouseItem;
                }
                else
                {
                    int num = item.stack + Main.mouseItem.stack;
                    if (num > item.maxStack)
                    {
                        item.stack = item.maxStack;
                        Main.mouseItem.stack = num - item.maxStack;
                    }
                    else
                    {
                        item.stack = num;
                        Main.mouseItem.TurnToAir();
                    }
                }
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        public override void SafeRightClick(UIMouseEvent evt)
        {
            if (Main.mouseItem.IsAir && !NoItem)
            {
                Item obj = item.Clone();
                obj.stack = 1;
                Main.mouseItem = obj;
                item.stack--;
                if (item.stack <= 0)
                {
                    item.TurnToAir();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            else if (!Main.mouseItem.IsAir && !NoItem && Main.mouseItem.type == item.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
            {
                Main.mouseItem.stack++;
                item.stack--;
                if (item.stack <= 0)
                {
                    item.TurnToAir();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D value = ModContent.Request<Texture2D>(NoItem ? Texture : TextureFilled, AssetRequestMode.ImmediateLoad).Value;
            CalculatedStyle dimensions = GetDimensions();
            if (value != null)
            {
                spriteBatch.Draw(value, dimensions.ToRectangle(), Color.White);
            }

            if (!NoItem)
            {
                Main.inventoryScale = 31f / 52f;
                ref Item reference = ref item;
                dimensions = GetDimensions();
                ItemSlot.Draw(spriteBatch, ref reference, 21, dimensions.Position(), default(Color));
                if (IsMouseHovering)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = "a";
                }
            }

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }
    }
}
