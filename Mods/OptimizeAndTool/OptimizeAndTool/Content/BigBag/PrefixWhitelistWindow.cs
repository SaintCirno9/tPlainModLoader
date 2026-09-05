using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TPML.UI;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using UITextBox = TPML.UI.UITextBox;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 词条保护白名单配置窗口：
    /// 采用经典垂直复选框列表，支持词条名称搜索、极品分类标识、一键恢复五大默认极品前缀与全选/清空。
    /// 作者: SaintCirno9
    /// </summary>
    public class PrefixWhitelistWindow : UIWindow
    {
        private static PrefixWhitelistWindow _instance = null;
        public static PrefixWhitelistWindow Instance => _instance ?? (_instance = new PrefixWhitelistWindow());

        private UIElement topControls = null;
        private UITextBox searchBox = null;
        private UIClearButton btnClearSearch = null;
        private UIButton btnResetDefault = null;
        private UIButton btnSelectAll = null;
        private UIButton btnClearAll = null;
        private UIText summaryText = null;
        private UIText keepCopiesText = null;

        private UIPanel listContainer = null;
        private UIList uiList = null;
        private UIScrollbar scrollbar = null;

        private string searchQuery = string.Empty;
        private string searchPendingQuery = string.Empty;
        private int searchDebounceTimer = 0;
        private List<PrefixEntry> allEntries = new List<PrefixEntry>();

        public PrefixWhitelistWindow() : base("词条保护白名单 (豁免售卖)", 400, 480)
        {
            _instance = this;

            MinWidth.Pixels = 340;
            MinHeight.Pixels = 360;

            // 移除右下角缩放手柄，保持紧凑质感
            foreach (UIElement el in Elements)
            {
                if (el is UIImage img && el != Child)
                {
                    RemoveChild(el);
                    break;
                }
            }

            topControls = new UIElement();
            topControls.Width.Set(0, 1);
            topControls.Height.Set(86, 0);
            topControls.Top.Set(6, 0);
            Child.Append(topControls);

            // 第一行：搜索框 + 清空按钮
            UIElement row1 = new UIElement();
            row1.Width.Set(-26, 1); // 避让关闭按钮
            row1.Height.Set(24, 0);
            row1.Top.Set(0, 0);
            topControls.Append(row1);

            searchBox = new UITextBox("搜索词条名称 / ID / 分类...");
            searchBox.Width.Set(-28, 1);
            searchBox.Height.Set(24, 0);
            searchBox.TextScale = 0.75f;
            searchBox.Text_MaxLength = 40;
            searchBox.SetPadding(3);
            searchBox.OnTextChanged += text =>
            {
                searchPendingQuery = text;
                searchDebounceTimer = 2;
            };
            row1.Append(searchBox);

            btnClearSearch = new UIClearButton(22, () => "清空搜索内容");
            btnClearSearch.Height.Set(24, 0);
            btnClearSearch.Left.Set(-22, 1);
            btnClearSearch.VAlign = 0.5f;
            btnClearSearch.OnClick += ClearSearch;
            row1.Append(btnClearSearch);

            // 第二行：操作按钮栏
            UIElement row2 = new UIElement();
            row2.Width.Set(0, 1);
            row2.Height.Set(26, 0);
            row2.Top.Set(28, 0);
            topControls.Append(row2);

            UIStackPanel btnStack = new UIStackPanel();
            btnStack.Width.Set(0, 1);
            btnStack.Height.Set(26, 0);
            btnStack.VAlign = 0.5f;
            btnStack.Horizontal = true;
            btnStack.ItemMargin = 6;
            row2.Append(btnStack);

            btnResetDefault = new UIButton("恢复默认极品", 0.7f);
            btnResetDefault.Height.Set(24, 0);
            btnResetDefault.SetPadding(4);
            btnResetDefault.OnLeftClick += (evt, el) =>
            {
                PrefixWhitelistManager.ResetToDefault();
                SoundEngine.PlaySound(SoundID.MenuTick);
                RebuildList();
            };
            btnStack.Append(btnResetDefault);

            btnSelectAll = new UIButton("全选", 0.7f);
            btnSelectAll.Height.Set(24, 0);
            btnSelectAll.SetPadding(4);
            btnSelectAll.OnLeftClick += (evt, el) =>
            {
                PrefixWhitelistManager.SelectAll(allEntries.Select(e => e.Id));
                SoundEngine.PlaySound(SoundID.MenuTick);
                RebuildList();
            };
            btnStack.Append(btnSelectAll);

            btnClearAll = new UIButton("清空", 0.7f);
            btnClearAll.Height.Set(24, 0);
            btnClearAll.SetPadding(4);
            btnClearAll.OnLeftClick += (evt, el) =>
            {
                PrefixWhitelistManager.ClearAll();
                SoundEngine.PlaySound(SoundID.MenuTick);
                RebuildList();
            };
            btnStack.Append(btnClearAll);

            summaryText = new UIText("已保护: 0/0", 0.75f);
            summaryText.VAlign = 0.5f;
            summaryText.TextColor = Color.Gold;
            btnStack.Append(summaryText);

            // 第三行：安全保留数量微调栏
            UIElement row3 = new UIElement();
            row3.Width.Set(0, 1);
            row3.Height.Set(24, 0);
            row3.Top.Set(58, 0);
            topControls.Append(row3);

            UIStackPanel keepStack = new UIStackPanel();
            keepStack.Width.Set(0, 1);
            keepStack.Height.Set(24, 0);
            keepStack.VAlign = 0.5f;
            keepStack.Horizontal = true;
            keepStack.ItemMargin = 8;
            row3.Append(keepStack);

            UIText keepLabel = new UIText("售卖同类安全保留:", 0.75f);
            keepLabel.VAlign = 0.5f;
            keepLabel.TextColor = new Color(220, 220, 220);
            keepStack.Append(keepLabel);

            UIMiniStepButton btnMinus = new UIMiniStepButton("-", () =>
            {
                int cur = BigBag.CurrentKeepCopiesThreshold;
                if (cur > 1)
                {
                    BigBag.CurrentKeepCopiesThreshold = cur - 1;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    UpdateKeepCopiesDisplay();
                }
            });
            btnMinus.VAlign = 0.5f;
            keepStack.Append(btnMinus);

            keepCopiesText = new UIText($"{BigBag.CurrentKeepCopiesThreshold} 件", 0.8f);
            keepCopiesText.VAlign = 0.5f;
            keepCopiesText.TextColor = Color.Gold;
            keepStack.Append(keepCopiesText);

            UIMiniStepButton btnPlus = new UIMiniStepButton("+", () =>
            {
                int cur = BigBag.CurrentKeepCopiesThreshold;
                if (cur < 10)
                {
                    BigBag.CurrentKeepCopiesThreshold = cur + 1;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    UpdateKeepCopiesDisplay();
                }
            });
            btnPlus.VAlign = 0.5f;
            keepStack.Append(btnPlus);

            // 垂直复选框列表容器
            listContainer = new UIPanel();
            listContainer.Top.Set(96, 0);
            listContainer.Width.Set(0, 1);
            listContainer.Height.Set(-100, 1);
            listContainer.SetPadding(4);
            listContainer.BackgroundColor = new Color(20, 24, 48) * 0.9f;
            listContainer.BorderColor = new Color(43, 60, 120);
            Child.Append(listContainer);

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(0, 1);
            scrollbar.HAlign = 1f;

            uiList = new UIList();
            uiList.Width.Set(-22, 1);
            uiList.Height.Set(0, 1);
            uiList.SetScrollbar(scrollbar);
            listContainer.Append(uiList);
            listContainer.Append(scrollbar);

            OnOpen += () =>
            {
                allEntries = PrefixWhitelistManager.GetAllPrefixEntries();
                PrefixWhitelistManager.OnWhitelistChanged -= RebuildList;
                PrefixWhitelistManager.OnWhitelistChanged += RebuildList;
                UpdateKeepCopiesDisplay();
                RebuildList();
            };

            OnClose += () =>
            {
                PrefixWhitelistManager.OnWhitelistChanged -= RebuildList;
            };
        }

        public void OpenOrClose(UIState parentState)
        {
            if (IsOpen)
            {
                Close();
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
            else
            {
                Open(parentState);
            }
        }

        public void ClearSearch()
        {
            if (searchBox != null) searchBox.Text = string.Empty;
            searchQuery = string.Empty;
            searchPendingQuery = string.Empty;
            searchDebounceTimer = 0;
            SoundEngine.PlaySound(SoundID.MenuTick);
            RebuildList();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (searchDebounceTimer > 0)
            {
                searchDebounceTimer--;
                if (searchDebounceTimer == 0 && searchQuery != searchPendingQuery)
                {
                    searchQuery = searchPendingQuery.Trim();
                    RebuildList();
                }
            }
        }

        public void RebuildList()
        {
            uiList?.Clear();
            if (allEntries == null || allEntries.Count == 0)
            {
                allEntries = PrefixWhitelistManager.GetAllPrefixEntries();
            }

            int activeCount = 0;
            int totalCount = allEntries.Count;

            string query = (searchQuery ?? "").ToLowerInvariant();

            foreach (var entry in allEntries)
            {
                bool isWhitelisted = PrefixWhitelistManager.IsWhitelisted(entry.Id);
                if (isWhitelisted) activeCount++;

                if (!string.IsNullOrWhiteSpace(query))
                {
                    bool matchName = entry.Name.ToLowerInvariant().Contains(query);
                    bool matchCat = entry.Category.ToLowerInvariant().Contains(query);
                    bool matchId = entry.Id.ToString() == query;
                    if (!matchName && !matchCat && !matchId) continue;
                }

                PrefixCheckboxItem item = new PrefixCheckboxItem(entry, isWhitelisted);
                uiList.Add(item);
            }

            if (summaryText != null)
            {
                summaryText.SetText($"已保护: {activeCount}/{totalCount}");
            }
        }

        public void UpdateKeepCopiesDisplay()
        {
            if (keepCopiesText != null)
            {
                keepCopiesText.SetText($"{BigBag.CurrentKeepCopiesThreshold} 件");
            }
        }
    }

    /// <summary>
    /// 词条复选框列表行项控件
    /// </summary>
    public class PrefixCheckboxItem : UIPanel
    {
        private readonly PrefixEntry entry;
        private readonly bool isChecked;

        public PrefixCheckboxItem(PrefixEntry entry, bool isChecked)
        {
            this.entry = entry;
            this.isChecked = isChecked;

            Height.Set(28, 0);
            Width.Set(0, 1);
            SetPadding(4);

            BackgroundColor = isChecked ? new Color(36, 52, 100) : new Color(25, 30, 60);
            BorderColor = isChecked ? (entry.IsTopTierDefault ? Color.Gold : new Color(80, 140, 240)) : new Color(40, 50, 90);

            // 1. 复选框方块指示器
            UIText checkText = new UIText(isChecked ? "[√]" : "[  ]", 0.8f);
            checkText.VAlign = 0.5f;
            checkText.Left.Set(4, 0);
            checkText.TextColor = isChecked ? (entry.IsTopTierDefault ? Color.Gold : Color.LimeGreen) : Color.Gray;
            Append(checkText);

            // 2. 词条名称文本（极品前缀以亮金色/青色突出）
            UIText nameText = new UIText(entry.Name, 0.85f);
            nameText.VAlign = 0.5f;
            nameText.Left.Set(36, 0);
            if (entry.IsTopTierDefault)
            {
                nameText.TextColor = Color.Gold;
            }
            else if (isChecked)
            {
                nameText.TextColor = Color.White;
            }
            else
            {
                nameText.TextColor = Color.LightGray * 0.8f;
            }
            Append(nameText);

            // 3. 词条 ID 与类别标签（靠右对齐）
            string tagText = $"{entry.Category} (ID:{entry.Id})";
            if (entry.IsTopTierDefault) tagText = "★极品默认 " + tagText;

            UIText catText = new UIText(tagText, 0.72f);
            catText.VAlign = 0.5f;
            catText.HAlign = 1f;
            catText.TextColor = entry.IsTopTierDefault ? Color.Goldenrod : Color.LightSteelBlue * 0.8f;
            Append(catText);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
            PrefixWhitelistManager.TogglePrefix(entry.Id);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (IsMouseHovering)
            {
                BackgroundColor = isChecked ? new Color(48, 70, 130) : new Color(38, 46, 85);
                BorderColor = isChecked ? Color.White : new Color(70, 90, 150);
            }
            else
            {
                BackgroundColor = isChecked ? new Color(36, 52, 100) : new Color(25, 30, 60);
                BorderColor = isChecked ? (entry.IsTopTierDefault ? Color.Gold : new Color(80, 140, 240)) : new Color(40, 50, 90);
            }

            base.DrawSelf(spriteBatch);
        }
    }

    /// <summary>
    /// 微型步进加减按钮（用于保留数量微调，绘制清晰锐利的加减字符）
    /// </summary>
    public class UIMiniStepButton : UIElement
    {
        private readonly string text;
        private readonly Action onClick;

        public UIMiniStepButton(string text, Action onClick)
        {
            this.text = text;
            this.onClick = onClick;
            Width.Set(24f, 0f);
            Height.Set(22f, 0f);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            onClick?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            CalculatedStyle dim = GetDimensions();
            Rectangle rect = dim.ToRectangle();

            Color bgColor = IsMouseHovering ? new Color(60, 85, 160) : new Color(30, 40, 80);

            Terraria.Utils.DrawInvBG(sb, rect, bgColor);

            Vector2 textSize = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(text) * 0.85f;
            Vector2 textPos = new Vector2(rect.X + (rect.Width - textSize.X) / 2f, rect.Y + (rect.Height - textSize.Y) / 2f);
            Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(sb, Terraria.GameContent.FontAssets.MouseText.Value, text, textPos, Color.White, 0f, Vector2.Zero, new Vector2(0.85f));

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(text == "-" ? "减少保留底数 (下限 1 件)" : "增加保留底数 (上限 10 件)");
            }
        }
    }
}
