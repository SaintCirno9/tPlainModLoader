using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MapAtlasTool.Utils;
using System;
using System.Collections.Generic;
using tContentPatch;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using TPML.Core.Pinyin;
using UITextBox = tContentPatch.Content.UI.UITextBox;

namespace MapAtlasTool.Content.UI
{
    /// <summary>
    /// 全屏大地图侧边栏面板: 顶部分类开关栏 + 快捷工具条 + 统一搜索(结构/NPC/箱子物品, 支持拼音) + 专用结果列表 + 飞视图。
    /// 仅在 Main.mapFullscreen 时驱动自有 UserInterface (全屏地图态原版 UI 层不活动)。
    /// 作者: SaintCirno9
    /// </summary>
    public class MapAtlasPanel : PatchMain
    {
        /// <summary>面板展开状态(随 setting.json 持久化)</summary>
        internal static GetSetReset<bool> PanelOpen = new GetSetReset<bool>(false, false);

        /// <summary>当前生效的搜索词(防抖后)</summary>
        internal static string ActiveQuery = "";

        /// <summary>搜索命中: 箱子索引集合 / 结构与NPC名称集合(供 StructureMarker 高亮判定; 搜索权威, 无视分类开关)</summary>
        internal static HashSet<int> HitChestIndexes = new HashSet<int>();
        internal static HashSet<string> HitTexts = new HashSet<string>(StringComparer.Ordinal);

        public static bool HasActiveQuery => !string.IsNullOrWhiteSpace(ActiveQuery);

        internal static void ClearSearchState()
        {
            ActiveQuery = "";
            HitChestIndexes.Clear();
            HitTexts.Clear();
        }

        private const int DebounceFrames = 18;
        private const int MaxStructureEntries = 100;
        private const int MaxChestEntries = 200;

        private static UserInterface _ui = null;
        private static PanelRoot _root = null;
        private static PanelWindow _window = null;
        private static int _debounceTimer = 0;
        private static string _pendingQuery = "";
        private static bool _lastOpenState = false;

        // 帧输入快照: UpdatePostfix 吞掉地图输入后, DrawMapPostfix 恢复给窗口交互
        private static bool _inputSwallowed = false;
        private static bool _frameMouseLeft = false;
        private static bool _frameMouseLeftRelease = false;
        private static bool _wasMapFullscreen = false;

        public static void RefreshStatsOnMainThread()
        {
            _window?.RefreshStats();
        }

        public override void UpdatePostfix(GameTime gameTime)
        {
            // 关闭大地图时顺便关闭面板界面（搜索词与高亮状态仍永久保留）
            if (_wasMapFullscreen && !Main.mapFullscreen)
            {
                if (PanelOpen.val)
                {
                    PanelOpen.val = false;
                    _lastOpenState = false;
                    _window?.Close();
                }
            }
            _wasMapFullscreen = Main.mapFullscreen;

            // 全屏地图 + 面板打开时, 若鼠标悬停面板则吞掉地图输入(拖拽/双击 Ping);
            // 窗口自身交互在 DrawMapPostfix 恢复真实输入后处理
            _inputSwallowed = false;
            if (!Main.mapFullscreen || _window == null || !PanelOpen.val) return;

            _frameMouseLeft = Main.mouseLeft;
            _frameMouseLeftRelease = Main.mouseLeftRelease;
            if (IsMouseOverWindow())
            {
                Main.mouseLeft = false;
                Main.mouseLeftRelease = false;
                _inputSwallowed = true;
            }
        }

        public override void DrawMapPostfix(GameTime gameTime)
        {
            if (!Main.mapFullscreen || !Main.mapEnabled || !Main.mapReady) return;

            EnsureBuilt();
            if (_ui == null || _root == null) return;

            // 根容器跟随分辨率
            if (_root.Width.Pixels != Main.screenWidth || _root.Height.Pixels != Main.screenHeight)
            {
                _root.Width.Set(Main.screenWidth, 0);
                _root.Height.Set(Main.screenHeight, 0);
                _root.Recalculate();
            }

            // 搜索防抖
            if (_debounceTimer > 0 && --_debounceTimer == 0)
            {
                ApplySearch(_pendingQuery);
            }

            // 面板开关同步
            if (PanelOpen.val != _lastOpenState)
            {
                _lastOpenState = PanelOpen.val;
                if (_lastOpenState)
                {
                    _window.Open(_root);
                    _window.RefreshStats();
                    _window.RefreshCategoryButtons();
                    _root.Recalculate();
                    _window.Recalculate();
                }
                else
                {
                    _window.Close();
                }
            }

            // 恢复真实输入供窗口交互(拖动/点击)
            if (_inputSwallowed)
            {
                Main.mouseLeft = _frameMouseLeft;
                Main.mouseLeftRelease = _frameMouseLeftRelease;
                _inputSwallowed = false;
            }

            _ui.Update(gameTime);
            _ui.Draw(Main.spriteBatch, gameTime);

            if (!PanelOpen.val)
            {
                DrawToggleButton();
            }
        }

