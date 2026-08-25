using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 容器物品槽（用于药水袋与旗帜盒）：
    /// 支持左键拿放换/堆叠，Shift+左键快速提取，右键半取/单放
    /// 作者: SaintCirno9
    /// </summary>
    public class ItemContainerSlot : UIPanel
    {
        private readonly ItemContainerStorage storage;
        private readonly int slotIndex;

        public ItemContainerSlot(ItemContainerStorage storage, int slotIndex)
        {
            this.storage = storage;
            this.slotIndex = slotIndex;

            Width.Set(44, 0);
            Height.Set(44, 0);
            SetPadding(4);
            BackgroundColor = BorderColor = new Color(43, 60, 120);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (storage?.Slots == null || slotIndex < 0 || slotIndex >= storage.Slots.Length) return;
            Item item = storage.Slots[slotIndex];
            Item mouse = Main.mouseItem;

            // Shift+左键: 快速提取回个人背包
            if (ItemSlot.ShiftInUse)
            {
                if (item == null || item.IsAir) return;

                SoundEngine.PlaySound(SoundID.Grab);
                Item rest = null;
                try
                {
                    ItemContainerStorage.IsTransferringOut = true;
                    rest = Main.LocalPlayer.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                }
                finally
                {
                    ItemContainerStorage.IsTransferringOut = false;
                }
                storage.Slots[slotIndex] = rest ?? new Item();
                storage.SaveNow();
                return;
            }

            if (mouse.IsAir)
            {
                if (item == null || item.IsAir) return;

                // 拿起整堆
                Main.mouseItem = item;
                storage.Slots[slotIndex] = new Item();
            }
            else if (item == null || item.IsAir)
            {
                // 放下鼠标物品 (需满足准入条件)
                if (!storage.MeetEntryCriteria(mouse)) return;

                storage.Slots[slotIndex] = mouse;
                Main.mouseItem = new Item();
            }
            else if (Item.CanStack(item, mouse) && item.stack < item.maxStack)
            {
                // 同类合并
                int move = Math.Min(item.maxStack - item.stack, mouse.stack);
                item.stack += move;
                mouse.stack -= move;
                if (mouse.stack <= 0) Main.mouseItem = new Item();
            }
            else
            {
                // 交换 (鼠标物品需满足准入条件)
                if (!storage.MeetEntryCriteria(mouse)) return;

                Main.mouseItem = item;
                storage.Slots[slotIndex] = mouse;
            }

            SoundEngine.PlaySound(SoundID.Grab);
            storage.SaveNow();
        }

        public override void RightClick(UIMouseEvent evt)
        {
            if (storage?.Slots == null || slotIndex < 0 || slotIndex >= storage.Slots.Length) return;
            Item item = storage.Slots[slotIndex];
            Item mouse = Main.mouseItem;

            if (mouse.IsAir)
            {
                if (item == null || item.IsAir) return;

                // 取一半
                int take = (item.stack + 1) / 2;
                Item half = item.Clone();
                half.stack = take;
                item.stack -= take;
                if (item.stack <= 0) storage.Slots[slotIndex] = new Item();
                Main.mouseItem = half;
            }
            else
            {
                // 放 1 个
                if (!storage.MeetEntryCriteria(mouse)) return;

                if (item == null || item.IsAir)
                {
                    Item one = mouse.Clone();
                    one.stack = 1;
                    storage.Slots[slotIndex] = one;
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
            storage.SaveNow();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!IsMouseHovering) return;

            Main.LocalPlayer.mouseInterface = true;
            if (storage?.Slots != null && slotIndex >= 0 && slotIndex < storage.Slots.Length)
            {
                Item item = storage.Slots[slotIndex];
                if (item != null && item.type > ItemID.None)
                {
                    ItemSlot.MouseHover(new Item[] { item });
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (storage?.Slots == null || slotIndex < 0 || slotIndex >= storage.Slots.Length) return;
            Item item = storage.Slots[slotIndex];
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
            Vector2 center = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

            ItemSlot.DrawItemIcon(item, ItemSlot.Context.CreativeInfinite, spriteBatch, center, 1f, rect.Width, Color.White);

            if (item.stack > 1)
            {
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch,
                    FontAssets.ItemStack.Value, item.stack.ToString(),
                    new Vector2(rect.X + 4f, rect.Y + rect.Height - 2f),
                    Color.White, 0f, new Vector2(0f, FontAssets.ItemStack.Value.LineSpacing),
                    new Vector2(0.8f), -1f, 0.8f);
            }
        }
    }
}
