using FishingMachine.Content.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace FishingMachine.UI
{
    /// <summary>
    /// 自动钓鱼机专属交互 UI 面板
    /// 提供钓竿/鱼饵/饰品插槽、40格战利品仓储、自由过滤与一键提取
    /// 作者: SaintCirno9
    /// </summary>
    public static class FishingMachineUI
    {
        public static bool IsVisible { get; set; } = false;
        public static bool SelectPoolMode { get; set; } = false;
        public static bool FreeFilterMode { get; set; } = false;
        public static TEFishingMachine CurrentEntity { get; set; }

        public static Vector2 UIPosition = new Vector2(400f, 200f);
        public const float UIWidth = 460f;
        public const float UIHeight = 440f;

        public static Rectangle PanelBounds => new Rectangle((int)UIPosition.X, (int)UIPosition.Y, (int)UIWidth, (int)UIHeight);
        public static bool IsMouseHoveringUI => IsVisible && PanelBounds.Contains(Main.mouseX, Main.mouseY);

        public static Texture2D SlotPoleTexture;
        public static Texture2D SlotBaitTexture;
        public static Texture2D SlotAccTexture;
        public static Texture2D FisherLootAll;
        public static Texture2D FisherLootAllHover;
        public static Texture2D ChestAutoDeposit;
        public static Texture2D ChestAutoDepositHover;
        public static Texture2D IconFreeFilter;
        public static Texture2D IconFreeFilterHover;
        public static Texture2D SelectPoolOff;
        public static Texture2D SelectPoolOn;
        public static Texture2D DisabledItem;

        private static bool _isDragging = false;
        private static Vector2 _dragOffset;

        public static void Toggle(TEFishingMachine entity)
        {
            if (CurrentEntity == entity && IsVisible)
            {
                Close();
            }
            else
            {
                Open(entity);
            }
        }

        public static void Open(TEFishingMachine entity)
        {
            CurrentEntity = entity;
            IsVisible = true;
            SelectPoolMode = false;
            FreeFilterMode = false;
            Main.playerInventory = true;

            // 居中或合理位置
            if (UIPosition.X < 10 || UIPosition.X > Main.screenWidth - UIWidth - 10 ||
                UIPosition.Y < 10 || UIPosition.Y > Main.screenHeight - UIHeight - 10)
            {
                UIPosition = new Vector2(Math.Max(50f, (Main.screenWidth - UIWidth) / 2f + 100f), Math.Max(100f, (Main.screenHeight - UIHeight) / 2f));
            }

            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public static void Close()
        {
            IsVisible = false;
            SelectPoolMode = false;
            FreeFilterMode = false;
            _isDragging = false;
            CurrentEntity = null;
            SoundEngine.PlaySound(SoundID.MenuClose);
        }

        /// <summary>
        /// 绘制与交互入口
        /// </summary>
        public static void Draw(SpriteBatch sb)
        {
            // 选择水域时的特殊指示
            if (SelectPoolMode && CurrentEntity != null && !Main.gameMenu)
            {
                DrawSelectPoolCursorTip(sb);
            }

            if (!IsVisible || CurrentEntity == null || Main.gameMenu) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead ||
                player.Distance(new Vector2(CurrentEntity.Position.X * 16 + 16, CurrentEntity.Position.Y * 16 + 16)) > 300f)
            {
                Close();
                return;
            }

            HandleDragging();

            Rectangle bgRect = PanelBounds;
            if (bgRect.Contains(Main.mouseX, Main.mouseY))
            {
                player.mouseInterface = true;
            }

            // 绘制主背景与边框
            sb.Draw(TextureAssets.MagicPixel.Value, bgRect, new Color(16, 22, 34, 240));
            DrawBorder(sb, bgRect, new Color(0, 200, 220), 2);

            DynamicSpriteFont font = FontAssets.MouseText.Value;

            // 标题
            string title = "自动钓鱼机 (FishingMachine)";
            Terraria.Utils.DrawBorderStringFourWay(sb, font, title, UIPosition.X + 16, UIPosition.Y + 12, Color.Cyan, Color.Black, Vector2.Zero, 0.95f);

            // 关闭按钮
            Rectangle closeBtn = new Rectangle((int)(UIPosition.X + UIWidth - 32), (int)(UIPosition.Y + 10), 22, 22);
            bool hoverClose = closeBtn.Contains(Main.mouseX, Main.mouseY);
            sb.Draw(TextureAssets.MagicPixel.Value, closeBtn, hoverClose ? Color.Red : new Color(60, 20, 20));
            Terraria.Utils.DrawBorderStringFourWay(sb, font, "X", closeBtn.X + 5, closeBtn.Y + 2, Color.White, Color.Black, Vector2.Zero, 0.85f);
            if (hoverClose && Main.mouseLeft && Main.mouseLeftRelease)
            {
                Close();
                Main.mouseLeftRelease = false;
                return;
            }

            // 状态栏 (去除任何裸露代码标签，优雅排版)
            string label = "状态: ";
            string statusText = CurrentEntity.statusTip ?? "空闲";
            Terraria.Utils.DrawBorderStringFourWay(sb, font, label, UIPosition.X + 16, UIPosition.Y + 38, Color.White, Color.Black, Vector2.Zero, 0.85f);
            Vector2 labelSize = font.MeasureString(label) * 0.85f;
            Terraria.Utils.DrawBorderStringFourWay(sb, font, statusText, UIPosition.X + 16 + labelSize.X, UIPosition.Y + 38, new Color(120, 255, 180), Color.Black, Vector2.Zero, 0.85f);

            // 顶部装备槽位
            float slotY = UIPosition.Y + 68f;
            DrawEquipSlot(sb, UIPosition.X + 20, slotY, 46, "请放入钓竿", ref CurrentEntity.fishingPole, SlotPoleTexture, (it) => it.fishingPole > 0);
            DrawEquipSlot(sb, UIPosition.X + 74, slotY, 46, "请放入鱼饵", ref CurrentEntity.bait, SlotBaitTexture, (it) => it.bait > 0, allowStackOne: true);
            DrawEquipSlot(sb, UIPosition.X + 128, slotY, 46, "请放入钓鱼饰品", ref CurrentEntity.accessory, SlotAccTexture, (it) => it.accessory);

            // 顶部四大功能图标按钮
            DrawIconButton(sb, UIPosition.X + 210, slotY, 44,
                FisherLootAll, FisherLootAllHover, false,
                "一键拿取非收藏战利品",
                () =>
                {
                    CurrentEntity.LootAll(player);
                    SoundEngine.PlaySound(SoundID.Grab);
                });

            DrawIconButton(sb, UIPosition.X + 265, slotY, 44,
                SelectPoolOff, SelectPoolOn, SelectPoolMode,
                SelectPoolMode ? "正在选择水域: 点击场景中的液体来指定钓点 (右键取消)" : "选择水域: 点击场景中的液体来指定钓点",
                () =>
                {
                    SelectPoolMode = !SelectPoolMode;
                    FreeFilterMode = false;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                });

            DrawIconButton(sb, UIPosition.X + 320, slotY, 44,
                IconFreeFilter, IconFreeFilterHover, FreeFilterMode,
                FreeFilterMode ? "正在自由过滤: 点击战利品槽物品加入/移出排除黑名单" : "自由过滤: 点击战利品槽切换排除黑名单",
                () =>
                {
                    FreeFilterMode = !FreeFilterMode;
                    SelectPoolMode = false;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                });

            DrawIconButton(sb, UIPosition.X + 375, slotY, 44,
                ChestAutoDeposit, ChestAutoDepositHover, CurrentEntity.AutoDeposit,
                CurrentEntity.AutoDeposit ? "自动存箱: 开启 (自动输送非收藏战利品至相邻宝箱)" : "自动存箱: 关闭 (点击开启自动输送)",
                () =>
                {
                    CurrentEntity.AutoDeposit = !CurrentEntity.AutoDeposit;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                });

            // 中间 40 格战利品网格 (5 行 x 8 列)
            float gridStartX = UIPosition.X + 20;
            float gridStartY = UIPosition.Y + 125f;
            const float slotSize = 48f;
            const float slotGap = 4f;

            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    int index = r * 8 + c;
                    if (index >= CurrentEntity.fish.Length) break;

                    float x = gridStartX + c * (slotSize + slotGap);
                    float y = gridStartY + r * (slotSize + slotGap);
                    DrawFishSlot(sb, x, y, slotSize, index, player);
                }
            }

            // 底部 7 个功能开关
            float btnY = UIPosition.Y + 395f;
            float toggleX = UIPosition.X + 20;
            float toggleW = 56f;
            float toggleGap = 4f;

            DrawToggleButton(sb, toggleX, btnY, toggleW, 30, "宝匣", ref CurrentEntity.CatchCrates);
            DrawToggleButton(sb, toggleX + toggleW + toggleGap, btnY, toggleW, 30, "饰品", ref CurrentEntity.CatchAccessories);
            DrawToggleButton(sb, toggleX + (toggleW + toggleGap) * 2f, btnY, toggleW, 30, "工具", ref CurrentEntity.CatchTools);
            DrawToggleButton(sb, toggleX + (toggleW + toggleGap) * 3f, btnY, toggleW, 30, "白色", ref CurrentEntity.CatchWhiteRarityCatches);
            DrawToggleButton(sb, toggleX + (toggleW + toggleGap) * 4f, btnY, toggleW, 30, "普通", ref CurrentEntity.CatchNormalCatches);
            DrawToggleButton(sb, toggleX + (toggleW + toggleGap) * 5f, btnY, toggleW, 30, "无限饵", ref CurrentEntity.InfiniteBait);
            DrawToggleButton(sb, toggleX + (toggleW + toggleGap) * 6f, btnY, toggleW, 30, "存箱", ref CurrentEntity.AutoDeposit);
        }

        private static void HandleDragging()
        {
            Rectangle titleBar = new Rectangle((int)UIPosition.X, (int)UIPosition.Y, (int)UIWidth - 40, 36);
            if (titleBar.Contains(Main.mouseX, Main.mouseY) && Main.mouseLeft && !_isDragging)
            {
                _isDragging = true;
                _dragOffset = new Vector2(Main.mouseX - UIPosition.X, Main.mouseY - UIPosition.Y);
            }

            if (_isDragging)
            {
                if (Main.mouseLeft)
                {
                    UIPosition = new Vector2(Main.mouseX - _dragOffset.X, Main.mouseY - _dragOffset.Y);
                    UIPosition.X = MathHelper.Clamp(UIPosition.X, 0, Main.screenWidth - UIWidth);
                    UIPosition.Y = MathHelper.Clamp(UIPosition.Y, 0, Main.screenHeight - UIHeight);
                }
                else
                {
                    _isDragging = false;
                }
            }
        }

        private static void DrawSelectPoolCursorTip(SpriteBatch sb)
        {
            if (IsMouseHoveringUI) return;

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string tip = "点击世界中的液体选定钓点 (右键取消)";
            Vector2 size = font.MeasureString(tip);
            Vector2 pos = new Vector2(Main.mouseX + 16, Main.mouseY + 16);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)pos.X - 4, (int)pos.Y - 2, (int)size.X + 8, (int)size.Y + 4), new Color(10, 15, 25, 230));
            Terraria.Utils.DrawBorderStringFourWay(sb, font, tip, pos.X, pos.Y, Color.Cyan, Color.Black, Vector2.Zero, 0.85f);
        }

        private static void DrawEquipSlot(SpriteBatch sb, float x, float y, float size, string emptyTip, ref Item item, Texture2D bgTex, Func<Item, bool> validator, bool allowStackOne = false)
        {
            Rectangle rect = new Rectangle((int)x, (int)y, (int)size, (int)size);
            bool hover = rect.Contains(Main.mouseX, Main.mouseY);

            // 槽位底框
            sb.Draw(TextureAssets.InventoryBack.Value, rect, hover ? new Color(160, 200, 240) : new Color(80, 100, 130));

            if (item != null && !item.IsAir)
            {
                DrawItemInSlot(sb, item, x, y, size);
            }
            else if (bgTex != null)
            {
                Vector2 texPos = new Vector2(rect.Center.X - bgTex.Width / 2f, rect.Center.Y - bgTex.Height / 2f);
                sb.Draw(bgTex, texPos, Color.White * 0.45f);
            }

            if (hover)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (item == null || item.IsAir)
                {
                    Main.instance.MouseText(emptyTip);
                }
                else
                {
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = item.Name;
                }

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Item mouseItem = Main.mouseItem;
                    if (mouseItem.IsAir || validator(mouseItem))
                    {
                        Terraria.Utils.Swap(ref item, ref Main.mouseItem);
                        SoundEngine.PlaySound(SoundID.Grab);
                    }
                    Main.mouseLeftRelease = false;
                }

                if (allowStackOne && Main.mouseRight && Main.mouseRightRelease)
                {
                    HandleStackOne(ref item);
                    Main.mouseRightRelease = false;
                }
            }
        }

        private static void DrawFishSlot(SpriteBatch sb, float x, float y, float size, int index, Player player)
        {
            Rectangle rect = new Rectangle((int)x, (int)y, (int)size, (int)size);
            bool hover = rect.Contains(Main.mouseX, Main.mouseY);

            Item item = CurrentEntity.fish[index];
            bool excluded = item != null && !item.IsAir && CurrentEntity.ExcludedItems.Contains(item.type);
            bool favorited = item != null && !item.IsAir && item.favorited;

            Color slotColor = hover ? new Color(160, 200, 240) : new Color(90, 110, 140);
            if (excluded) slotColor = new Color(130, 50, 50);
            sb.Draw(TextureAssets.InventoryBack.Value, rect, slotColor);

            if (favorited)
            {
                DrawBorder(sb, rect, Color.Gold, 2);
            }

            if (item != null && !item.IsAir)
            {
                DrawItemInSlot(sb, item, x, y, size);

                if (excluded && DisabledItem != null)
                {
                    sb.Draw(DisabledItem, new Vector2(rect.X + rect.Width - 18, rect.Y + 2), Color.White * 0.95f);
                }
            }

            if (hover)
            {
                Main.LocalPlayer.mouseInterface = true;

                if (FreeFilterMode)
                {
                    Main.instance.MouseText("点击切换自由过滤名单");
                }
                else if (item != null && !item.IsAir)
                {
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = item.Name;
                }

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    if (FreeFilterMode)
                    {
                        if (item != null && !item.IsAir)
                        {
                            CurrentEntity.ToggleExcludedItem(item.type);
                            SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                    }
                    else if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt) ||
                             Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightAlt))
                    {
                        if (item != null && !item.IsAir)
                        {
                            item.favorited = !item.favorited;
                            SoundEngine.PlaySound(SoundID.MenuTick);
                        }
                    }
                    else if (ItemSlot.ShiftInUse)
                    {
                        // Shift 点击快速存入玩家背包
                        if (item != null && !item.IsAir)
                        {
                            Item left = player.GetItem(item, GetItemSettings.QuickTransferFromSlot);
                            if (left.stack <= 0)
                            {
                                CurrentEntity.fish[index] = new Item();
                            }
                            else
                            {
                                CurrentEntity.fish[index] = left;
                            }
                            SoundEngine.PlaySound(SoundID.Grab);
                            Recipe.UpdateRecipeList();
                        }
                    }
                    else
                    {
                        // 鼠标左键常规交互：同类合并堆叠或交换
                        HandleSlotLeftClick(index);
                    }
                    Main.mouseLeftRelease = false;
                }

                if (Main.mouseRight && Main.mouseRightRelease && !FreeFilterMode)
                {
                    StackOneItemToSlot(index);
                    Main.mouseRightRelease = false;
                }
            }
        }

        private static void HandleSlotLeftClick(int index)
        {
            Item slot = CurrentEntity.fish[index];
            Item mouse = Main.mouseItem;

            if (slot == null) slot = CurrentEntity.fish[index] = new Item();
            if (mouse == null) mouse = Main.mouseItem = new Item();

            if (!slot.IsAir && !mouse.IsAir && slot.type == mouse.type && slot.stack < slot.maxStack)
            {
                // 合并堆叠
                int transfer = Math.Min(mouse.stack, slot.maxStack - slot.stack);
                slot.stack += transfer;
                mouse.stack -= transfer;
                if (mouse.stack <= 0) mouse.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else
            {
                // 正常交换
                Terraria.Utils.Swap(ref CurrentEntity.fish[index], ref Main.mouseItem);
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        private static void DrawItemInSlot(SpriteBatch sb, Item item, float x, float y, float size)
        {
            if (item == null || item.IsAir) return;

            Texture2D tex = TextureAssets.Item[item.type].Value;
            if (tex == null) return;

            Rectangle frame = Main.itemAnimations[item.type] != null
                ? Main.itemAnimations[item.type].GetFrame(tex)
                : tex.Frame();

            float maxDimension = Math.Max(frame.Width, frame.Height);
            // 饱满自适应缩放（留出 8px 边距）
            float scale = (size - 8f) / Math.Max(maxDimension, 1f);
            if (scale > 1.15f) scale = 1.15f;

            Vector2 center = new Vector2(x + size / 2f, y + size / 2f);
            Vector2 origin = frame.Size() / 2f;

            // 绘制物品
            Color drawColor = item.GetAlpha(Color.White);
            sb.Draw(tex, center, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);

            if (item.color != default(Color))
            {
                sb.Draw(tex, center, frame, item.GetColor(Color.White), 0f, origin, scale, SpriteEffects.None, 0f);
            }

            // 堆叠数字
            if (item.stack > 1)
            {
                DynamicSpriteFont font = FontAssets.ItemStack.Value;
                string text = item.stack.ToString();
                Vector2 textSize = font.MeasureString(text) * 0.85f;
                Vector2 textPos = new Vector2(x + size - textSize.X - 3f, y + size - textSize.Y - 1f);
                Terraria.Utils.DrawBorderStringFourWay(sb, font, text, textPos.X, textPos.Y, Color.White, Color.Black, Vector2.Zero, 0.85f);
            }
        }

        private static void HandleStackOne(ref Item slotItem)
        {
            Item mouseItem = Main.mouseItem;
            if (slotItem == null || slotItem.IsAir)
            {
                if (mouseItem.IsAir || mouseItem.bait <= 0) return;
                slotItem = mouseItem.Clone();
                slotItem.stack = 1;
                mouseItem.stack--;
                if (mouseItem.stack <= 0) mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }

            if (mouseItem.IsAir)
            {
                Main.mouseItem = slotItem.Clone();
                Main.mouseItem.stack = 1;
                slotItem.stack--;
                if (slotItem.stack <= 0) slotItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }

            if (mouseItem.type == slotItem.type && slotItem.stack < slotItem.maxStack)
            {
                slotItem.stack++;
                mouseItem.stack--;
                if (mouseItem.stack <= 0) mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        private static void StackOneItemToSlot(int index)
        {
            Item slot = CurrentEntity.fish[index];
            Item mouseItem = Main.mouseItem;

            if (slot == null || slot.IsAir)
            {
                if (mouseItem.IsAir) return;
                CurrentEntity.fish[index] = mouseItem.Clone();
                CurrentEntity.fish[index].stack = 1;
                mouseItem.stack--;
                if (mouseItem.stack <= 0) mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }

            if (mouseItem.IsAir)
            {
                Main.mouseItem = slot.Clone();
                Main.mouseItem.stack = 1;
                slot.stack--;
                if (slot.stack <= 0) slot.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }

            if (mouseItem.type == slot.type && slot.stack < slot.maxStack)
            {
                slot.stack++;
                mouseItem.stack--;
                if (mouseItem.stack <= 0) mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        private static void DrawIconButton(SpriteBatch sb, float x, float y, float size,
            Texture2D normal, Texture2D hoverTex, bool active, string tooltip, Action onClick)
        {
            Rectangle rect = new Rectangle((int)x, (int)y, (int)size, (int)size);
            bool hover = rect.Contains(Main.mouseX, Main.mouseY);
            Texture2D tex = hover && hoverTex != null ? hoverTex : normal;
            Color bg = active ? new Color(0, 130, 100) : (hover ? new Color(70, 100, 140) : new Color(40, 60, 85));

            sb.Draw(TextureAssets.MagicPixel.Value, rect, bg);
            DrawBorder(sb, rect, hover ? Color.Cyan : (active ? Color.LightGreen : Color.Gray), 1);

            if (tex != null)
            {
                float drawX = rect.Center.X - tex.Width / 2f;
                float drawY = rect.Center.Y - tex.Height / 2f;
                sb.Draw(tex, new Vector2(drawX, drawY), Color.White);
            }

            if (hover)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (!string.IsNullOrEmpty(tooltip))
                {
                    Main.instance.MouseText(tooltip);
                }

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    onClick?.Invoke();
                    Main.mouseLeftRelease = false;
                }
            }
        }

        private static void DrawToggleButton(SpriteBatch sb, float x, float y, float w, float h, string text, ref bool state)
        {
            Rectangle rect = new Rectangle((int)x, (int)y, (int)w, (int)h);
            bool hover = rect.Contains(Main.mouseX, Main.mouseY);

            Color bgColor = state ? (hover ? new Color(0, 180, 100) : new Color(0, 130, 70)) : (hover ? new Color(100, 60, 60) : new Color(60, 40, 40));
            sb.Draw(TextureAssets.MagicPixel.Value, rect, bgColor);
            DrawBorder(sb, rect, state ? Color.LightGreen : Color.Gray, 1);

            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string display = $"{text}: {(state ? "开" : "关")}";
            Vector2 size = font.MeasureString(display);
            float scale = Math.Min(0.72f, (w - 4f) / Math.Max(size.X, 1f));
            Terraria.Utils.DrawBorderStringFourWay(sb, font, display,
                rect.Center.X - size.X * scale / 2f, rect.Y + (rect.Height - size.Y * scale) / 2f,
                Color.White, Color.Black, Vector2.Zero, scale);

            if (hover)
            {
                Main.LocalPlayer.mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    state = !state;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    Main.mouseLeftRelease = false;
                }
            }
        }

        private static void DrawBorder(SpriteBatch sb, Rectangle r, Color c, int thickness)
        {
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, r.Width, thickness), c);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, thickness, r.Height), c);
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
        }
    }
}