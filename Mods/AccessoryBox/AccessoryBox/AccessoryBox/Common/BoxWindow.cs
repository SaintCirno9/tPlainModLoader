using AccessoryBox.Common.UI;
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

namespace AccessoryBox.Common
{
    /// <summary>
    /// 饰品箱窗口：滚动 + 自动换行自适应布局，容量变化自动重建，顶部工具栏，全域滚轮平滑支持
    /// 作者: SaintCirno9
    /// </summary>
    internal class BoxWindow : UIWindow
    {
        private UIBoxWrapPanel wp = null;
        private UIList uiList = null;
        private UIScrollbar scrollbar = null;

        public BoxWindow() : base("饰品箱", 460, 360)
        {
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

            wp = new UIBoxWrapPanel();
            wp.Width.Set(0, 1);
            wp.ItemMargin = 4;
            uiList.Add(wp);

            AccessoryBox.OnCapacityChanged += Rebuild;
            OnClose += AccessoryBoxStorage.SaveNow;
            OnOpen += () =>
            {
                if (!Main.playerInventory)
                {
                    Main.playerInventory = true;
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
            };

            Rebuild();
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
            UIBoxButton btnDeposit = new UIBoxButton(height, () => "一键存入背包非收藏、非快捷栏物品", "Images/UI/Cursor_4");
            btnDeposit.OnClick += () => AccessoryBox.DepositAllFromPlayer(Main.LocalPlayer);
            sp.Append(btnDeposit);

            // 2. 快速堆叠
            UIBoxButton btnQuickStack = new UIBoxButton(height, () => "一键向饰品箱快速堆叠已有物品", "Images/UI/Cursor_8");
            btnQuickStack.OnClick += () => AccessoryBox.QuickStackFromPlayer(Main.LocalPlayer);
            sp.Append(btnQuickStack);

            // 3. 全部取出
            UIBoxButton btnLoot = new UIBoxButton(height, () => "一键取出饰品箱物品到个人背包", "Images/UI/Cursor_6");
            btnLoot.OnClick += () => AccessoryBox.LootAllToPlayer(Main.LocalPlayer);
            sp.Append(btnLoot);

            // 4. 整理饰品箱
            UIBoxButton btnSort = new UIBoxButton(height, () => "整理并排序饰品箱（饰品与防具优先）", "Images/UI/Cursor_9");
            btnSort.OnClick += () => AccessoryBox.SortAccessoryBox();
            sp.Append(btnSort);

            // 5. 饰品属性挂载开关
            UIBoxButton btnPassive = new UIBoxButton(height, () => $"箱内饰品属性生效: {(AccessoryBox.EnablePassive ? "[开启]" : "[关闭]")}", "Images/Item_1862");
            btnPassive.OnClick += () =>
            {
                AccessoryBox.EnablePassive = !AccessoryBox.EnablePassive;
            };
            btnPassive.OnUpdate += _ =>
            {
                btnPassive.Color = AccessoryBox.EnablePassive
                    ? (btnPassive.IsMouseHovering ? Color.White : Color.White * 0.85f)
                    : (btnPassive.IsMouseHovering ? Color.Gray : Color.Gray * 0.5f);
            };
            sp.Append(btnPassive);

            return container;
        }

        private void Rebuild()
        {
            wp.RemoveAllChildren();

            Item[] slots = AccessoryBox.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                wp.Append(new BoxItem(slots, i));
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

            // 鼠标悬停在窗口范围内时，驱动背包滚动条并防止滚轮误切快捷栏
            if (ContainsPoint(Main.MouseScreen))
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
    /// 自动换行面板：自适应计算总高度，确保滚动条视图范围精确
    /// </summary>
    internal class UIBoxWrapPanel : UIWrapPanel
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

    /// <summary>
    /// 顶部工具栏图标按钮
    /// </summary>
    internal class UIBoxButton : UIImage
    {
        public Action OnClick = null;
        public Func<string> GetMouseText = null;

        public UIBoxButton(float size, Func<string> getMouseText, string image) :
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
