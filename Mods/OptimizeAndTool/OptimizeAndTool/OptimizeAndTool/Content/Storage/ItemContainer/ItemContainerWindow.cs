using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.Storage.ItemContainer
{
    /// <summary>
    /// 药水袋 UI 窗口
    /// </summary>
    public class PotionBagWindow : ItemContainerWindow
    {
        private static PotionBagWindow instance;
        public static PotionBagWindow Instance => instance ?? (instance = new PotionBagWindow());
        public static new bool IsOpen => instance != null && instance.Parent != null;

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

        public PotionBagWindow() : base(PotionBagStorage.Instance)
        {
            instance = this;
        }
    }

    /// <summary>
    /// 旗帜盒 UI 窗口
    /// </summary>
    public class BannerChestWindow : ItemContainerWindow
    {
        private static BannerChestWindow instance;
        public static BannerChestWindow Instance => instance ?? (instance = new BannerChestWindow());
        public static new bool IsOpen => instance != null && instance.Parent != null;

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

        public BannerChestWindow() : base(BannerChestStorage.Instance)
        {
            instance = this;
        }
    }

    /// <summary>
    /// 通用大型收纳袋窗口：网格自动换行 + 滚动条平滑支持 + 顶部快捷工具栏
    /// </summary>
    public class ItemContainerWindow : UIWindow
    {
        public ItemContainerStorage Storage { get; }

        private UIItemContainerWrapPanel wp = null;
        private UIList uiList = null;
        private UIScrollbar scrollbar = null;

        public ItemContainerWindow(ItemContainerStorage storage) : base(storage.Title, 460, 360)
        {
            Storage = storage;

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

            wp = new UIItemContainerWrapPanel();
            wp.Width.Set(0, 1);
            wp.ItemMargin = 4;
            uiList.Add(wp);

            storage.OnSlotsChanged += Rebuild;
            OnClose += () => Storage.SaveNow();
            OnOpen += () =>
            {
                Storage.EnsurePlayerLoaded();
                if (!Main.playerInventory)
                {
                    Main.playerInventory = true;
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                }
                Rebuild();
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

            // 1. 一键从随身各处收集
            UIContainerButton btnCollect = new UIContainerButton(height, () => "一键从背包及随身各储物箱收集", "Images/UI/Cursor_4");
            btnCollect.OnClick += () => Storage.CollectFromAllInventories(Main.LocalPlayer);
            sp.Append(btnCollect);

            // 2. 快速堆叠
            UIContainerButton btnQuickStack = new UIContainerButton(height, () => "一键快速堆叠背包中已有物品", "Images/UI/Cursor_8");
            btnQuickStack.OnClick += () => Storage.QuickStackFromPlayer(Main.LocalPlayer);
            sp.Append(btnQuickStack);

            // 3. 一键全部退回
            UIContainerButton btnLoot = new UIContainerButton(height, () => "一键将所有物品取出至背包", "Images/UI/Cursor_6");
            btnLoot.OnClick += () => Storage.WithdrawAll(Main.LocalPlayer);
            sp.Append(btnLoot);

            // 4. 一键整理排序
            UIContainerButton btnSort = new UIContainerButton(height, () => "整理合并并排序", "Images/UI/Cursor_9");
            btnSort.OnClick += () => Storage.AutoSort();
            sp.Append(btnSort);

            // 5. 自动收纳开关
            UIContainerButton btnAutoStorage = new UIContainerButton(
                height,
                () => $"拾取时自动吸入收纳: {(Storage.AutoStorage ? "[开启]" : "[关闭]")}",
                "Images/Item_5010"
            );
            btnAutoStorage.OnClick += () =>
            {
                Storage.AutoStorage = !Storage.AutoStorage;
                Storage.SaveNow();
            };
            btnAutoStorage.OnUpdate += _ =>
            {
                btnAutoStorage.Color = Storage.AutoStorage ? (btnAutoStorage.IsMouseHovering ? Color.White : Color.White * 0.85f) : (btnAutoStorage.IsMouseHovering ? Color.Gray : Color.Gray * 0.5f);
            };
            sp.Append(btnAutoStorage);

            return container;
        }

        public void Rebuild()
        {
            wp.Elements.Clear();
            Item[] slots = Storage.Slots;
            if (slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    wp.Append(new ItemContainerSlot(Storage, i));
                }
            }
            uiList?.Recalculate();
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

    public class UIItemContainerWrapPanel : UIElement
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

    public class UIContainerButton : UIImage
    {
        public Action OnClick = null;
        public Func<string> GetMouseText = null;

        public UIContainerButton(float size, Func<string> getMouseText, string image) :
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
