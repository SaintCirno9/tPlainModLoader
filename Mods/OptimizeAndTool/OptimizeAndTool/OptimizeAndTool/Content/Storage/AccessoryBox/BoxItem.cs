using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 饰品箱物品格：标准容器交互
    /// 左键拿放换/堆叠，Shift+左键快速退回背包，右键半取/单放，完整悬停 Tooltip 与堆叠数字渲染
    /// 作者: SaintCirno9
    /// </summary>
    public class BoxItem : UIPanel
    {
        private readonly Item[] inv;
        private readonly int slot;

        public BoxItem(Item[] inv, int slot)
        {
            this.inv = inv;
            this.slot = slot;

            Width.Set(44, 0);
            Height.Set(44, 0);
            SetPadding(4);
            BackgroundColor = BorderColor = new Color(43, 60, 120);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            Item item = inv[slot];
            Item mouse = Main.mouseItem;

            if (ItemSlot.ShiftInUse)
            {
                if (item == null || item.IsAir) return;

                SoundEngine.PlaySound(SoundID.Grab);
                Item rest = Main.LocalPlayer.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                inv[slot] = rest ?? new Item();
                AccessoryBoxStorage.SaveNow();
                return;
            }

            if (mouse.IsAir)
            {
                if (item == null || item.IsAir) return;

                Main.mouseItem = item;
                inv[slot] = new Item();
            }
            else if (item == null || item.IsAir)
            {
                inv[slot] = mouse;
                Main.mouseItem = new Item();
            }
            else if (Item.CanStack(item, mouse) && item.stack < item.maxStack)
            {
                int move = Math.Min(item.maxStack - item.stack, mouse.stack);
                item.stack += move;
                mouse.stack -= move;
                if (mouse.stack <= 0) Main.mouseItem = new Item();
            }
            else
            {
                Main.mouseItem = item;
                inv[slot] = mouse;
            }

            SoundEngine.PlaySound(SoundID.Grab);
            AccessoryBoxStorage.SaveNow();
        }

        public override void RightClick(UIMouseEvent evt)
        {
            Item item = inv[slot];
            Item mouse = Main.mouseItem;

            if (mouse.IsAir)
            {
                if (item == null || item.IsAir) return;

                int take = (item.stack + 1) / 2;
                Item half = item.Clone();
                half.stack = take;
                item.stack -= take;
                if (item.stack <= 0) inv[slot] = new Item();
                Main.mouseItem = half;
            }
            else if (item == null || item.IsAir)
            {
                Item one = mouse.Clone();
                one.stack = 1;
                inv[slot] = one;
                mouse.stack--;
                if (mouse.stack <= 0) Main.mouseItem = new Item();
            }
            else if (Item.CanStack(item, mouse) && item.stack < item.maxStack)
            {
                item.stack++;
                mouse.stack--;
                if (mouse.stack <= 0) Main.mouseItem = new Item();
            }

            SoundEngine.PlaySound(SoundID.Grab);
            AccessoryBoxStorage.SaveNow();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            Item item = inv[slot];
            if (item == null || item.IsAir) return;

            CalculatedStyle dim = GetDimensions();

            Texture2D tex = Terraria.GameContent.TextureAssets.Item[item.type].Value;
            Rectangle frame = Main.itemAnimations[item.type]?.GetFrame(tex) ?? tex.Frame();

            float maxSide = Math.Max(frame.Width, frame.Height);
            float scale = maxSide > 32f ? 32f / maxSide : 1f;

            Vector2 origin = frame.Size() / 2f;
            Vector2 pos = new Vector2(dim.X + dim.Width / 2f, dim.Y + dim.Height / 2f);

            sb.Draw(tex, pos, frame, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);

            if (item.stack > 1)
            {
                string s = item.stack.ToString();
                Vector2 numOrigin = FontAssets.MouseText.Value.MeasureString(s) * new Vector2(1f, 1f);
                Vector2 numPos = new Vector2(dim.X + dim.Width - 3f, dim.Y + dim.Height - 3f);
                ChatManager.DrawColorCodedStringWithShadow(
                    sb,
                    FontAssets.MouseText.Value,
                    s,
                    numPos,
                    Color.White,
                    0f,
                    numOrigin,
                    new Vector2(0.75f)
                );
            }

            if (IsMouseHovering)
            {
                Main.HoverItem = item.Clone();
                Main.hoverItemName = item.Name;
            }
        }
    }
}
