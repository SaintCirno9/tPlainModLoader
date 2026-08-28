using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using OptimizeAndTool.Content.Storage.Core;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using TPML.Core.Pinyin;
using UITextBox = tContentPatch.Content.UI.UITextBox;

namespace OptimizeAndTool.Content.QoL.InfiniteBuff
{
    /// <summary>
    /// 无限增益过滤管理主窗口：
    /// 支持增益拼音/中文/ID极速搜索、全开/全关一键切换、收藏置顶管理、隐藏原版无尽图标与自适应网格展示。
    /// 作者: SaintCirno9
    /// </summary>
    public class InfiniteBuffWindow : UIWindow
    {
        private static InfiniteBuffWindow instance = null;
        public static InfiniteBuffWindow Instance => instance ?? (instance = new InfiniteBuffWindow());

        public static bool IsWindowOpen => instance != null && instance.IsOpen;

        private UIElement topControls = null;
        private UITextBox searchBox = null;
        private UIClearButton btnClearSearch = null;
        private UIButton1 btnEnableAll = null;
        private UIButton1 btnDisableAll = null;
        private UIButton1 btnToggleHideBuffs = null;
        private UIText summaryText = null;

        private UIPanel gridContainer = null;
        private UIScrollViewer scrollViewer = null;
        private UIWrapPanel wrapPanel = null;

        private string searchQuery = string.Empty;
        private string searchPendingQuery = string.Empty;
        private int searchDebounceTimer = 0;