        // ---------- 搜索 ----------

        internal static void RequestSearch(string text)
        {
            _pendingQuery = text ?? "";
            _debounceTimer = DebounceFrames;
        }

        internal static void ClearSearch()
        {
            _pendingQuery = "";
            _debounceTimer = 0;
            if (_window != null) _window.SetSearchText("");
            ApplySearch("");
        }

        private static void ApplySearch(string query)
        {
            ActiveQuery = (query ?? "").Trim();
            HitChestIndexes.Clear();
            HitTexts.Clear();

            List<StructurePin> structHits = new List<StructurePin>();
            List<AtlasChestHit> chestHits = new List<AtlasChestHit>();

            if (ActiveQuery.Length > 0)
            {
                // 1. 静态结构与宝箱类型名(箱子是否命中由物品/箱名判定, 此处只筛非箱子 pin)
                StructurePin[] pins = StructureMarker.GetPinsSnapshot();
                foreach (StructurePin pin in pins)
                {
                    if (pin == null || pin.ChestIndex >= 0) continue;
                    if (PinyinHelper.Matches(pin.Name, ActiveQuery))
                    {
                        HitTexts.Add(pin.Name);
                        structHits.Add(pin);
                    }
                }
                structHits.Sort((a, b) => a.PositionInTiles.Y.CompareTo(b.PositionInTiles.Y));

                // 2. 受困/特殊 NPC 名单(动态图钉按名字命中高亮, 不进结果列表)
                foreach (NpcMarkerInfo info in StructureMarker.NpcMarkerTable)
                {
                    if (PinyinHelper.Matches(info.Name, ActiveQuery))
                    {
                        HitTexts.Add(info.Name);
                    }
                }

                // 3. 箱子(箱内物品名 + 自定义箱名, 实时校验)
                chestHits = ChestItemIndex.Query(ActiveQuery);
                chestHits.Sort((a, b) => (a.Y.CompareTo(b.Y) != 0) ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
                foreach (AtlasChestHit hit in chestHits)
                {
                    HitChestIndexes.Add(hit.ChestIndex);
                }
            }

            _window?.SetResults(ActiveQuery, structHits, chestHits);
        }

        /// <summary>飞至目标(原版地图平移动画, 不传送玩家)</summary>
        internal static void FlyMapFullscreenTo(Vector2 posInTiles)
        {
            if (!Main.mapFullscreen) return;
            Main.PanTargetMapFullscreenEnd = posInTiles;
            Main.PanTargetMapFullscreen = true;
        }

        // ---------- 构建 ----------

        private static void EnsureBuilt()
        {
            if (_ui != null) return;

            _root = new PanelRoot();
            _window = new PanelWindow();
            _ui = new UserInterface();
            _ui.SetState(_root);

            _root.Width.Set(Main.screenWidth, 0);
            _root.Height.Set(Main.screenHeight, 0);
            _root.Recalculate();
        }

        private static bool IsMouseOverWindow()
        {
            if (_window == null || !_window.IsOpen) return false;
            CalculatedStyle dim = _window.GetOuterDimensions();
            return new Rectangle((int)dim.X, (int)dim.Y, (int)dim.Width, (int)dim.Height)
                .Contains(PlayerInput.MouseX, PlayerInput.MouseY);
        }

        // 折叠态: 地图左上角展开按钮
        private static void DrawToggleButton()
        {
            const int size = 28;
            Rectangle rect = new Rectangle(10, 60, size, size);
            bool hover = rect.Contains(Main.mouseX, Main.mouseY);

            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.Draw(magicPixel, new Rectangle(rect.X - 2, rect.Y - 2, size + 4, size + 4),
                (hover ? Color.Gold : Color.DarkSlateGray) * 0.9f);
            Main.spriteBatch.Draw(magicPixel, rect, Color.Black * 0.75f);

            Texture2D icon = LoadItemIconSafe(ItemID.WorldGlobe);
            if (icon != null)
            {
                float iconScale = (size - 8f) / Math.Max(icon.Width, icon.Height);
                Main.spriteBatch.Draw(icon, new Vector2(rect.X + size / 2, rect.Y + size / 2), null,
                    Color.White, 0f, new Vector2(icon.Width / 2f, icon.Height / 2f), iconScale, SpriteEffects.None, 0f);
            }

            if (hover)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText("打开地图图鉴面板");
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    Main.mouseLeft = false;
                    PanelOpen.val = true;
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
            }
        }

