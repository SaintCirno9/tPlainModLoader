using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using OptimizeAndTool.Content.Storage.Core;
using TPML.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using UITextBox = TPML.UI.UITextBox;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 通用大型容器 UI 窗口：
    /// 精确对齐饰品包经典原生质感与自适应排版，杜绝左右空隙，具备原版物品栏材质、智能 Mod 侧边栏、16 大细分类多行网格、独立搜索工具栏、分类空格手动存入与分类精准提取。
    /// 作者: SaintCirno9
    /// </summary>
    public class UniversalBagWindow : UIWindow
    {
        public IBagInventory CurrentBag { get; private set; }

        public const int SLOTS_PER_ROW = 10;
        public const int MAX_VISIBLE_ROWS = 7;
        public const float SLOT_SIZE = 40f;
        public const float SLOT_MARGIN = 4f;

        private UIElement topToolbar = null;
        private UIElement categoryToolbar = null;
        private UIElement searchToolbar = null;
        private UIElement contentArea = null;
        private UIBoxWrapPanel wp = null;
        private UIList uiList = null;
        private UIScrollbar scrollbar = null;
        private ModIconSidebar sidebar = null;
        private UIText capacityText = null;

        // 分类与拼音检索组件
        private UITextBox searchTextBox = null;
        private UIClearButton btnSearchClear = null;
        private BagItemCategory currentCategory = BagItemCategory.All;
        private List<UIBoxButton> categoryButtons = new List<UIBoxButton>();
        private string searchQuery = string.Empty;
        private string searchPendingQuery = string.Empty;
        private int searchDebounceTimer = 0;

        public BagItemCategory CurrentCategory => currentCategory;
        public string SearchQuery => searchQuery;

        public UniversalBagWindow(string defaultTitle = "收纳容器") : base(defaultTitle, 476, 420)
        {
            // 移除右下角缩放手柄，保持原生紧凑自适应尺寸
            foreach (UIElement el in Elements)
            {
                if (el is UIImage img && el != Child)
                {
                    RemoveChild(el);
                    break;
                }
            }

            float toolbarTopMargin = 6f;
            float toolbarHeight = 22f;
            float catToolbarTopMargin = 4f;
            float catToolbarHeight = 24f; // 单行分类网格 (24px)
            float searchToolbarTopMargin = 4f;
            float searchToolbarHeight = 22f;

            // 1. 第一行：顶部操作按钮栏（右侧预留 26px 避让右上角 X 关闭按钮）
            topToolbar = new UIElement();
            topToolbar.Width.Set(-26f, 1f);
            topToolbar.Top.Set(toolbarTopMargin, 0);
            topToolbar.Height.Set(toolbarHeight, 0);
            Child.Append(topToolbar);

            // 2. 第二行：16 大细分类独占多行网格
            categoryToolbar = new UIElement();
            categoryToolbar.Width.Set(0, 1);
            categoryToolbar.Top.Set(toolbarTopMargin + toolbarHeight + catToolbarTopMargin, 0);
            categoryToolbar.Height.Set(catToolbarHeight, 0);
            Child.Append(categoryToolbar);

            // 3. 第三行：搜索工具栏（独占一行）
            searchToolbar = new UIElement();
            searchToolbar.Width.Set(0, 1);
            searchToolbar.Top.Set(toolbarTopMargin + toolbarHeight + catToolbarTopMargin + catToolbarHeight + searchToolbarTopMargin, 0);
            searchToolbar.Height.Set(searchToolbarHeight, 0);
            Child.Append(searchToolbar);

            // 4. 内容展示区（网格 + 侧边栏 + 滚动条）
            float topOffset = toolbarTopMargin + toolbarHeight + catToolbarTopMargin + catToolbarHeight + searchToolbarTopMargin + searchToolbarHeight + 6f;
            contentArea = new UIElement();
            contentArea.Width.Set(0, 1);
            contentArea.Top.Set(topOffset, 0);
            contentArea.Height.Set(-topOffset, 1);
            Child.Append(contentArea);

            sidebar = new ModIconSidebar(null);
            sidebar.OnFilterChanged += _ => RebuildSlots();

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(0, 1);

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
                    CurrentBag.OnSlotsChanged -= RequestRebuild;
                }
            };
        }

        private bool _needsRebuild = false;

        public void RequestRebuild()
        {
            _needsRebuild = true;
        }

        public void Open(IBagInventory bag, UIState parentState)
        {
            if (CurrentBag != null)
            {
                CurrentBag.OnSlotsChanged -= RequestRebuild;
            }

            CurrentBag = bag;
            if (CurrentBag != null)
            {
                CurrentBag.OnSlotsChanged += RequestRebuild;
                if (ui_title != null)
                {
                    ui_title.SetText(CurrentBag.Title);
                }
            }

            if (!Main.playerInventory)
            {
                Main.playerInventory = true;
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }

            BuildTopToolbar();
            BuildCategoryToolbar();
            BuildSearchToolbar();
            Open(parentState);
            Rebuild();
        }

        public void Toggle(IBagInventory bag, UIState parentState)
        {
            if (bag == null) return;

            if (IsOpen && CurrentBag == bag)
            {
                Close();
            }
            else
            {
                Open(bag, parentState);
            }
        }

        private void BuildTopToolbar()
        {
            topToolbar.Elements.Clear();
            if (CurrentBag == null) return;

            int height = 22;

            UIStackPanel sp = new UIStackPanel();
            sp.Height.Set(height, 0);
            sp.VAlign = 0.5f;
            sp.IsAutoUpdateSize = true;
            sp.Horizontal = true;
            sp.ItemMargin = 6;
            topToolbar.Append(sp);

            // 1. 全部存入
            UIBoxButton btnDeposit = new UIBoxButton(height, () => "一键存入背包中所有非快捷栏、非收藏物品", "Images/UI/Cursor_4");
            btnDeposit.OnClick += () => CurrentBag?.DepositAll(Main.LocalPlayer);
            sp.Append(btnDeposit);

            // 2. 快速堆叠
            UIBoxButton btnQuickStack = new UIBoxButton(height, () => "一键向容器快速堆叠已有物品", "Images/UI/Cursor_8");
            btnQuickStack.OnClick += () => CurrentBag?.QuickStack(Main.LocalPlayer);
            sp.Append(btnQuickStack);

            // 3. 全部取出（智能联动当前分类与搜索筛选）
            UIBoxButton btnLoot = new UIBoxButton(
                height,
                () => IsActiveFilter() ? "一键取出当前筛选/分类下的所有物品回个人背包" : "一键取出容器中所有物品回个人背包",
                () => "Images/UI/Cursor_6"
            );
            btnLoot.OnClick += () =>
            {
                if (CurrentBag == null) return;
                if (!IsActiveFilter())
                {
                    CurrentBag.LootAll(Main.LocalPlayer, null);
                }
                else
                {
                    string currentMod = sidebar != null ? sidebar.CurrentFilter : "All";
                    BagItemCategory targetCategory = currentCategory;
                    string query = searchQuery;

                    Func<Item, bool> filterPredicate = (it) =>
                    {
                        if (it == null || it.IsAir) return false;

                        if (currentMod != "All")
                        {
                            if (currentMod == "Terraria")
                            {
                                if (it.type >= ItemID.Count) return false;
                            }
                            else
                            {
                                ModItem modIt = ItemLoader.GetModItem(it.type);
                                if ((modIt?.Mod?.Name ?? "TPML") != currentMod) return false;
                            }
                        }

                        if (targetCategory != BagItemCategory.All)
                        {
                            if (!BagCategoryHelper.MatchesCategory(it, targetCategory)) return false;
                        }

                        if (!string.IsNullOrWhiteSpace(query))
                        {
                            if (!BagCategoryHelper.MatchesSearch(it, query)) return false;
                        }

                        return true;
                    };

                    CurrentBag.LootAll(Main.LocalPlayer, filterPredicate);
                }
            };
            sp.Append(btnLoot);

            // 4. 智能整理排序（收藏 Favorited 置顶）
            UIBoxButton btnSort = new UIBoxButton(height, () => "一键智能整理并排序容器物品（收藏锁定物品自动置顶）", "Images/UI/Cursor_9");
            btnSort.OnClick += () => CurrentBag?.Sort();
            sp.Append(btnSort);

            // 5. 外观显隐扩展按钮（若实现 IIVisualToggleable）
            if (CurrentBag is IVisualToggleable vis)
            {
                UIBoxButton btnAllVisuals = new UIBoxButton(
                    height,
                    () => "一键切换所有物品的外观可见性 (全部显示/全部隐藏)",
                    () => vis.HasAnyVisibleVisuals() ? "Images/UI/InfoIcon_0" : "Images/UI/InfoIcon_5"
                );
                btnAllVisuals.OnClick += () =>
                {
                    vis.ToggleAllVisuals();
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    CurrentBag.TriggerSlotsChanged();
                };
                sp.Append(btnAllVisuals);
            }

            // 6. 自定义扩展工具栏按钮（若实现 IToolbarCustomActions）
            if (CurrentBag is IToolbarCustomActions customActions)
            {
                var buttons = customActions.GetCustomToolbarButtons();
                if (buttons != null)
                {
                    foreach (var btnDef in buttons)
                    {
                        if (btnDef == null) continue;
                        UIBoxButton btn = new UIBoxButton(height, btnDef.TooltipFunc, btnDef.IconPathFunc, btnDef.ColorFunc);
                        btn.OnClick += btnDef.OnClick;
                        sp.Append(btn);
                    }
                }
            }

            // 7. 统计文本（靠右对齐）
            capacityText = new UIText("0/0", 0.8f);
            capacityText.VAlign = 0.5f;
            capacityText.HAlign = 1f;
            topToolbar.Append(capacityText);
        }

        private bool IsActiveFilter()
        {
            string modFilter = sidebar != null ? sidebar.CurrentFilter : "All";
            return currentCategory != BagItemCategory.All ||
                   !string.IsNullOrWhiteSpace(searchQuery) ||
                   modFilter != "All";
        }

        private void BuildCategoryToolbar()
        {
            categoryToolbar.Elements.Clear();
            categoryButtons.Clear();

            if (CurrentBag == null || !CurrentBag.ShowFilterBar)
            {
                if (categoryToolbar.Parent == Child) Child.RemoveChild(categoryToolbar);
                return;
            }

            if (categoryToolbar.Parent != Child) Child.Append(categoryToolbar);

            int btnSize = 24;

            var categories = new (BagItemCategory cat, string desc, string icon)[]
            {
                (BagItemCategory.All, "全部物品", "Images/Item_2712"),
                (BagItemCategory.Weapon, "武器（近战/远程/魔法/召唤）", "Images/Item_3507"),
                (BagItemCategory.Tool, "工具（镐/斧/锤/钓竿/扳手等）", "Images/Item_3509"),
                (BagItemCategory.Armor, "防具（头盔/胸甲/护腿）", "Images/Item_895"),
                (BagItemCategory.Accessory, "饰品（配饰/坐骑/宠物/钩爪）", "Images/Item_54"),
                (BagItemCategory.VanityDye, "时装与染料（外观衣物/各色染料）", "Images/Item_2873"),
                (BagItemCategory.Potion, "药水与食物（治疗/魔力/增益/食物）", "Images/Item_296"),
                (BagItemCategory.Ammo, "弹药（箭矢/子弹/火箭/飞镖）", "Images/Item_40"),
                (BagItemCategory.Bait, "鱼饵（各类诱饵/钓鱼昆虫等）", "Images/Item_2676"),
                (BagItemCategory.Tile, "物块与建筑（方块/墙壁/平台）", "Images/Item_2"),
                (BagItemCategory.Furniture, "家具与装饰（桌椅床门/箱子/挂画）", "Images/Item_362"),
                (BagItemCategory.Statue, "雕像（怪物/掉落/文字/功能/装饰雕像）", "Images/Item_473"),
                (BagItemCategory.Summon, "召唤物与信物（Boss召唤物/天界符/事件信物）", "Images/Item_3601"),
                (BagItemCategory.Light, "光源与照明（火把/荧光棒/提灯/蜡烛）", "Images/Item_8"),
                (BagItemCategory.Material, "合成素材（矿石/锭/灵魂/制作材料）", "Images/Item_706"),
                (BagItemCategory.Misc, "杂项与消耗（钱币/宝藏袋/宝匣/礼包/其他）", "Images/Item_73")
            };

            for (int i = 0; i < categories.Length; i++)
            {
                var (cat, desc, icon) = categories[i];
                BagItemCategory targetCat = cat;

                UIBoxButton btn = new UIBoxButton(
                    btnSize,
                    () => $"{desc} {(currentCategory == targetCat ? "[已选中]" : "")}",
                    () => icon,
                    () => currentCategory == targetCat ? Color.White : new Color(180, 180, 180),
                    () => currentCategory == targetCat
                );

                btn.HAlign = (float)i / (categories.Length - 1);
                btn.VAlign = 0.5f;

                btn.OnClick += () =>
                {
                    if (currentCategory == targetCat)
                    {
                        currentCategory = BagItemCategory.All;
                    }
                    else
                    {
                        currentCategory = targetCat;
                    }
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    RebuildSlots();
                };

                categoryButtons.Add(btn);
                categoryToolbar.Append(btn);
            }
        }

        private void BuildSearchToolbar()
        {
            searchToolbar.Elements.Clear();

            if (CurrentBag == null || !CurrentBag.ShowFilterBar)
            {
                if (searchToolbar.Parent == Child) Child.RemoveChild(searchToolbar);
                return;
            }

            if (searchToolbar.Parent != Child) Child.Append(searchToolbar);

            int height = 22;

            // 搜索框（独占整行，铺开更宽裕）
            searchTextBox = new UITextBox("搜索物品名称 / 拼音首字母 / ID / 词条...");
            searchTextBox.Width.Set(-30, 1);
            searchTextBox.Height.Set(height, 0);
            searchTextBox.Left.Set(0, 0);
            searchTextBox.VAlign = 0.5f;
            searchTextBox.TextScale = 0.75f;
            searchTextBox.Text_MaxLength = 50;
            searchTextBox.SetPadding(3);
            searchTextBox.Text = searchQuery;
            searchTextBox.OnTextChanged += (text) =>
            {
                searchDebounceTimer = 2;
                searchPendingQuery = text;
            };
            searchTextBox.OnRightClick += (evt, el) => ClearSearch();
            searchToolbar.Append(searchTextBox);

            // 一键清空搜索按钮
            btnSearchClear = new UIClearButton(22, () => "清空搜索内容 (亦可右键搜索框清空)");
            btnSearchClear.Height.Set(height, 0);
            btnSearchClear.Left.Set(-24, 1);
            btnSearchClear.VAlign = 0.5f;
            btnSearchClear.OnClick += ClearSearch;
            searchToolbar.Append(btnSearchClear);
        }

        public void ClearSearch()
        {
            if (searchTextBox != null)
            {
                searchTextBox.Text = string.Empty;
            }
            searchQuery = string.Empty;
            searchPendingQuery = string.Empty;
            searchDebounceTimer = 0;
            SoundEngine.PlaySound(SoundID.MenuTick);
            RebuildSlots();
        }

        public void Rebuild()
        {
            if (sidebar != null && CurrentBag != null)
            {
                sidebar.SetBag(CurrentBag);
                sidebar.Rebuild();
            }
            if (ui_title != null && CurrentBag != null)
            {
                ui_title.SetText(CurrentBag.Title);
            }
            RebuildSlots();
        }

        private void RebuildSlots()
        {
            wp.Elements.Clear();
            if (CurrentBag?.Slots == null) return;

            bool showFilter = CurrentBag.ShowFilterBar;
            if (showFilter)
            {
                if (categoryToolbar.Parent != Child) Child.Append(categoryToolbar);
                if (searchToolbar.Parent != Child) Child.Append(searchToolbar);
            }
            else
            {
                if (categoryToolbar.Parent == Child) Child.RemoveChild(categoryToolbar);
                if (searchToolbar.Parent == Child) Child.RemoveChild(searchToolbar);
            }

            float toolbarTopMargin = 6f;
            float toolbarHeight = 22f;
            float catToolbarTopMargin = 4f;
            float catToolbarHeight = 24f;
            float searchToolbarTopMargin = 4f;
            float searchToolbarHeight = 22f;

            float topOffset = showFilter
                ? (toolbarTopMargin + toolbarHeight + catToolbarTopMargin + catToolbarHeight + searchToolbarTopMargin + searchToolbarHeight + 6f)
                : (toolbarTopMargin + toolbarHeight + 6f);

            contentArea.Top.Set(topOffset, 0);
            contentArea.Height.Set(-topOffset, 1);

            string modFilter = sidebar != null ? sidebar.CurrentFilter : "All";
            Item[] inv = CurrentBag.Slots;
            bool isFiltering = IsActiveFilter();

            int filledCount = 0;
            List<int> displaySlotIndices = new List<int>();

            for (int i = 0; i < inv.Length; i++)
            {
                Item it = inv[i];
                if (it != null && !it.IsAir) filledCount++;

                if (!isFiltering)
                {
                    // 未处于任何筛选状态时，展示全部槽位（含空格）
                    displaySlotIndices.Add(i);
                }
                else
                {
                    // 处于筛选/分类状态时，先收集命中的非空物品
                    if (it != null && !it.IsAir)
                    {
                        bool pass = true;

                        // 1. Mod 来源筛选
                        if (modFilter != "All")
                        {
                            if (modFilter == "Terraria") pass = it.type < ItemID.Count;
                            else
                            {
                                ModItem modIt = ItemLoader.GetModItem(it.type);
                                pass = (modIt?.Mod?.Name ?? "TPML") == modFilter;
                            }
                        }

                        // 2. 物品分类筛选
                        if (pass && currentCategory != BagItemCategory.All)
                        {
                            pass = BagCategoryHelper.MatchesCategory(it, currentCategory);
                        }

                        // 3. 拼音/ID/词条搜索匹配
                        if (pass && !string.IsNullOrWhiteSpace(searchQuery))
                        {
                            pass = BagCategoryHelper.MatchesSearch(it, searchQuery);
                        }

                        if (pass)
                        {
                            displaySlotIndices.Add(i);
                        }
                    }
                }
            }

            int matchedCount = displaySlotIndices.Count;

            // 关键优化：在处于筛选/分类状态时，紧跟匹配物品末尾追加 10 个可用空格，方便手动存入并自动归类
            if (isFiltering)
            {
                if (CurrentBag.IsDynamicCapacity)
                {
                    CurrentBag.EnsureTrailingEmptySlots(10);
                    inv = CurrentBag.Slots;
                }

                int emptyAppended = 0;
                for (int i = 0; i < inv.Length && emptyAppended < 10; i++)
                {
                    if (inv[i] == null || inv[i].IsAir)
                    {
                        displaySlotIndices.Add(i);
                        emptyAppended++;
                    }
                }
            }

            foreach (int slotIdx in displaySlotIndices)
            {
                wp.Append(new UniversalBagSlot(CurrentBag, slotIdx));
            }

            // 计算网格与窗口自适应尺寸
            bool showSidebar = sidebar != null && sidebar.HasMultipleMods;
            if (showSidebar)
            {
                if (sidebar.Parent != contentArea) contentArea.Append(sidebar);
                sidebar.Left.Set(0, 0);
                sidebar.Width.Set(42f, 0);
            }
            else
            {
                if (sidebar != null && sidebar.Parent == contentArea) contentArea.RemoveChild(sidebar);
            }

            float gridW = SLOTS_PER_ROW * (SLOT_SIZE + SLOT_MARGIN) - SLOT_MARGIN; // 436px
            int totalDisplaySlots = displaySlotIndices.Count;
            int rowCount = Math.Max(1, (int)Math.Ceiling((double)totalDisplaySlots / SLOTS_PER_ROW));
            int visibleRows = MAX_VISIBLE_ROWS; // 锁定固定 7 行高度，杜绝筛选/搜索少量物品时窗口高度塌陷与视觉抖动，并为 Mod 侧栏提供充足展示空间
            float gridH = visibleRows * (SLOT_SIZE + SLOT_MARGIN) - SLOT_MARGIN; // 7 行约 304px

            bool needScrollbar = rowCount > MAX_VISIBLE_ROWS;
            float sidebarOffset = showSidebar ? 46f : 0f;

            if (needScrollbar)
            {
                if (scrollbar.Parent != contentArea) contentArea.Append(scrollbar);
                scrollbar.Left.Set(sidebarOffset + gridW + 4f, 0);
                scrollbar.Width.Set(20f, 0);
                scrollbar.Top.Set(6f, 0);
                scrollbar.Height.Set(gridH - 12f, 0);
            }
            else
            {
                if (scrollbar.Parent == contentArea) contentArea.RemoveChild(scrollbar);
            }

            contentArea.Height.Set(gridH, 0);
            uiList.Left.Set(sidebarOffset, 0);
            uiList.Width.Set(gridW, 0);
            uiList.Height.Set(gridH, 0);

            float totalWinW = sidebarOffset + gridW + (needScrollbar ? 24f : 0f) + 20f;
            float totalWinH = topOffset + gridH + 44f;

            Width.Set(totalWinW, 0);
            Height.Set(totalWinH, 0);

            if (capacityText != null)
            {
                if (isFiltering)
                {
                    capacityText.SetText($"匹配: {matchedCount} | 已存: {filledCount}/{inv.Length}");
                    capacityText.TextColor = Color.LightSkyBlue;
                }
                else
                {
                    string t = CurrentBag.GetCapacityText();
                    if (string.IsNullOrEmpty(t))
                    {
                        t = $"已存: {filledCount}/{inv.Length}";
                    }
                    capacityText.SetText(t);
                    capacityText.TextColor = filledCount >= inv.Length ? Color.Gold : Color.LightGray;
                }
            }

            Recalculate();
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

            // 搜索输入防抖处理
            if (searchDebounceTimer > 0)
            {
                searchDebounceTimer--;
                if (searchDebounceTimer == 0)
                {
                    searchQuery = searchPendingQuery;
                    RebuildSlots();
                }
            }

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

            // 帧末安全执行脏标记重构，彻底杜绝在 UIElement.Update 递归遍历子控件时修改集合
            if (_needsRebuild)
            {
                _needsRebuild = false;
                Rebuild();
            }
        }
    }

    /// <summary>
    /// 自适应换行排版容器
    /// </summary>
    public class UIBoxWrapPanel : UIElement
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

    /// <summary>
    /// 通用容器方形操作按钮控件（支持图标、悬停提示、选中高亮与左右键交互）
    /// </summary>
    public class UIBoxButton : UIPanel
    {
        public event Action OnClick;
        public new event Action OnRightClick;
        private readonly Func<string> tooltip;
        private readonly Func<string> iconPathFunc;
        private readonly Func<Color> colorFunc;
        private readonly Func<bool> isActiveFunc;

        public UIBoxButton(int size, Func<string> tooltip, string iconPath, Func<Color> colorFunc = null, Func<bool> isActiveFunc = null)
            : this(size, tooltip, () => iconPath, colorFunc, isActiveFunc) { }

        public UIBoxButton(int size, Func<string> tooltip, Func<string> iconPathFunc, Func<Color> colorFunc = null, Func<bool> isActiveFunc = null)
        {
            this.tooltip = tooltip;
            this.iconPathFunc = iconPathFunc;
            this.colorFunc = colorFunc;
            this.isActiveFunc = isActiveFunc;

            Width.Set(size, 0);
            Height.Set(size, 0);
            SetPadding(0);
            BackgroundColor = BorderColor = new Color(43, 60, 120);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            OnClick?.Invoke();
        }

        public override void RightClick(UIMouseEvent evt)
        {
            OnRightClick?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            bool active = isActiveFunc != null && isActiveFunc();
            BackgroundColor = active ? new Color(60, 95, 185) : (IsMouseHovering ? new Color(55, 80, 150) : new Color(43, 60, 120));
            BorderColor = active ? Color.Gold : (IsMouseHovering ? Color.White : new Color(43, 60, 120));

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
                    Color drawColor = colorFunc != null ? colorFunc() : (active ? Color.White : new Color(220, 220, 220));
                    sb.Draw(tex, pos, null, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
                }
            }

            if (IsMouseHovering && tooltip != null)
            {
                Main.hoverItemName = tooltip();
            }
        }
    }

    /// <summary>
    /// 微型清空按钮控件
    /// </summary>
    public class UIClearButton : UIPanel
    {
        public event Action OnClick;
        private readonly Func<string> tooltip;

        public UIClearButton(int size, Func<string> tooltip)
        {
            this.tooltip = tooltip;
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
            BackgroundColor = IsMouseHovering ? new Color(150, 50, 50) : new Color(43, 60, 120);
            BorderColor = IsMouseHovering ? Color.Gold : new Color(43, 60, 120);
            base.DrawSelf(sb);

            CalculatedStyle dim = GetDimensions();
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string text = "×";
            Vector2 textSize = font.MeasureString(text) * 0.75f;
            Vector2 pos = new Vector2(dim.X + (dim.Width - textSize.X) / 2f, dim.Y + (dim.Height - textSize.Y) / 2f);
            sb.DrawString(font, text, pos, Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);

            if (IsMouseHovering && tooltip != null)
            {
                Main.hoverItemName = tooltip();
            }
        }
    }
}

