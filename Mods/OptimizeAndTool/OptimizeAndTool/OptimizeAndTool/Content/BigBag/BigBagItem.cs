using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 巨大背包物品格（与饰品箱同构的事件驱动模式）
    /// 左键拿起/放下/交换/堆叠，Shift+左键快速转移回背包，右键半取/单个放
    /// 作者: SaintCirno9
    /// </summary>
    internal class BigBagItem : UIPanel
    {
        private readonly Item[] inv;
        private readonly int slot;

        public BigBagItem(Item[] inv, int slot)
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

            // Shift+左键: 快速转移回背包，放不下的留在格内
            if (ItemSlot.ShiftInUse)
            {
                if (item.IsAir) return;

                SoundEngine.PlaySound(SoundID.Grab);
                Item rest = Main.LocalPlayer.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                inv[slot] = rest ?? new Item();
                BigBagStorage.SaveNow();
                return;
            }

            if (mouse.IsAir)
            {
                if (item.IsAir) return;

                // 拿起整堆
                Main.mouseItem = item;
                inv[slot] = new Item();
            }
            else if (item.IsAir)
            {
                // 放下鼠标物品
                inv[slot] = mouse;
                Main.mouseItem = new Item();
            }
            else if (Item.CanStack(item, mouse) && item.stack < item.maxStack)
            {
                // 同类堆叠合并
                int move = Math.Min(item.maxStack - item.stack, mouse.stack);
                item.stack += move;
                mouse.stack -= move;
                if (mouse.stack <= 0) Main.mouseItem = new Item();
            }
            else
            {
                // 交换
                Main.mouseItem = item;
                inv[slot] = mouse;
            }

            SoundEngine.PlaySound(SoundID.Grab);
            BigBagStorage.SaveNow();
        }

        public override void RightClick(UIMouseEvent evt)
        {
            Item item = inv[slot];
            Item mouse = Main.mouseItem;

            if (mouse.IsAir)
            {
                if (item.IsAir) return;

                // 取一半（向上取整）
                int take = (item.stack + 1) / 2;
                Item half = item.Clone();
                half.stack = take;
                item.stack -= take;
                if (item.stack <= 0) inv[slot] = new Item();
                Main.mouseItem = half;
            }
            else
            {
                // 放一个（空格放置或同类追加）
                if (item.IsAir)
                {
                    Item one = mouse.Clone();
                    one.stack = 1;
                    inv[slot] = one;
                    mouse.stack--;
                }
                else if (Item.CanStack(item, mouse) && item.stack < item.maxStack)
                {
                    item.stack++;
                    mouse.stack--;
                }
                else return;

                if (mouse.stack <= 0) Main.mouseItem = new Item();
            }

            SoundEngine.PlaySound(SoundID.Grab);
            BigBagStorage.SaveNow();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!IsMouseHovering) return;

            Main.LocalPlayer.mouseInterface = true;
            Item item = inv[slot];
            if (item != null && item.type > ItemID.None)
            {
                ItemSlot.MouseHover(new Item[] { item });
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            Item item = inv[slot];
            if (item == null || item.IsAir) return;

            if (IsMouseHovering && item.type > ItemID.None)
            {
                ItemSlot.MouseHover(new Item[] { item });
                if (ItemSlot.ShiftInUse && !item.favorited)
                {
                    Main.cursorOverride = 8;
                }
            }

            CalculatedStyle rect = GetInnerDimensions();
            Vector2 center = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

            ItemSlot.DrawItemIcon(item, ItemSlot.Context.CreativeInfinite, spriteBatch, center, 1f, rect.Width, Color.White);

            if (item.stack > 1)
            {
                Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch,
                    Terraria.GameContent.FontAssets.ItemStack.Value, item.stack.ToString(),
                    new Vector2(rect.X + 4f, rect.Y + rect.Height - 2f),
                    Color.White, 0f, new Vector2(0f, Terraria.GameContent.FontAssets.ItemStack.Value.LineSpacing),
                    new Vector2(0.8f), -1f, 0.8f);
            }
        }
    }
}