        internal static Texture2D LoadItemIconSafe(int itemId)
        {
            if (itemId <= 0) return null;
            try
            {
                Main.instance.LoadItem(itemId);
                return TextureAssets.Item[itemId]?.Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>覆盖全屏的根容器(仅承载窗口)</summary>
        private class PanelRoot : UIState
        {
            public PanelRoot()
            {
                Width.Set(Main.screenWidth, 0);
                Height.Set(Main.screenHeight, 0);
            }
        }

        /// <summary>搜索结果条目: 双击飞至目标</summary>
        private class ResultEntry : UIItemMouseText
        {
            public ResultEntry(Texture2D ico, string title, string tooltip, Action onDoubleClick) : base(ico, title)
            {
                MouseText = tooltip;
                OnDoubleClickAction = onDoubleClick;
                OnLeftDoubleClick += (evt, el) => OnDoubleClickAction?.Invoke();
            }

            private Action OnDoubleClickAction;
        }

        /// <summary>
        /// 大类切换状态按钮 (带高亮边框与即时状态显示)
        /// </summary>
        private class CategoryToggleButton : UIPanel
        {
            private readonly UIText _label;
            private readonly string _categoryName;
            private readonly Func<bool> _getState;
            private readonly Action _onToggle;
            private readonly string _tooltip;

            public CategoryToggleButton(string categoryName, Func<bool> getState, Action onToggle, string tooltip)
            {
                _categoryName = categoryName;
                _getState = getState;
                _onToggle = onToggle;
                _tooltip = tooltip;

                SetPadding(0);
                _label = new UIText("", 0.75f);
                _label.HAlign = 0.5f;
                _label.VAlign = 0.5f;
                Append(_label);

                OnMouseOver += (evt, el) => SoundEngine.PlaySound(SoundID.MenuTick);
                OnLeftClick += (evt, el) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    _onToggle?.Invoke();
                    UpdateAppearance();
                };

                UpdateAppearance();
            }

            public void UpdateAppearance()
            {
                bool active = _getState != null && _getState();
                _label.SetText($"{_categoryName} [{(active ? "开" : "关")}]");
                _label.TextColor = active ? Color.White : new Color(160, 160, 160);

                if (active)
                {
                    BackgroundColor = new Color(35, 70, 125) * 0.95f;
                    BorderColor = new Color(110, 175, 255);
                }
                else
                {
                    BackgroundColor = new Color(30, 35, 48) * 0.85f;
                    BorderColor = new Color(65, 75, 95);
                }
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (IsMouseHovering && !string.IsNullOrEmpty(_tooltip))
                {
                    Main.instance.MouseText(_tooltip);
                }
            }
        }

        /// <summary>
        /// 紧凑型工具栏按钮 (全开/全关/重扫)
        /// </summary>
        private class CompactButton : UIPanel
        {
            private readonly UIText _label;
            private readonly Action _onClick;
            private readonly string _mouseText;

            public CompactButton(string text, Action onClick, string mouseText)
            {
                _onClick = onClick;
                _mouseText = mouseText;
                SetPadding(0);
                BackgroundColor = new Color(42, 52, 78) * 0.9f;
                BorderColor = new Color(80, 105, 155);

                _label = new UIText(text, 0.72f);
                _label.HAlign = 0.5f;
                _label.VAlign = 0.5f;
                Append(_label);

                OnMouseOver += (evt, el) =>
                {
                    BackgroundColor = new Color(65, 90, 145) * 0.95f;
                    BorderColor = Color.Gold;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                };
                OnMouseOut += (evt, el) =>
                {
                    BackgroundColor = new Color(42, 52, 78) * 0.9f;
                    BorderColor = new Color(80, 105, 155);
                };
                OnLeftClick += (evt, el) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    _onClick?.Invoke();
                };
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (IsMouseHovering && !string.IsNullOrEmpty(_mouseText))
                {
                    Main.instance.MouseText(_mouseText);
                }
            }
        }

