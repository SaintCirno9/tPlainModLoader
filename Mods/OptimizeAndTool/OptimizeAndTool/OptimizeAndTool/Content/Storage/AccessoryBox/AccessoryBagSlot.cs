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
    /// 饰品袋物品槽位 UI：
    /// 支持独立眼睛外观显隐、Alt 收藏锁定、Shift 快速退回背包、左键/右键存取与防重复拦截
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagSlot : UIPanel
    {
        private readonly AccessoryBagItem bag;
        private readonly int slotIndex;

        public AccessoryBagSlot(AccessoryBagItem bag, int slotIndex)
        {
            this.bag = bag;
            this.slotIndex = slotIndex;

            Width.Set(44, 0);
            Height.Set(44, 0);
            SetPadding(4);
            BackgroundColor = BorderColor = new Color(33, 43, 79);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (bag?.personalInventory == null || slotIndex < 0 || slotIndex >= bag.personalInventory.Length) return;

            CalculatedStyle dim = GetDimensions();
            Vector2 rel = Main.MouseScreen - new Vector2(dim.X, dim.Y);

            // 1. 点击右上角眼睛图标 (14x14 区域) 切换外观可见性
            if (rel.X > dim.Width - 16f && rel.Y < 16f && bag.hideVisuals != null && slotIndex < bag.hideVisuals.Length)
            {
                bag.hideVisuals[slotIndex] = !bag.hideVisuals[slotIndex];
                SoundEngine.PlaySound(SoundID.MenuTick);
                bag.TriggerSlotsChanged();
                return;
            }

            Item item = bag.personalInventory[slotIndex];
            Item mouse = Main.mouseItem;

            // 2. Alt+左键: 收藏 / 取消收藏
            if (ItemSlot.ControlInUse && !item.IsAir)
            {
                item.favorited = !item.favorited;
                SoundEngine.PlaySound(SoundID.MenuTick);
                bag.TriggerSlotsChanged();
                return;
            }

            // 3. Shift+左键: 快速提取回个人背包
            if (ItemSlot.ShiftInUse)
            {
                if (item == null || item.IsAir) return;

                SoundEngine.PlaySound(SoundID.Grab);
                Item rest = Main.LocalPlayer.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                bag.personalInventory[slotIndex] = rest ?? new Item();
                bag.TriggerSlotsChanged();
                return;
            }

            // 4. 普通左键操作 (拿取 / 放入 / 交换)
            if (mouse.IsAir)
            {
                if (item == null || item.IsAir) return;

                Main.mouseItem = item;
                bag.personalInventory[slotIndex] = new Item();
            }
            else if (item == null || item.IsAir)
            {
                if (!mouse.accessory && mouse.prefix <= 0 && mouse.defense <= 0)
                {
                    // 仅允许放入饰品或带词缀/防御属性的装备
                    if (!mouse.accessory) return;
                }

                if (CheckDuplicates(mouse, slotIndex)) return;

                bag.personalInventory[slotIndex] = mouse;
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
                if (!mouse.accessory) return;
                if (CheckDuplicates(mouse, slotIndex)) return;

                Main.mouseItem = item;
                bag.personalInventory[slotIndex] = mouse;
            }

            SoundEngine.PlaySound(SoundID.Grab);
            bag.TriggerSlotsChanged();
        }

        public override void RightClick(UIMouseEvent evt)
        {
            if (bag?.personalInventory == null || slotIndex < 0 || slotIndex >= bag.personalInventory.Length) return;

            Item item = bag.personalInventory[slotIndex];
            Item mouse = Main.mouseItem;

            if (mouse.IsAir)
            {
                if (item == null || item.IsAir) return;

                int take = (item.stack + 1) / 2;
                Item half = item.Clone();
                half.stack = take;
                item.stack -= take;
                if (item.stack <= 0) bag.personalInventory[slotIndex] = new Item();
                Main.mouseItem = half;
            }
            else
            {
                if (!mouse.accessory) return;
                if (CheckDuplicates(mouse, slotIndex)) return;

                if (item == null || item.IsAir)
                {
                    Item one = mouse.Clone();
                    one.stack = 1;
                    bag.personalInventory[slotIndex] = one;
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
            bag.TriggerSlotsChanged();
        }

        private bool CheckDuplicates(Item candidate, int currentSlot)
        {
            if (AccessoryBagConfig.PreventBagDuplicates.val && bag.personalInventory != null)
            {
                for (int i = 0; i < bag.personalInventory.Length; i++)
                {
                    if (i != currentSlot && bag.personalInventory[i] != null && !bag.personalInventory[i].IsAir && bag.personalInventory[i].type == candidate.type)
                    {
                        SoundEngine.PlaySound(SoundID.MenuClose);
                        Main.NewText($"[饰品袋] 已存在同种饰品 {candidate.Name}，禁止重复存放！", Color.OrangeRed);
                        return true;
                    }
                }
            }

            if (AccessoryBagConfig.PreventPlayerBagDuplicates.val && Main.LocalPlayer?.armor != null)
            {
                for (int i = 3; i < Main.LocalPlayer.armor.Length; i++)
                {
                    Item armorIt = Main.LocalPlayer.armor[i];
                    if (armorIt != null && !armorIt.IsAir && armorIt.type == candidate.type)
                    {
                        SoundEngine.PlaySound(SoundID.MenuClose);
                        Main.NewText($"[饰品袋] 角色已装备 {candidate.Name}，禁止在袋中重复挂载！", Color.OrangeRed);
                        return true;
                    }
                }
            }

            if (AccessoryBagConfig.EnableMaxDuplicateAccessory.val)
            {
                int curCount = bag.CountDuplicate(candidate);
                if (curCount >= AccessoryBagConfig.MaxDuplicateAccessory.val)
                {
                    SoundEngine.PlaySound(SoundID.MenuClose);
                    Main.NewText($"[饰品袋] 同种饰品最大上限为 {AccessoryBagConfig.MaxDuplicateAccessory.val} 个！", Color.OrangeRed);
                    return true;
                }
            }

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!IsMouseHovering) return;

            Main.LocalPlayer.mouseInterface = true;
            if (bag?.personalInventory != null && slotIndex >= 0 && slotIndex < bag.personalInventory.Length)
            {
                Item item = bag.personalInventory[slotIndex];
                if (item != null && item.type > ItemID.None)
                {
                    ItemSlot.MouseHover(new Item[] { item });
                }
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            if (bag?.personalInventory == null || slotIndex < 0 || slotIndex >= bag.personalInventory.Length) return;
            Item item = bag.personalInventory[slotIndex];

            CalculatedStyle dim = GetDimensions();

            // 1. 收藏金色边框高亮
            if (item != null && !item.IsAir && item.favorited)
            {
                Texture2D favTex = TextureAssets.InventoryBack19.Value;
                Color favCol = Color.Lerp(Main.OurFavoriteColor, Color.White, 0.5f);
                sb.Draw(favTex, dim.ToRectangle(), favCol);
            }

            // 2. 绘制物品图标
            if (item != null && !item.IsAir && item.type > ItemID.None)
            {
                if (!TextureAssets.Item[item.type].IsLoaded)
                {
                    Main.instance.LoadItem(item.type);
                }

                Texture2D tex = TextureAssets.Item[item.type].Value;
                Rectangle frame = Main.itemAnimations[item.type]?.GetFrame(tex) ?? tex.Frame();

                float maxSide = Math.Max(frame.Width, frame.Height);
                float scale = maxSide > 32f ? 32f / maxSide : 1f;

                Vector2 origin = frame.Size() / 2f;
                Vector2 pos = new Vector2(dim.X + dim.Width / 2f, dim.Y + dim.Height / 2f);

                Color alpha = item.GetAlpha(Color.White);
                sb.Draw(tex, pos, frame, alpha, 0f, origin, scale, SpriteEffects.None, 0f);

                if (item.stack > 1)
                {
                    string s = item.stack.ToString();
                    Vector2 numOrigin = FontAssets.ItemStack.Value.MeasureString(s) * new Vector2(0f, 1f);
                    Vector2 numPos = new Vector2(dim.X + 4f, dim.Y + dim.Height - 2f);
                    ChatManager.DrawColorCodedStringWithShadow(
                        sb,
                        FontAssets.ItemStack.Value,
                        s,
                        numPos,
                        Color.White,
                        0f,
                        numOrigin,
                        new Vector2(0.75f)
                    );
                }
            }

            // 3. 绘制右上角眼睛外观图标 (绿色开启 / 划线关闭)
            if (bag.hideVisuals != null && slotIndex < bag.hideVisuals.Length)
            {
                bool hidden = bag.hideVisuals[slotIndex];
                Texture2D eyeTex = hidden ? TextureAssets.InventoryTickOff.Value : TextureAssets.InventoryTickOn.Value;
                Vector2 eyePos = new Vector2(dim.X + dim.Width - 13f, dim.Y + 3f);
                Color eyeCol = Color.White * 0.85f;

                Rectangle eyeRect = new Rectangle((int)eyePos.X, (int)eyePos.Y, 12, 12);
                if (eyeRect.Contains(Main.MouseScreen.ToPoint()))
                {
                    eyeCol = Color.White;
                }

                sb.Draw(eyeTex, eyePos, null, eyeCol, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }

            if (IsMouseHovering && item != null && item.type > ItemID.None)
            {
                ItemSlot.MouseHover(new Item[] { item });
                if (ItemSlot.ShiftInUse && !item.favorited)
                {
                    Main.cursorOverride = 8;
                }
            }
        }
    }
}
