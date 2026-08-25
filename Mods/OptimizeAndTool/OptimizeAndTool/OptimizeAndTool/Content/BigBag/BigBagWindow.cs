using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// <summary>
    /// 巨大背包窗口：滚动 + 自动换行格子布局，容量变化自动重建，顶部操作工具栏，支持全域平滑滚轮
    /// 作者: SaintCirno9
    /// </summary>
    internal class BigBagWindow : UIWindow
    {
        private UIBigBagWrapPanel wp = null;
        private UIList uiList = null;
        private UIScrollbar scrollbar = null;

        private float lastLeft = -1;
        private float lastTop = -1;
        private float lastWidth = -1;
        private float lastHeight = -1;
        private bool wasDraggingOrResizing = false;

        public BigBagWindow() : base("巨大背包", 460, 360)
        {
            // 恢复持久化保存的窗口大小与位置
            if (SettingUI_player.BigBagWidth.HasValue && SettingUI_player.BigBagHeight.HasValue)
            {
                Width.Pixels = Math.Max(MinWidth.Pixels, SettingUI_player.BigBagWidth.Value);
                Height.Pixels = Math.Max(MinHeight.Pixels, SettingUI_player.BigBagHeight.Value);
            }

            if (SettingUI_player.BigBagPosX.HasValue && SettingUI_player.BigBagPosY.HasValue)
            {
                Left.Pixels = SettingUI_player.BigBagPosX.Value;
                Top.Pixels = SettingUI_player.BigBagPosY.Value;
            }

            lastLeft = Left.Pixels;
            lastTop = Top.Pixels;
            lastWidth = Width.Pixels;
            lastHeight = Height.Pixels;

            UIElement btns = BuildBtns();
            Child.Append(btns);

            UIPanel panel = new UIPanel();
            panel.Width.Set(0, 1);
            panel.Height.Set(-btns.Height.Pixels - 4, 1);
            panel.VAlign = 1;
            panel.SetPadding(6);
            panel.BorderColor = panel.BackgroundColor;
            Child.Append(panel);

            scrollbar = new UIScrollbar();
            scrollbar.Height.Set(-12, 1);
            scrollbar.HAlign = 1;
            scrollbar.VAlign = 0.5f;

            uiList = new UIList();
            uiList.Width.Set(-25, 1);
            uiList.Height.Precent = 1;
            uiList.SetScrollbar(scrollbar);

            panel.Append(uiList);
            panel.Append(scrollbar);

            wp = new UIBigBagWrapPanel();
            wp.Width.Set(0, 1);
            wp.ItemMargin = 4;
            uiList.Add(wp);

            BigBag.OnCapacityChanged += Rebuild;
            OnClose += () =>
            {
                SaveWindowLayout();
                BigBagStorage.SaveNow();
            };
            OnOpen += () =>
            {
                if (Main.LocalPlayer != null && BigBagStorage.ActivePlayerName != Main.LocalPlayer.name)
                {
                    BigBagStorage.LoadForPlayer(Main.LocalPlayer);
                }

                if (!Main.playerInventory)
                {
                    Main.playerInventory = true;
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
            };

            Rebuild();
        }

        private void SaveWindowLayout()
        {
            if (Math.Abs(Left.Pixels - lastLeft) > 0.5f ||
                Math.Abs(Top.Pixels - lastTop) > 0.5f ||
                Math.Abs(Width.Pixels - lastWidth) > 0.5f ||
                Math.Abs(Height.Pixels - lastHeight) > 0.5f)
            {
                lastLeft = Left.Pixels;
                lastTop = Top.Pixels;
                lastWidth = Width.Pixels;
                lastHeight = Height.Pixels;

                SettingUI_player.BigBagPosX = Left.Pixels;
                SettingUI_player.BigBagPosY = Top.Pixels;
                SettingUI_player.BigBagWidth = Width.Pixels;
                SettingUI_player.BigBagHeight = Height.Pixels;
                SettingUI_player.SaveSetting();
            }
        }

        private UIElement BuildBtns()
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
            sp.ItemMargin = 8;
            container.Append(sp);

            // 1. 全部存入
            UIBigBagButton btnDeposit = new UIBigBagButton(height, () => "一键存入背包非收藏、非快捷栏物品", "Images/UI/Cursor_4");
            btnDeposit.OnClick += () => BigBag.DepositAllFromPlayer(Main.LocalPlayer);
            sp.Append(btnDeposit);

            // 2. 快速堆叠
            UIBigBagButton btnQuickStack = new UIBigBagButton(height, () => "一键向大背包快速堆叠已有物品", "Images/UI/Cursor_8");
            btnQuickStack.OnClick += () => BigBag.QuickStackFromPlayer(Main.LocalPlayer);
            sp.Append(btnQuickStack);

            // 3. 全部取出
            UIBigBagButton btnLoot = new UIBigBagButton(height, () => "一键取出大背包物品到个人背包", "Images/UI/Cursor_6");
            btnLoot.OnClick += () => BigBag.LootAllToPlayer(Main.LocalPlayer);
            sp.Append(btnLoot);

            // 4. 整理大背包
            UIBigBagButton btnSort = new UIBigBagButton(height, () => "整理并排序大背包物品", "Images/UI/Cursor_9");
            btnSort.OnClick += () => BigBag.SortBigBag();
            sp.Append(btnSort);

            // 5. 拾取自动堆叠开关
            UIBigBagButton btnAutoStack = new UIBigBagButton(height, () => $"拾取时自动堆入大背包: {(BigBag.AutoStackOnPickup.val ? "[开启]" : "[关闭]")}", "Images/Item_5010");
            btnAutoStack.OnClick += () =>
            {
                BigBag.AutoStackOnPickup.val = !BigBag.AutoStackOnPickup.val;
            };
            btnAutoStack.OnUpdate += _ =>
            {
                btnAutoStack.Color = BigBag.AutoStackOnPickup.val ? (btnAutoStack.IsMouseHovering ? Color.White : Color.White * 0.85f) : (btnAutoStack.IsMouseHovering ? Color.Gray : Color.Gray * 0.5f);
            };
            sp.Append(btnAutoStack);

            return container;
        }

        private void Rebuild()
        {
            wp.RemoveAllChildren();

            Item[] slots = BigBag.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                wp.Append(new BigBagItem(slots, i));
            }
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

            wp.UpdateContainer_Height();

            // 监听拖动或调整大小结束，即时落盘保存最新布局
            bool isMouseLeft = Main.mouseLeft;
            if (wasDraggingOrResizing && !isMouseLeft)
            {
                SaveWindowLayout();
            }
            wasDraggingOrResizing = isMouseLeft && ContainsPoint(Main.MouseScreen);

            // 鼠标悬停在活动窗口范围内（含 16px 外沿与滑条容差）时，拦截快捷栏与制造列表滚轮并平滑驱动滚动条
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

    /// <summary>
    /// 自动换行面板：在排版时自适应设置自身总高度，确保滚动条视图范围准确
    /// </summary>
    internal class UIBigBagWrapPanel : UIWrapPanel
    {
        public override void Recalculate()
        {
            float x = 0;
            float y = 0;
            float i_HeightMax = 0;
            float innerWidth = GetInnerDimensions().Width;

            foreach (UIElement i in Children)
            {
                float elemWidth = i.GetOuterDimensions().Width;
                float elemHeight = i.GetOuterDimensions().Height;

                if (x + elemWidth > innerWidth && x > 0)
                {
                    x = 0;
                    y += i_HeightMax + ItemMargin;
                    i_HeightMax = 0;
                }

                i.Left.Set(x, 0);
                i.Top.Set(y, 0);

                i_HeightMax = Math.Max(i_HeightMax, elemHeight);
                x += ItemMargin + elemWidth;
            }

            float totalHeight = y + i_HeightMax;
            Height.Set(totalHeight, 0);

            base.Recalculate();
        }
    }

    internal class UIBigBagButton : UIImage
    {
        public Action OnClick = null;
        public Func<string> GetMouseText = null;

        public UIBigBagButton(float size, Func<string> getMouseText, string image) :
            base(Main.Assets.Request<Texture2D>(image, AssetRequestMode.ImmediateLoad))
        {
            Width.Pixels = Height.Pixels = size;
            ScaleToFit = true;
            GetMouseText = getMouseText;

            OnMouseOver += (e, s) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Color = Color.White;
            };

            Color = Color.White * 0.7f;
            OnMouseOut += (e, s) => Color = Color.White * 0.7f;

            OnLeftClick += (e, s) =>
            {
                OnClick?.Invoke();
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsMouseHovering && GetMouseText != null)
            {
                string text = GetMouseText();
                if (!string.IsNullOrEmpty(text)) Main.instance.MouseText(text);
            }
        }
    }
}