        /// <summary>
        /// 统计文本(支持悬停提示)
        /// </summary>
        private class HoverText : UIText
        {
            public string HoverTooltip = null;

            public HoverText(string text, float textScale = 1) : base(text, textScale) { }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (IsMouseHovering && !string.IsNullOrEmpty(HoverTooltip))
                {
                    Main.instance.MouseText(HoverTooltip);
                }
            }
        }

        /// <summary>
        /// 搜索框一键清除微型按钮 (尺寸 24x26，默认深色，悬停红底金边，居中绘制白色 "×")
        /// </summary>
        private class UIClearButton : UIPanel
        {
            private readonly UIText _label;
            private readonly Action _onClick;

            public UIClearButton(Action onClick)
            {
                _onClick = onClick;
                SetPadding(0);
                BackgroundColor = new Color(42, 52, 78) * 0.9f;
                BorderColor = new Color(80, 105, 155);

                _label = new UIText("×", 0.85f);
                _label.HAlign = 0.5f;
                _label.VAlign = 0.5f;
                _label.TextColor = Color.White;
                Append(_label);

                OnMouseOver += (evt, el) =>
                {
                    BackgroundColor = new Color(150, 40, 40) * 0.95f;
                    BorderColor = Color.Gold;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                };
                OnMouseOut += (evt, el) =>
                {
                    BackgroundColor = new Color(42, 52, 78) * 0.9f;
                    BorderColor = new Color(80, 105, 155);
                };
                OnLeftClick += (evt, el) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    _onClick?.Invoke();
                };
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);
                if (IsMouseHovering)
                {
                    Main.instance.MouseText("清空搜索内容 (亦可右键搜索框清空)");
                }
            }
        }

        /// <summary>面板窗口 (加宽至 470px，高度 450px，专用搜索结果列表)</summary>
        private class PanelWindow : UIWindow
        {
            private List<CategoryToggleButton> _categoryButtons = new List<CategoryToggleButton>();
            private UITextBox _searchBox = null;
            private HoverText _statsText = null;
            private UIScrollViewer2 _resultsView = null;

            public PanelWindow() : base("地图图鉴", 470, 450)
            {
                MinWidth.Pixels = 420;
                MinHeight.Pixels = 320;
                Left.Set(14, 0);
                Top.Set(56, 0);
                Child.Top.Pixels = 42;
                Child.Height.Set(-52, 1f);

                OnClose += () =>
                {
                    PanelOpen.val = false;
                    _lastOpenState = false;
                };

                // 1. 第一行: 4 个大类切换开关栏 (Top = 4, Height = 24)
                UIElement categoryToolbar = new UIElement();
                categoryToolbar.Left.Set(0, 0);
                categoryToolbar.Top.Set(4, 0);
                categoryToolbar.Width.Set(0, 1f);
                categoryToolbar.Height.Set(24, 0);

                AddCategoryBtn(categoryToolbar, "结构", () => AtlasValSet.IsStructuresEnabled,
                    () => AtlasValSet.ToggleStructures(), "切换世界遗迹/矿藏/微群落标记（花苞/剑冢/神庙/微光/空岛/地牢等）", 0);
                AddCategoryBtn(categoryToolbar, "宝箱", () => AtlasValSet.IsChestsEnabled,
                    () => AtlasValSet.ToggleChests(), "切换全量宝箱雷达标记（地表/地下/环境神器/暗影箱）", 1);
                AddCategoryBtn(categoryToolbar, "NPC", () => AtlasValSet.IsNPCsEnabled,
                    () => AtlasValSet.ToggleNPCs(), "切换受困与特殊NPC实时动态追踪（哥布林/机械师/巫师等）", 2);
                AddCategoryBtn(categoryToolbar, "陷阱", () => AtlasValSet.IsTrapsEnabled,
                    () => AtlasValSet.ToggleTraps(), "切换地下炸药机关与陷阱/死人宝箱标记", 3);

                Child.Append(categoryToolbar);

                // 2. 第二行: 快捷操作与统计栏 (Top = 34, Height = 24)
                UIElement actionToolbar = new UIElement();
                actionToolbar.Left.Set(0, 0);
                actionToolbar.Top.Set(34, 0);
                actionToolbar.Width.Set(0, 1f);
                actionToolbar.Height.Set(24, 0);

                CompactButton btnOpenAll = new CompactButton("全开", () => AtlasValSet.SetAllStructureMarkers(true),
                    "一键开启所有关键结构与宝箱标记");
                btnOpenAll.Left.Set(0, 0);
                btnOpenAll.Top.Set(0, 0);
                btnOpenAll.Width.Set(48, 0);
                btnOpenAll.Height.Set(22, 0);
                actionToolbar.Append(btnOpenAll);

                CompactButton btnCloseAll = new CompactButton("全关", () => AtlasValSet.SetAllStructureMarkers(false),
                    "一键关闭所有关键结构与宝箱标记");
                btnCloseAll.Left.Set(54, 0);
                btnCloseAll.Top.Set(0, 0);
                btnCloseAll.Width.Set(48, 0);
                btnCloseAll.Height.Set(22, 0);
                actionToolbar.Append(btnCloseAll);

                CompactButton btnRescan = new CompactButton("重扫", () => StructureMarker.TriggerRescan(),
                    "立即遍历世界方块与宝箱重新扫描全部结构位置");
                btnRescan.Left.Set(108, 0);
                btnRescan.Top.Set(0, 0);
                btnRescan.Width.Set(48, 0);
                btnRescan.Height.Set(22, 0);
                actionToolbar.Append(btnRescan);

                _statsText = new HoverText("已索引箱子: 0/0", 0.72f);
                _statsText.Left.Set(168, 0);
                _statsText.Width.Set(-168, 1f);
                _statsText.Height.Set(22, 0);
                _statsText.VAlign = 0.5f;
                actionToolbar.Append(_statsText);

                Child.Append(actionToolbar);

                // 3. 第三行: 搜索框与一键清除按钮 (Top = 64, Height = 26)
                UIElement searchToolbar = new UIElement();
                searchToolbar.Left.Set(0, 0);
                searchToolbar.Top.Set(64, 0);
                searchToolbar.Width.Set(0, 1f);
                searchToolbar.Height.Set(26, 0);

                _searchBox = new UITextBox("搜索: 结构 / NPC / 箱内物品 / 拼音 / ID");
                _searchBox.Left.Set(0, 0);
                _searchBox.Top.Set(0, 0);
                _searchBox.Width.Set(-28, 1f);
                _searchBox.Height.Set(26, 0);
                _searchBox.TextScale = 0.75f;
                _searchBox.Text_MaxLength = 50;
                _searchBox.SetPadding(3);
                _searchBox.OnTextChanged += text => MapAtlasPanel.RequestSearch(text);
                _searchBox.OnRightClick += (evt, el) => MapAtlasPanel.ClearSearch();
                searchToolbar.Append(_searchBox);

                UIClearButton btnSearchClear = new UIClearButton(() =>
                {
                    if (_searchBox != null) _searchBox.Text = "";
                    MapAtlasPanel.ClearSearch();
                });
                btnSearchClear.Left.Set(-24, 1f);
                btnSearchClear.Top.Set(0, 0);
                btnSearchClear.Width.Set(24, 0);
                btnSearchClear.Height.Set(26, 0);
                btnSearchClear.VAlign = 0.5f;
                searchToolbar.Append(btnSearchClear);

                Child.Append(searchToolbar);

                // 4. 第四行: 专用的搜索结果列表 (Top = 98, Height = 100% - 104)
                _resultsView = new UIScrollViewer2();
                _resultsView.Left.Set(0, 0);
                _resultsView.Top.Set(98, 0);
                _resultsView.Width.Set(0, 1f);
                _resultsView.Height.Set(-104, 1f);
                _resultsView.ItemMargin = 2;
                Child.Append(_resultsView);

                AtlasValSet.OnCategoryStateChanged += RefreshCategoryButtons;

                RefreshStats();
                RefreshCategoryButtons();
                SetResults("", new List<StructurePin>(), new List<AtlasChestHit>());
                Recalculate();
            }

            public override void Open(UIElement windowParent)
            {
                base.Open(windowParent);
                Left.Set(14, 0);
                Top.Set(56, 0);
                Width.Set(470, 0);
                Height.Set(450, 0);
                Child.Top.Pixels = 42;
                Child.Height.Set(-52, 1f);
                Recalculate();
            }

            private void AddCategoryBtn(UIElement parent, string name, Func<bool> getState, Action onToggle, string tooltip, int index)
            {
                CategoryToggleButton btn = new CategoryToggleButton(name, getState, onToggle, tooltip);
                btn.Top.Set(0, 0);
                btn.Left.Set(index * 112f, 0);
                btn.Width.Set(106f, 0);
                btn.Height.Set(24, 0);
                parent.Append(btn);
                _categoryButtons.Add(btn);
            }

            public void RefreshCategoryButtons()
            {
                foreach (CategoryToggleButton btn in _categoryButtons)
                {
                    btn?.UpdateAppearance();
                }
            }

            public void SetSearchText(string text)
            {
                if (_searchBox != null) _searchBox.Text = text;
            }

            public void RefreshStats()
            {
                (int indexed, int total) = ChestItemIndex.GetStats();
                _statsText.SetText($"已索引箱子: {indexed}/{total}");
                _statsText.HoverTooltip = (indexed < total)
                    ? $"已索引 {indexed}/{total} 个箱子\n多人客户端仅在打开过箱子后才会同步箱内物品数据"
                    : $"已索引全部 {total} 个箱子";
            }

            public void SetResults(string query, List<StructurePin> structHits, List<AtlasChestHit> chestHits)
            {
                RefreshStats();
                _resultsView.ClearChild();

                if (string.IsNullOrWhiteSpace(query))
                {
                    AddGroupTitle("输入关键词搜索世界结构、NPC 或箱内物品");
                    AddGroupTitle("支持中文 / 全拼 / 首字母 / 物品ID，双击条目定位");
                    _resultsView.Recalculate();
                    return;
                }

                int totalCount = structHits.Count + chestHits.Count;
                AddGroupTitle($"搜索结果: 共 {totalCount} 处 (双击条目定位)");

                // 结构与遗迹
                if (structHits.Count > 0)
                {
                    AddGroupTitle($"结构 / 遗迹 ({structHits.Count})");
                    int structShown = Math.Min(structHits.Count, MaxStructureEntries);
                    for (int i = 0; i < structShown; i++)
                    {
                        StructurePin pin = structHits[i];
                        ResultEntry entry = new ResultEntry(
                            MapAtlasPanel.LoadItemIconSafe(pin.ItemId),
                            pin.Name,
                            StructureMarker.BuildSearchTooltip(pin),
                            () => MapAtlasPanel.FlyMapFullscreenTo(pin.PositionInTiles));
                        _resultsView.AddChild(entry);
                    }
                    if (structHits.Count > MaxStructureEntries)
                    {
                        AddGroupTitle($"…其余 {structHits.Count - MaxStructureEntries} 处结构未列出(地图仍高亮)");
                    }
                }

                // 宝箱
                if (chestHits.Count > 0)
                {
                    AddGroupTitle($"宝箱 ({chestHits.Count})");
                    int chestShown = Math.Min(chestHits.Count, MaxChestEntries);
                    for (int i = 0; i < chestShown; i++)
                    {
                        AtlasChestHit hit = chestHits[i];
                        ChestDisplayInfo info = StructureMarker.BuildChestSearchInfo(hit.ChestIndex, hit.MatchText);
                        ResultEntry entry = new ResultEntry(
                            MapAtlasPanel.LoadItemIconSafe(info.Icon),
                            info.Title,
                            info.Tooltip,
                            () => MapAtlasPanel.FlyMapFullscreenTo(new Vector2(hit.X + 0.5f, hit.Y + 0.5f)));
                        _resultsView.AddChild(entry);
                    }
                    if (chestHits.Count > MaxChestEntries)
                    {
                        AddGroupTitle($"…其余 {chestHits.Count - MaxChestEntries} 个宝箱未列出(地图仍高亮)");
                    }
                }

                if (totalCount == 0)
                {
                    AddGroupTitle("未找到匹配的结构或宝箱物品");
                }

                _resultsView.Recalculate();
            }

            private void AddGroupTitle(string text)
            {
                UIText title = new UIText(text, 0.78f)
                {
                    TextColor = new Color(255, 228, 94),
                    Height = { Pixels = 22 },
                    MarginTop = 4,
                };
                title.Left.Set(4, 0);
                _resultsView.AddChild(title);
            }
        }
    }
}

