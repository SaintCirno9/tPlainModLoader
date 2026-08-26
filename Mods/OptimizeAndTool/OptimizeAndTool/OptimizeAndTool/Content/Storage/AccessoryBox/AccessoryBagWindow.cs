using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 随身饰品袋 UI 窗口：
    /// 精确对齐 AccBag 经典质感与自适应排版，杜绝左右空隙，具备原版物品栏材质与智能侧边栏
    /// 作者: SaintCirno9
    /// </summary>
    public class AccessoryBagWindow : UIWindow
    {
        private static AccessoryBagWindow instance = null;
        public static AccessoryBagWindow Instance => instance ?? (instance = new AccessoryBagWindow());

        public static new bool IsOpen => instance != null && instance.Parent != null;
        public static bool IsOpenAndHovering => IsOpen && instance.IsMouseHovering;

        public AccessoryBagItem CurrentBag { get; private set; }

        private const int SLOTS_PER_ROW = 10;
        private const int MAX_VISIBLE_ROWS = 7;
        private const float SLOT_SIZE = 40f;
        private const float SLOT_MARGIN = 4f;

        private UIElement contentArea = null;
        private UIBoxWrapPanel wp = null;
        private UIList uiList = null;
        private UIScrollbar scrollbar = null;
        private ModIconSidebar sidebar = null;
        private UIText capacityText = null;

        public static void Toggle(AccessoryBagItem bag = null)
        {
            if (bag == null)
            {
                bag = AccessoryBagCacheManager.GetFirstCarriedBag();
            }

            if (IsOpen && Instance.CurrentBag == bag && bag != null)
            {
                Instance.Close();
            }
            else if (bag != null)
            {
                if (ModifyInterfaceLayers.ui_state != null)
                {
                    Instance.Open(bag, ModifyInterfaceLayers.ui_state);
                }
            }
            else if (IsOpen)
            {
                Instance.Close();
            }
        }

        public AccessoryBagWindow() : base("随身饰品袋", 476, 360)
        {
            instance = this;

            UIElement topToolbar = BuildTopToolbar();
            Child.Append(topToolbar);

            contentArea = new UIElement();
            contentArea.Width.Set(0, 1);
            contentArea.Height.Set(-topToolbar.Height.Pixels - 6, 1);
            contentArea.VAlign = 1;
            Child.Append(contentArea);

            sidebar = new ModIconSidebar(null);
            sidebar.OnFilterChanged += _ => RebuildSlots();
            contentArea.Append(sidebar);

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(0, 1);
            scrollbar.HAlign = 1;

            uiList = new UIList();
            uiList.Height.Precent = 1;
            uiList.SetScrollbar(scrollbar);

            contentArea.Append(uiList);

            wp = new UIBoxWrapPanel();
            wp.Width.Set(SLOTS_PER_ROW * (SLOT_SIZE + SLOT_MARGIN) - SLOT_MARGIN, 0);
            wp.ItemMargin = (int)SLOT_MARGIN;
            uiList.Add(wp);

            OnClose += () =>
            {
                if (CurrentBag != null)
                {
                    CurrentBag.OnSlotsChanged -= Rebuild;
                }
            };
        }

        public void Open(AccessoryBagItem bag, UIState parentState)
        {
            if (CurrentBag != null)
            {
                CurrentBag.OnSlotsChanged -= Rebuild;
            }

            CurrentBag = bag;
            if (CurrentBag != null)
            {
                CurrentBag.ResizeToConfig();
                CurrentBag.OnSlotsChanged += Rebuild;
                if (ui_title != null)
                {
                    ui_title.SetText($"随身饰品袋 [{CurrentBag.ShortID}]");
                }
            }

            if (!Main.playerInventory)
            {
                Main.playerInventory = true;
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }

            Open(parentState);
            Rebuild();
        }

        private UIElement BuildTopToolbar()
        {
            int height = 22;

            UIElement container = new UIElement();
            container.Width.Set(0, 1);
            container.Height.Set(height, 0);

            UIStackPanel sp = new UIStackPanel();
            sp.Height.Set(height, 0);
            sp.VAlign = 0.5f;
            sp.IsAutoUpdateSize = true;
            sp.Horizontal = true;
            sp.ItemMargin = 6;
            container.Append(sp);

            // 1. 全部存入
            UIBoxButton btnDeposit = new UIBoxButton(height, () => "一键存入背包中所有非快捷栏、非收藏饰品", "Images/UI/Cursor_4");
            btnDeposit.OnClick += DepositAll;
            sp.Append(btnDeposit);

            // 2. 快速堆叠
            UIBoxButton btnQuickStack = new UIBoxButton(height, () => "一键快速堆叠背包中已有饰品", "Images/UI/Cursor_8");
            btnQuickStack.OnClick += QuickStack;
            sp.Append(btnQuickStack);

            // 3. 全部取出
            UIBoxButton btnLoot = new UIBoxButton(height, () => "一键取出袋中所有饰品回个人背包", "Images/UI/Cursor_6");
            btnLoot.OnClick += LootAll;
            sp.Append(btnLoot);

            // 4. 智能整理排序
            UIBoxButton btnSort = new UIBoxButton(height, () => "按饰品类型、价值与稀有度整理排序 (保留收藏位置)", "Images/UI/Cursor_3");
            btnSort.OnClick += SortBag;
            sp.Append(btnSort);

            // 5. 一键全显/全隐外观
            UIBoxButton btnAllVisuals = new UIBoxButton(
                height,
                () => "一键切换袋中所有饰品的外观可见性 (全部显示/全部隐藏)",
                () => HasAnyVisibleVisuals() ? "Images/UI/InfoIcon_0" : "Images/UI/InfoIcon_5"
            );
            btnAllVisuals.OnClick += ToggleAllVisuals;
            sp.Append(btnAllVisuals);

            // 6. 被动属性生效开关
            UIBoxButton btnPassive = new UIBoxButton(
                height,
                () => AccessoryBagConfig.EnablePassive.val ? "被动饰品属性: 已生效 (点击禁用)" : "被动饰品属性: 已禁用 (点击开启)",
                () => AccessoryBagConfig.EnablePassive.val ? "Images/Item_158" : "Images/UI/InfoIcon_5"
            );
            btnPassive.OnClick += () =>
            {
                AccessoryBagConfig.EnablePassive.val = !AccessoryBagConfig.EnablePassive.val;
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            sp.Append(btnPassive);

            // 7. 统计文本
            capacityText = new UIText("0/40", 0.8f);
            capacityText.VAlign = 0.5f;
            sp.Append(capacityText);

            return container;
        }

        private bool HasAnyVisibleVisuals()
        {
            if (CurrentBag?.hideVisuals == null) return false;
            for (int i = 0; i < CurrentBag.hideVisuals.Length; i++)
            {
                if (!CurrentBag.hideVisuals[i]) return true;
            }
            return false;
        }

        private void ToggleAllVisuals()
        {
            if (CurrentBag?.hideVisuals == null) return;
            bool targetHidden = HasAnyVisibleVisuals();
            for (int i = 0; i < CurrentBag.hideVisuals.Length; i++)
            {
                CurrentBag.hideVisuals[i] = targetHidden;
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
            CurrentBag.TriggerSlotsChanged();
        }

        public void Rebuild()
        {
            if (sidebar != null && CurrentBag != null)
            {
                sidebar.SetBag(CurrentBag);
                sidebar.Rebuild();
            }
            RebuildSlots();
        }

        private void RebuildSlots()
        {
            wp.Elements.Clear();
            if (CurrentBag?.personalInventory == null) return;

            string filter = sidebar != null ? sidebar.CurrentFilter : "All";
            Item[] inv = CurrentBag.personalInventory;

            int filledCount = 0;
            int matchedSlotCount = 0;
            for (int i = 0; i < inv.Length; i++)
            {
                Item it = inv[i];
                if (it != null && !it.IsAir) filledCount++;

                bool pass = true;
                if (filter != "All")
                {
                    if (it == null || it.IsAir) pass = false;
                    else if (filter == "Terraria") pass = it.type < ItemID.Count;
                    else
                    {
                        ModItem modIt = ItemLoader.GetModItem(it.type);
                        pass = (modIt?.Mod?.Name ?? "TPML") == filter;
                    }
                }

                if (pass)
                {
                    wp.Append(new AccessoryBagSlot(CurrentBag, i));
                    matchedSlotCount++;
                }
            }

            // 计算网格与窗口自适应尺寸
            bool showSidebar = sidebar != null && sidebar.HasMultipleMods;
            sidebar.Width.Set(showSidebar ? 42f : 0f, 0);

            float gridW = SLOTS_PER_ROW * (SLOT_SIZE + SLOT_MARGIN) - SLOT_MARGIN; // 436px
            int rowCount = Math.Max(1, (int)Math.Ceiling((double)matchedSlotCount / SLOTS_PER_ROW));
            int visibleRows = Math.Min(rowCount, MAX_VISIBLE_ROWS);
            float gridH = visibleRows * (SLOT_SIZE + SLOT_MARGIN) - SLOT_MARGIN; // 7 行约 304px

            bool needScrollbar = rowCount > MAX_VISIBLE_ROWS;
            if (needScrollbar)
            {
                if (scrollbar.Parent != contentArea) contentArea.Append(scrollbar);
            }
            else
            {
                if (scrollbar.Parent == contentArea) contentArea.RemoveChild(scrollbar);
            }

            float sidebarOffset = showSidebar ? 46f : 0f;
            uiList.Left.Set(sidebarOffset, 0);
            uiList.Width.Set(gridW + (needScrollbar ? 20f : 0f), 0);

            float totalWinW = sidebarOffset + gridW + (needScrollbar ? 26f : 12f) + 16f;
            float totalWinH = 34f + gridH + 18f;

            Width.Set(totalWinW, 0);
            Height.Set(totalWinH, 0);

            if (capacityText != null)
            {
                int total = inv.Length;
                string t = $"已存: {filledCount}/{total}";
                if (AccessoryBagConfig.EnableEffectiveSlotsLimit.val)
                {
                    t += $" (生效前 {AccessoryBagConfig.EffectiveSlots.val} 格)";
                }
                capacityText.SetText(t);
                capacityText.TextColor = filledCount >= total ? Color.Gold : Color.LightGray;
            }

            Recalculate();
        }

        public void DepositAll()
        {
            Player player = Main.LocalPlayer;
            if (player?.inventory == null || CurrentBag?.personalInventory == null) return;

            bool moved = false;
            Item[] pInv = player.inventory;
            Item[] bInv = CurrentBag.personalInventory;

            for (int i = 10; i < 50; i++)
            {
                Item pIt = pInv[i];
                if (pIt == null || pIt.IsAir || pIt.favorited || !pIt.accessory) continue;

                // 尝试存入
                for (int j = 0; j < bInv.Length; j++)
                {
                    if (bInv[j] == null || bInv[j].IsAir)
                    {
                        bInv[j] = pIt.Clone();
                        pInv[i] = new Item();
                        moved = true;
                        break;
                    }
                }
            }

            if (moved)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                CurrentBag.TriggerSlotsChanged();
            }
        }

        public void QuickStack()
        {
            Player player = Main.LocalPlayer;
            if (player?.inventory == null || CurrentBag?.personalInventory == null) return;

            bool moved = false;
            Item[] pInv = player.inventory;
            Item[] bInv = CurrentBag.personalInventory;

            for (int i = 10; i < 50; i++)
            {
                Item pIt = pInv[i];
                if (pIt == null || pIt.IsAir || pIt.favorited || !pIt.accessory) continue;

                for (int j = 0; j < bInv.Length; j++)
                {
                    Item bIt = bInv[j];
                    if (bIt != null && !bIt.IsAir && bIt.type == pIt.type && bIt.stack < bIt.maxStack && Item.CanStack(bIt, pIt))
                    {
                        int take = Math.Min(pIt.stack, bIt.maxStack - bIt.stack);
                        bIt.stack += take;
                        pIt.stack -= take;
                        moved = true;
                        if (pIt.stack <= 0)
                        {
                            pInv[i] = new Item();
                            break;
                        }
                    }
                }
            }

            if (moved)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                CurrentBag.TriggerSlotsChanged();
            }
        }

        public void LootAll()
        {
            Player player = Main.LocalPlayer;
            if (player?.inventory == null || CurrentBag?.personalInventory == null) return;

            bool moved = false;
            Item[] bInv = CurrentBag.personalInventory;

            for (int i = 0; i < bInv.Length; i++)
            {
                Item bIt = bInv[i];
                if (bIt == null || bIt.IsAir) continue;

                int orig = bIt.stack;
                bInv[i] = player.GetItem(bIt, GetItemSettings.QuickTransferFromSlot);
                if (bInv[i] == null) bInv[i] = new Item();
                if (bInv[i].stack != orig) moved = true;
            }

            if (moved)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                CurrentBag.TriggerSlotsChanged();
            }
        }

        public void SortBag()
        {
            if (CurrentBag?.personalInventory == null) return;
            Item[] bInv = CurrentBag.personalInventory;

            var items = new List<Item>();
            var favPositions = new Dictionary<int, Item>();

            for (int i = 0; i < bInv.Length; i++)
            {
                if (bInv[i] != null && !bInv[i].IsAir)
                {
                    if (bInv[i].favorited) favPositions[i] = bInv[i];
                    else items.Add(bInv[i]);
                }
            }

            items.Sort((x, y) =>
            {
                if (x.rare != y.rare) return y.rare.CompareTo(x.rare);
                if (x.value != y.value) return y.value.CompareTo(x.value);
                return x.type.CompareTo(y.type);
            });

            int listIdx = 0;
            for (int i = 0; i < bInv.Length; i++)
            {
                if (favPositions.ContainsKey(i))
                {
                    bInv[i] = favPositions[i];
                }
                else if (listIdx < items.Count)
                {
                    bInv[i] = items[listIdx++];
                }
                else
                {
                    bInv[i] = new Item();
                }
            }

            SoundEngine.PlaySound(SoundID.Grab);
            CurrentBag.TriggerSlotsChanged();
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            if (scrollbar != null && evt.ScrollWheelValue != 0)
            {
                scrollbar.ViewPosition -= evt.ScrollWheelValue;
                Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = 0;
                Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsOpen && ModifyInterfaceLayers.IsHoveringWindow(this))
            {
                Main.LocalPlayer.mouseInterface = true;

                int delta = Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI;
                if (delta == 0) delta = Terraria.GameInput.PlayerInput.ScrollWheelDelta;

                if (delta != 0 && scrollbar != null)
                {
                    scrollbar.ViewPosition -= delta;
                    Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = 0;
                    Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;
                }
            }
        }
    }

    internal class UIBoxWrapPanel : UIElement
    {
        public int ItemMargin { get; set; } = 4;

        public override void RecalculateChildren()
        {
            float width = Parent != null ? Parent.GetDimensions().Width : GetDimensions().Width;
            if (width <= 0) return;

            float x = 0;
            float y = 0;
            float rowHeight = 0;

            foreach (UIElement item in Elements)
            {
                item.Recalculate();
                float iw = item.Width.Pixels;
                float ih = item.Height.Pixels;

                if (x + iw > width && x > 0)
                {
                    x = 0;
                    y += rowHeight + ItemMargin;
                    rowHeight = 0;
                }

                item.Left.Set(x, 0);
                item.Top.Set(y, 0);
                x += iw + ItemMargin;
                if (ih > rowHeight) rowHeight = ih;
                item.Recalculate();
            }

            Height.Set(y + rowHeight, 0);
        }
    }

    internal class UIBoxButton : UIPanel
    {
        public event Action OnClick;
        private readonly Func<string> tooltip;
        private readonly Func<string> iconPathFunc;

        public UIBoxButton(int size, Func<string> tooltip, string iconPath)
            : this(size, tooltip, () => iconPath) { }

        public UIBoxButton(int size, Func<string> tooltip, Func<string> iconPathFunc)
        {
            this.tooltip = tooltip;
            this.iconPathFunc = iconPathFunc;

            Width.Set(size, 0);
            Height.Set(size, 0);
            SetPadding(0);
            BackgroundColor = BorderColor = new Color(43, 60, 120);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            OnClick?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            CalculatedStyle dim = GetDimensions();
            string path = iconPathFunc?.Invoke();
            if (!string.IsNullOrEmpty(path))
            {
                Asset<Texture2D> asset = Main.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
                if (asset?.Value != null)
                {
                    Texture2D tex = asset.Value;
                    float maxSide = Math.Max(tex.Width, tex.Height);
                    float scale = maxSide > 16f ? 16f / maxSide : 1f;
                    Vector2 origin = tex.Size() / 2f;
                    Vector2 pos = new Vector2(dim.X + dim.Width / 2f, dim.Y + dim.Height / 2f);
                    sb.Draw(tex, pos, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
                }
            }

            if (IsMouseHovering && tooltip != null)
            {
                Main.hoverItemName = tooltip();
            }
        }
    }
}