        public InfiniteBuffWindow() : base("无限增益管理", 476, 420)
        {
            instance = this;

            // 移除右下角缩放抓手
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
            topControls.Height.Set(58, 0);
            topControls.Top.Set(4, 0);
            Child.Append(topControls);

            // 第一行：搜索框与清空按钮
            UIElement row1 = new UIElement();
            row1.Width.Set(0, 1);
            row1.Height.Set(24, 0);
            row1.Top.Set(0, 0);
            topControls.Append(row1);

            searchBox = new UITextBox("搜索增益名称 / 拼音 / ID");
            searchBox.Width.Set(-30, 1);
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
            btnClearSearch.Left.Set(-24, 1);
            btnClearSearch.VAlign = 0.5f;
            btnClearSearch.OnClick += ClearSearch;
            row1.Append(btnClearSearch);

            // 第二行：操作按钮（全开、全关、隐藏原版图标开关、统计）
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

            btnEnableAll = new UIButton1("全部启用", 0.7f);
            btnEnableAll.Height.Set(24, 0);
            btnEnableAll.SetPadding(4);
            btnEnableAll.OnLeftClick += (evt, el) =>
            {
                InfiniteBuffStorage.ClearBlacklist();
                SoundEngine.PlaySound(SoundID.MenuTick);
                Rebuild();
            };
            btnStack.Append(btnEnableAll);

            btnDisableAll = new UIButton1("全部禁用", 0.7f);
            btnDisableAll.Height.Set(24, 0);
            btnDisableAll.SetPadding(4);
            btnDisableAll.OnLeftClick += (evt, el) =>
            {
                InfiniteBuffStorage.AddAllToBlacklist(InfinitePotionAndBuff.AvailableInfiniteBuffs);
                SoundEngine.PlaySound(SoundID.MenuTick);
                Rebuild();
            };
            btnStack.Append(btnDisableAll);

            btnToggleHideBuffs = new UIButton1(GetHideBuffsButtonText(), 0.7f);
            btnToggleHideBuffs.Height.Set(24, 0);
            btnToggleHideBuffs.SetPadding(4);
            btnToggleHideBuffs.OnLeftClick += (evt, el) =>
            {
                InfinitePotionAndBuff.HideEndlessBuffs.val = !InfinitePotionAndBuff.HideEndlessBuffs.val;
                SettingUI_player.SaveSetting();
                btnToggleHideBuffs.SetText(GetHideBuffsButtonText());
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            btnStack.Append(btnToggleHideBuffs);

            summaryText = new UIText("已启用: 0/0", 0.75f);
            summaryText.VAlign = 0.5f;
            summaryText.TextColor = Color.LightSkyBlue;
            btnStack.Append(summaryText);

            // 第三部分：展示网格与滚动容器
            gridContainer = new UIPanel();
            gridContainer.Top.Set(66, 0);
            gridContainer.Width.Set(0, 1);
            gridContainer.Height.Set(-66, 1);
            gridContainer.SetPadding(6);
            gridContainer.BackgroundColor = new Color(20, 25, 45, 180);
            gridContainer.BorderColor = new Color(40, 60, 110, 200);
            gridContainer.OverflowHidden = true;
            Child.Append(gridContainer);

            scrollViewer = new UIScrollViewer();
            scrollViewer.Width.Precent = 1;
            scrollViewer.Height.Precent = 1;
            gridContainer.Append(scrollViewer);

            wrapPanel = new UIWrapPanel();
            wrapPanel.Width.Precent = 1;
            wrapPanel.ItemMargin = 4f;
            scrollViewer.SetChild(wrapPanel);

            OnOpen += () =>
            {
                if (Main.LocalPlayer != null)
                {
                    InfinitePotionAndBuff.ResetScanCache(); // 绕过扫描节流，确保开窗即用最新数据重建列表
                    InfinitePotionAndBuff.UpdateAvailableBuffs(Main.LocalPlayer);
                }
                // 先取消再订阅，避免反复开关窗口导致事件重复订阅、OnDataChanged 触发多次 Rebuild
                InfiniteBuffStorage.OnDataChanged -= Rebuild;
                InfiniteBuffStorage.OnDataChanged += Rebuild;
                if (btnToggleHideBuffs != null)
                {
                    btnToggleHideBuffs.SetText(GetHideBuffsButtonText());
                }
                Rebuild();
            };

            OnClose += () =>
            {
                InfiniteBuffStorage.OnDataChanged -= Rebuild;
            };
        }

        private string GetHideBuffsButtonText()
        {
            return InfinitePotionAndBuff.HideEndlessBuffs.val ? "隐藏图标: 开" : "隐藏图标: 关";
        }

        public void ClearSearch()
        {
            if (searchBox != null) searchBox.Text = string.Empty;
            searchQuery = string.Empty;
            searchPendingQuery = string.Empty;
            searchDebounceTimer = 0;
            SoundEngine.PlaySound(SoundID.MenuTick);
            Rebuild();
        }

        public void Toggle(UIState parentState = null)
        {
            if (IsOpen)
            {
                Close();
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
            else
            {
                Open(parentState ?? ModifyInterfaceLayers.ui_game_state);
                SoundEngine.PlaySound(SoundID.MenuOpen);
            }
        }

        public void Rebuild()
        {
            wrapPanel.RemoveAllChildren();

            var available = InfinitePotionAndBuff.AvailableInfiniteBuffs;
            if (available == null || available.Count == 0)
            {
                if (summaryText != null)
                {
                    summaryText.SetText("背包内无可用无尽增益");
                    summaryText.TextColor = Color.Gray;
                }
                return;
            }

            int activeCount = 0;
            var buffList = new List<int>(available);

            // 过滤匹配
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                buffList = buffList.Where(MatchesSearch).ToList();
            }

            // 排序：1. 收藏置顶 2. 状态(启用在前) 3. Buff ID
            buffList.Sort((a, b) =>
            {
                bool favA = InfiniteBuffStorage.Favorites.Contains(a);
                bool favB = InfiniteBuffStorage.Favorites.Contains(b);
                if (favA != favB) return favB.CompareTo(favA);

                bool blackA = InfiniteBuffStorage.Blacklist.Contains(a);
                bool blackB = InfiniteBuffStorage.Blacklist.Contains(b);
                if (blackA != blackB) return blackA.CompareTo(blackB);

                return a.CompareTo(b);
            });

            foreach (int buffId in buffList)
            {
                if (!InfiniteBuffStorage.Blacklist.Contains(buffId))
                {
                    activeCount++;
                }
                wrapPanel.Append(new UIBuffIcon(buffId));
            }

            if (summaryText != null)
            {
                int total = available.Count;
                int totalActive = available.Count(b => !InfiniteBuffStorage.Blacklist.Contains(b));
                summaryText.SetText($"已启用: {totalActive}/{total}");
                summaryText.TextColor = totalActive > 0 ? Color.LightSkyBlue : Color.OrangeRed;
            }

            wrapPanel.Width.Precent = 1;
            wrapPanel.Recalculate();
            scrollViewer.List?.Recalculate();
        }

        private bool MatchesSearch(int buffType)
        {
            if (string.IsNullOrWhiteSpace(searchQuery)) return true;
            string q = searchQuery.Trim();

            if (int.TryParse(q, out int queryId) && buffType == queryId) return true;

            string name = Lang.GetBuffName(buffType);
            if (!string.IsNullOrEmpty(name) && PinyinHelper.Matches(name, q)) return true;

            string desc = Lang.GetBuffDescription(buffType);
            if (!string.IsNullOrEmpty(desc) && PinyinHelper.Matches(desc, q)) return true;

            return false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (searchDebounceTimer > 0)
            {
                searchDebounceTimer--;
                if (searchDebounceTimer == 0)
                {
                    searchQuery = searchPendingQuery;
                    Rebuild();
                }
            }

            // 鼠标悬停在窗口内时平滑滚轮与界面拦截
            if (IsOpen && ModifyInterfaceLayers.IsHoveringWindow(this))
            {
                Main.LocalPlayer.mouseInterface = true;

                int delta = Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI;
                if (delta == 0) delta = Terraria.GameInput.PlayerInput.ScrollWheelDelta;

                if (delta != 0 && scrollViewer?.Scrollbar != null)
                {
                    scrollViewer.Scrollbar.ViewPosition -= delta;
                    Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = 0;
                    Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;
                }
            }
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            if (scrollViewer?.Scrollbar != null && evt.ScrollWheelValue != 0)
            {
                scrollViewer.Scrollbar.ViewPosition -= evt.ScrollWheelValue;
                Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = 0;
                Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;
            }
        }
    }
}
