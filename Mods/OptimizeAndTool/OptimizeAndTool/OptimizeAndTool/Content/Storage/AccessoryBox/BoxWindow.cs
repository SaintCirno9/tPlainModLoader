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

namespace OptimizeAndTool.Content.Storage.AccessoryBox
{
    /// <summary>
    /// 饰品箱窗口：滚动 + 自动换行自适应布局，容量变化自动重建，顶部工具栏，全域滚轮平滑支持
    /// 作者: SaintCirno9
    /// </summary>
    public class BoxWindow : UIWindow
    {
        private static BoxWindow instance = null;

        public static BoxWindow Instance
        {
            get
            {
                if (instance == null) instance = new BoxWindow();
                return instance;
            }
        }

        public static new bool IsOpen => instance != null && instance.Parent != null;

        public static bool IsOpenAndHovering => IsOpen && instance.IsMouseHovering;

        public static void Toggle()
        {
            if (IsOpen)
            {
                Instance.Close();
            }
            else
            {
                if (ModifyInterfaceLayers.ui_state != null)
                {
                    Instance.Open(ModifyInterfaceLayers.ui_state);
                }
            }
        }

        private UIBoxWrapPanel wp = null;
        private UIList uiList = null;
        private UIScrollbar scrollbar = null;

        public BoxWindow() : base("饰品箱", 460, 360)
        {
            instance = this;
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

            // 4. 整理排序
            UIBoxButton btnSort = new UIBoxButton(height, () => "按饰品类型与稀有度整理排序", "Images/UI/Cursor_3");
            btnSort.OnClick += () => AccessoryBox.SortAccessoryBox();
            sp.Append(btnSort);

            // 5. 属性生效开关
            UIBoxButton btnPassive = new UIBoxButton(
                height,
                () => AccessoryBox.EnablePassive.val ? "被动饰品属性: 已生效 (点击关闭)" : "被动饰品属性: 已禁用 (点击开启)",
                () => AccessoryBox.EnablePassive.val ? "Images/UI/InfoIcon_0" : "Images/UI/InfoIcon_5"
            );
            btnPassive.OnClick += () =>
            {
                AccessoryBox.EnablePassive.val = !AccessoryBox.EnablePassive.val;
                SoundEngine.PlaySound(SoundID.MenuTick);
            };
            sp.Append(btnPassive);

            return container;
        }

        public void Rebuild()
        {
            wp.Elements.Clear();
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

            // 鼠标悬停在饰品箱窗口范围内（含 16px 外沿与滑条容差）时，拦截快捷栏与制造列表滚轮，并平滑驱动滚动条
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
}
