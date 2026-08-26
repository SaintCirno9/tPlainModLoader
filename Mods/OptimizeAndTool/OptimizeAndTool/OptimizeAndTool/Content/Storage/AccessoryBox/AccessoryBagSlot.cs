using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋物品槽位 UI：
    /// 采用原版官方物品栏材质 (TextureAssets.InventoryBack)、金色收藏底图与 10x10 眼睛角标，
    /// 具备原版质感、平滑居中渲染与全套快捷存取/防重交互。
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagSlot : UIElement
    {
        private readonly AccessoryBagItem bag;
        private readonly int slotIndex;
        private bool isMouseHovering;

        public AccessoryBagSlot(AccessoryBagItem bag, int slotIndex)
        {
            this.bag = bag;
            this.slotIndex = slotIndex;

            Width.Set(40f, 0f);
            Height.Set(40f, 0f);
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            isMouseHovering = true;
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            isMouseHovering = false;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (bag?.personalInventory == null || slotIndex < 0 || slotIndex >= bag.personalInventory.Length) return;

            CalculatedStyle dim = GetDimensions();
            Vector2 rel = Main.MouseScreen - new Vector2(dim.X, dim.Y);

            // 1. 点击右上角眼睛图标 (14x14 区域) 切换外观可见性
            if (rel.X > dim.Width - 14f && rel.Y < 14f && bag.hideVisuals != null && slotIndex < bag.hideVisuals.Length)
            {
                bag.hideVisuals[slotIndex] = !bag.hideVisuals[slotIndex];
                SoundEngine.PlaySound(SoundID.MenuTick);
                bag.TriggerSlotsChanged();
                return;
            }

            Item item = bag.personalInventory[slotIndex];
            Item mouse = Main.mouseItem;

            // 2. Alt/Ctrl+左键: 收藏 / 取消收藏
            if ((ItemSlot.ControlInUse || ItemSlot.ShiftInUse && Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt)) && item != null && !item.IsAir)
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

                Main.mouseItem = item.Clone();
                bag.personalInventory[slotIndex] = new Item();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else if (item == null || item.IsAir)
            {
                if (!mouse.accessory && mouse.prefix <= 0 && mouse.defense <= 0)
                {
                    if (!mouse.accessory) return;
                }

                if (CheckDuplicates(mouse, slotIndex)) return;

                bag.personalInventory[slotIndex] = mouse.Clone();
                Main.mouseItem = new Item();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else if (Item.CanStack(item, mouse) && item.stack < item.maxStack)
            {
                int move = Math.Min(item.maxStack - item.stack, mouse.stack);
                item.stack += move;
                mouse.stack -= move;
                if (mouse.stack <= 0) Main.mouseItem = new Item();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else
            {
                if (!mouse.accessory) return;
                if (CheckDuplicates(mouse, slotIndex)) return;

                Item old = item.Clone();
                bag.personalInventory[slotIndex] = mouse.Clone();
                Main.mouseItem = old;
                SoundEngine.PlaySound(SoundID.Grab);
            }

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
                SoundEngine.PlaySound(SoundID.Grab);
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
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                else if (Item.CanStack(item, mouse) && item.stack < item.maxStack)
                {
                    item.stack++;
                    mouse.stack--;
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                else return;

                if (mouse.stack <= 0) Main.mouseItem = new Item();
            }

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

            if (bag?.personalInventory == null || slotIndex < 0 || slotIndex >= bag.personalInventory.Length) return;

            if (Parent != null)
            {
                CalculatedStyle parentDims = Parent.GetInnerDimensions();
                Rectangle parentRect = parentDims.ToRectangle();
                CalculatedStyle slotDims = GetDimensions();
                Rectangle slotRect = slotDims.ToRectangle();
                if (!parentRect.Intersects(slotRect))
                {
                    isMouseHovering = false;
                    return;
                }
            }

            if (isMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Item item = bag.personalInventory[slotIndex];
                if (item != null && !item.IsAir)
                {
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = item.Name;
                }
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            if (bag?.personalInventory == null || slotIndex < 0 || slotIndex >= bag.personalInventory.Length) return;

            CalculatedStyle dim = GetDimensions();
            Rectangle slotRect = dim.ToRectangle();

            // 1. 绘制原版官方物品栏底图 (TextureAssets.InventoryBack)
            Texture2D backTex = TextureAssets.InventoryBack.Value;
            sb.Draw(backTex, slotRect, Color.White);

            Item item = bag.personalInventory[slotIndex];

            // 2. 绘制金色收藏锁定底图 (TextureAssets.InventoryBack19)
            if (item != null && !item.IsAir && item.favorited)
            {
                Texture2D favTex = TextureAssets.InventoryBack19.Value;
                Color favCol = Color.Lerp(Main.OurFavoriteColor, Color.White, 0.5f);
                sb.Draw(favTex, slotRect, favCol);
            }

            // 3. 绘制物品贴图 (黄金比例 0.65f 居中等比缩放)
            if (item != null && !item.IsAir && item.type > ItemID.None)
            {
                if (!TextureAssets.Item[item.type].IsLoaded)
                {
                    Main.instance.LoadItem(item.type);
                }

                Texture2D itemTex = TextureAssets.Item[item.type].Value;
                if (itemTex != null && !itemTex.IsDisposed)
                {
                    Rectangle frame = Main.itemAnimations[item.type]?.GetFrame(itemTex) ?? itemTex.Bounds;
                    float maxBound = (float)slotRect.Width * 0.65f;
                    float scale = 1f;
                    if ((float)frame.Width > maxBound || (float)frame.Height > maxBound)
                    {
                        scale = Math.Min(maxBound / (float)frame.Width, maxBound / (float)frame.Height);
                    }

                    Vector2 drawSize = new Vector2(frame.Width * scale, frame.Height * scale);
                    Vector2 drawPos = new Vector2(
                        slotRect.X + ((float)slotRect.Width - drawSize.X) * 0.5f,
                        slotRect.Y + ((float)slotRect.Height - drawSize.Y) * 0.5f
                    );

                    Color itemAlpha = item.GetAlpha(Color.White);
                    sb.Draw(itemTex, drawPos, frame, itemAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                    // 堆叠数字
                    if (item.stack > 1)
                    {
                        string stackStr = item.stack.ToString();
                        ChatManager.DrawColorCodedStringWithShadow(
                            sb,
                            FontAssets.ItemStack.Value,
                            stackStr,
                            new Vector2(slotRect.X + 2f, slotRect.Y + slotRect.Height - 12f),
                            Color.White,
                            0f,
                            Vector2.Zero,
                            new Vector2(0.7f)
                        );
                    }
                }

                // 4. 绘制右上角 10x10 眼睛显隐角标 (TextureAssets.InventoryTickOn / Off)
                if (bag.hideVisuals != null && slotIndex < bag.hideVisuals.Length)
                {
                    bool hidden = bag.hideVisuals[slotIndex];
                    Texture2D eyeTex = hidden ? TextureAssets.InventoryTickOff.Value : TextureAssets.InventoryTickOn.Value;
                    Vector2 eyePos = new Vector2(slotRect.X + slotRect.Width - 12f, slotRect.Y + 2f);
                    Color eyeCol = Color.White * 0.7f;

                    Rectangle eyeHitRect = new Rectangle((int)eyePos.X, (int)eyePos.Y, 10, 10);
                    if (eyeHitRect.Contains(Main.MouseScreen.ToPoint()))
                    {
                        eyeCol = Color.White;
                        Main.LocalPlayer.mouseInterface = true;
                    }

                    sb.Draw(eyeTex, eyePos, null, eyeCol, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }
            }

            // 5. 悬停光标指示与 Shift 辅助手型
            if (isMouseHovering && item != null && !item.IsAir)
            {
                if (ItemSlot.ShiftInUse && !item.favorited)
                {
                    Main.cursorOverride = 8;
                }
            }
        }
    }
}
