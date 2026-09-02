using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using OptimizeAndTool.Content.Storage.Core;
using OptimizeAndTool.Content.Storage.ItemContainer;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 通用容器物品槽 UI 组件：
    /// 采用原版官方物品栏材质 (TextureAssets.InventoryBack)、金色收藏锁定框与 0.65f 黄金比例缩放，
    /// 原生支持动画帧、堆叠文字阴影、可选眼睛显隐角标以及全套左键/右键/Shift/Alt/Ctrl 交互。
    /// 作者: SaintCirno9
    /// </summary>
    public class UniversalBagSlot : UIElement
    {
        private readonly IBagInventory bag;
        private readonly int slotIndex;

        // 右键分堆与加速状态机私有计时器
        private int rightMouseDownTimer = -1;
        private int superFastStackTimer = 0;

        public UniversalBagSlot(IBagInventory bag, int slotIndex)
        {
            this.bag = bag;
            this.slotIndex = slotIndex;

            Width.Set(40f, 0f);
            Height.Set(40f, 0f);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (bag?.Slots == null || slotIndex < 0 || slotIndex >= bag.Slots.Length) return;

            CalculatedStyle dim = GetDimensions();
            Vector2 rel = Main.MouseScreen - new Vector2(dim.X, dim.Y);

            // 1. 若支持外观显隐且点击右上角眼睛图标 (14x14 区域)
            if (bag is IVisualToggleable vis && vis.HideVisuals != null && slotIndex < vis.HideVisuals.Length)
            {
                if (rel.X > dim.Width - 14f && rel.Y < 14f)
                {
                    vis.ToggleVisual(slotIndex);
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    bag.TriggerSlotsChanged();
                    return;
                }
            }

            Item item = bag.Slots[slotIndex];
            Item mouse = Main.mouseItem;

            // 2. Alt/Favorite+左键: 收藏 / 取消收藏
            bool isFavoriteKey = Main.keyState.IsKeyDown(Main.FavoriteKey) ||
                                 Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) ||
                                 Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt);

            if (bag.CanFavorite && isFavoriteKey && item != null && !item.IsAir)
            {
                item.favorited = !item.favorited;
                SoundEngine.PlaySound(SoundID.MenuTick);
                bag.TriggerSlotsChanged();
                return;
            }

            // 2.5 Ctrl+左键且商人界面打开: 快速售出物品 (对齐原版商人售出逻辑)
            if (ItemSlot.ControlInUse && Main.npcShop > 0 && item != null && !item.IsAir && !item.favorited && (item.type < 71 || item.type > 74))
            {
                Chest shopChest = Main.instance.shop[Main.npcShop];
                if (Main.LocalPlayer.SellItem(item))
                {
                    shopChest.AddItemToShop(item);
                    ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(item, ItemSlot.Context.BankItem, ItemSlot.Context.ShopItem));
                    item.TurnToAir();
                    SoundEngine.PlaySound(SoundID.Coins);
                }
                else if (item.value == 0)
                {
                    shopChest.AddItemToShop(item);
                    ItemSlot.AnnounceTransfer(new ItemSlot.ItemTransferInfo(item, ItemSlot.Context.BankItem, ItemSlot.Context.ShopItem));
                    item.TurnToAir();
                    SoundEngine.PlaySound(SoundID.Grab);
                }

                if (bag.IsDynamicCapacity)
                {
                    bag.EnsureTrailingEmptySlots(10);
                }
                bag.TriggerSlotsChanged();
                return;
            }

            // 3. Shift+左键: 快速提取回个人背包
            if (ItemSlot.ShiftInUse)
            {
                if (item == null || item.IsAir) return;

                SoundEngine.PlaySound(SoundID.Grab);
                Item rest = null;
                try
                {
                    ItemContainerItem.IsTransferringOut = true;
                    rest = Main.LocalPlayer.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                }
                finally
                {
                    ItemContainerItem.IsTransferringOut = false;
                }
                bag.Slots[slotIndex] = rest ?? new Item();
                bag.TriggerSlotsChanged();
                return;
            }

            // 4. 普通左键操作 (拿取 / 放入 / 堆叠 / 交换)
            if (mouse.IsAir)
            {
                if (item == null || item.IsAir) return;

                Main.mouseItem = item.Clone();
                bag.Slots[slotIndex] = new Item();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else if (item == null || item.IsAir)
            {
                if (!bag.MeetEntryCriteria(mouse, slotIndex)) return;

                bag.Slots[slotIndex] = mouse.Clone();
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
                if (!bag.MeetEntryCriteria(mouse, slotIndex)) return;

                Item old = item.Clone();
                bag.Slots[slotIndex] = mouse.Clone();
                Main.mouseItem = old;
                SoundEngine.PlaySound(SoundID.Grab);
            }

            if (bag.IsDynamicCapacity)
            {
                bag.EnsureTrailingEmptySlots(10);
            }
            bag.TriggerSlotsChanged();
        }

        public override void RightClick(UIMouseEvent evt)
        {
            // 右键生命周期已由 Update 中的状态机统一精准调度，此处避免重复触发
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (bag?.Slots == null || slotIndex < 0 || slotIndex >= bag.Slots.Length) return;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;

                // 统一处理右键状态机 (单击防误触、缓慢加速分堆、原版装备穿戴与开箱)
                HandleRightClickLogic();
            }
            else
            {
                // 光标移出当前槽位，若之前处于按住状态则触发释放重置
                if (rightMouseDownTimer >= 0)
                {
                    OnRightMouseReleased();
                }
            }
        }

        /// <summary>
        /// 统一调度右键分堆状态机、长按加速与原版扩展交互（开箱/穿戴）
        /// </summary>
        private void HandleRightClickLogic()
        {
            // 鼠标未按住右键时：若之前按住过则触发松开收尾
            if (!Main.mouseRight)
            {
                if (rightMouseDownTimer >= 0)
                {
                    OnRightMouseReleased();
                }
                return;
            }

            // 玩家处于使用物品动画中则禁止交互
            if (Main.LocalPlayer.itemAnimation > 0)
            {
                return;
            }

            Item item = bag.Slots[slotIndex];
            if (item == null || item.IsAir)
            {
                if (rightMouseDownTimer >= 0)
                {
                    OnRightMouseReleased();
                }
                return;
            }

            // 1. 原版扩展右键交互：光标为空时支持单件装备右键快速穿戴替换与宝藏袋/礼包原地开启
            if (Main.mouseItem.IsAir)
            {
                // 1.1 单件装备/时装/染料快捷穿戴 (SwapEquip)
                if (item.stack == 1 && ItemSlot.CanSwapEquip(item))
                {
                    if (Main.mouseRightRelease)
                    {
                        ItemSlot.SwapEquip(bag.Slots, ItemSlot.Context.InventoryItem, slotIndex);
                        Main.mouseRightRelease = false;

                        // 若换下的旧装备不符合当前容器限制，自动转移至玩家主背包
                        if (!bag.Slots[slotIndex].IsAir && !bag.MeetEntryCriteria(bag.Slots[slotIndex], slotIndex))
                        {
                            Item ejected = bag.Slots[slotIndex].Clone();
                            bag.Slots[slotIndex].TurnToAir();
                            Main.LocalPlayer.GetItem(ejected, GetItemSettings.QuickTransferFromSlot);
                        }

                        if (bag.IsDynamicCapacity)
                        {
                            bag.EnsureTrailingEmptySlots(10);
                        }
                        bag.TriggerSlotsChanged();
                        rightMouseDownTimer = 0;
                    }
                    return;
                }

                // 1.2 宝藏袋与可开启容器原地开启
                ModItem modItem = ItemLoader.GetModItem(item.type);
                bool canOpenModItem = modItem != null && modItem.CanRightClick();
                bool isOpenableVanilla = ItemID.Sets.OpenableBag[item.type];

                if (canOpenModItem || isOpenableVanilla)
                {
                    if (Main.mouseRightRelease)
                    {
                        if (canOpenModItem)
                        {
                            modItem.RightClick(Main.LocalPlayer);
                            item.stack--;
                            if (item.stack <= 0)
                            {
                                item.TurnToAir();
                            }
                            SoundEngine.PlaySound(SoundID.Grab);
                        }
                        else
                        {
                            ItemSlot.TryOpenContainer(bag.Slots, ItemSlot.Context.InventoryItem, slotIndex, Main.LocalPlayer);
                        }

                        Main.mouseRightRelease = false;
                        if (bag.IsDynamicCapacity)
                        {
                            bag.EnsureTrailingEmptySlots(10);
                        }
                        bag.TriggerSlotsChanged();
                        rightMouseDownTimer = 0;
                    }
                    return;
                }
            }

            // 2. 右键分堆逻辑 (支持光标为空时取出 1 个，或光标持有同类未满物品时堆叠取出)
            if (!Main.mouseItem.IsAir && (!Item.CanStack(Main.mouseItem, item) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
            {
                return;
            }

            // 初次按下瞬间：取出 1 个并开始计时
            if (rightMouseDownTimer == -1)
            {
                rightMouseDownTimer = 0;
                superFastStackTimer = 0;
                TakeSlotItemToMouse(1);
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            // 持续长按计时推进
            rightMouseDownTimer++;

            bool shouldTake = false;
            int takeCount = 1;

            // 对齐原版分堆曲线：
            // 前 30 帧 (约 0.5 秒) 防误触静止停顿
            if (rightMouseDownTimer >= 30)
            {
                if (rightMouseDownTimer < 60)
                {
                    // 30~60 帧区间：从 6 帧/个渐进缩短为 4 帧/个、2 帧/个
                    int interval = rightMouseDownTimer < 40 ? 6 : (rightMouseDownTimer < 50 ? 4 : 2);
                    if (rightMouseDownTimer % interval == 0)
                    {
                        shouldTake = true;
                        takeCount = 1;
                    }
                }
                else
                {
                    // >= 60 帧：激活超速多倍堆叠，每 2 帧取出 1+n 个
                    if (rightMouseDownTimer % 2 == 0)
                    {
                        if (superFastStackTimer < 40)
                        {
                            superFastStackTimer++;
                        }
                        shouldTake = true;
                        takeCount = 1 + (superFastStackTimer / 2);
                    }
                }
            }

            if (shouldTake)
            {
                int beforeStack = item.stack;
                TakeSlotItemToMouse(takeCount);
                if (item.stack != beforeStack)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }
        }

        private void OnRightMouseReleased()
        {
            rightMouseDownTimer = -1;
            superFastStackTimer = 0;

            if (bag != null)
            {
                if (bag.IsDynamicCapacity)
                {
                    bag.EnsureTrailingEmptySlots(10);
                }
                bag.TriggerSlotsChanged();
            }
        }

        /// <summary>
        /// 从槽位中取出指定数量物品至鼠标光标（轻量化数据修改，不触发全量 UI 销毁重建）
        /// </summary>
        private void TakeSlotItemToMouse(int count)
        {
            if (bag?.Slots == null || slotIndex < 0 || slotIndex >= bag.Slots.Length) return;
            Item item = bag.Slots[slotIndex];
            if (item == null || item.IsAir) return;

            Item mouse = Main.mouseItem;
            if (mouse.IsAir)
            {
                int actualTake = Math.Min(count, item.stack);
                Main.mouseItem = item.Clone();
                Main.mouseItem.stack = actualTake;
                if (!item.favorited || item.stack > actualTake)
                {
                    Main.mouseItem.favorited = false;
                }
                item.stack -= actualTake;
                if (item.stack <= 0)
                {
                    item.TurnToAir();
                }
            }
            else if (Item.CanStack(mouse, item))
            {
                int maxCanAdd = mouse.maxStack - mouse.stack;
                int actualTake = Math.Min(Math.Min(count, item.stack), maxCanAdd);
                if (actualTake > 0)
                {
                    mouse.stack += actualTake;
                    item.stack -= actualTake;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                    }
                }
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            if (bag?.Slots == null || slotIndex < 0 || slotIndex >= bag.Slots.Length) return;

            CalculatedStyle dim = GetDimensions();
            Rectangle slotRect = dim.ToRectangle();

            // 视口裁剪与虚拟化渲染优化：若槽位不在当前 UIList 裁剪可视窗口或屏幕范围内，直接跳过绘制与贴图加载
            Rectangle scissor = sb.GraphicsDevice.ScissorRectangle;
            if (scissor.Width > 0 && scissor.Height > 0 && !slotRect.Intersects(scissor))
            {
                return;
            }

            // 1. 绘制原版官方物品栏底图 (TextureAssets.InventoryBack)
            Texture2D backTex = TextureAssets.InventoryBack.Value;
            sb.Draw(backTex, slotRect, Color.White);

            Item item = bag.Slots[slotIndex];

            // 2. 绘制金色收藏锁定底图 (TextureAssets.InventoryBack19)
            if (bag.CanFavorite && item != null && !item.IsAir && item.favorited)
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

                    // 堆叠数字 (带阴影)
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
                if (bag is IVisualToggleable vis && vis.HideVisuals != null && slotIndex < vis.HideVisuals.Length)
                {
                    bool hidden = vis.HideVisuals[slotIndex];
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

            // 5. 悬停提示 Tooltip 与光标指示
            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (item != null && !item.IsAir)
                {
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = item.AffixName();
                    ItemSlot.MouseHover(new Item[] { item });

                    bool isFavoriteKey = Main.keyState.IsKeyDown(Main.FavoriteKey) ||
                                         Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) ||
                                         Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt);

                    if (bag.CanFavorite && isFavoriteKey)
                    {
                        Main.cursorOverride = 3; // 原版金色星星收藏光标
                    }
                    else if (ItemSlot.ControlInUse && Main.npcShop > 0 && !item.favorited && (item.type < 71 || item.type > 74))
                    {
                        Main.cursorOverride = 10; // 原版金币售出光标
                    }
                    else if (ItemSlot.ShiftInUse && !item.favorited)
                    {
                        Main.cursorOverride = 8; // 快速转移手型光标
                    }
                }
            }
        }
    }
}
