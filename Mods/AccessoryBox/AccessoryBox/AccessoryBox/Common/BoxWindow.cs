using AccessoryBox.Common.UI;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria.UI;

namespace AccessoryBox.Common
{
    internal class BoxWindow : UIWindow
    {
        protected BoxConsole console;

        public BoxWindow(BoxConsole console, string title, int width, int height) : base(title, width, height)
        {
            this.console = console;

            UIElement btns = BuildBtns();
            Child.Append(btns);

            UIScrollViewer sv = new UIScrollViewer();
            sv.Width.Set(0, 1);
            sv.Height.Set(-btns.Height.Pixels - 2, 1);
            Child.Append(sv);

            UIWrapPanel wp = new UIWrapPanel();
            wp.Width.Set(0, 1);
            wp.ItemMargin = 2;
            sv.Append(wp);
        }

        private UIElement BuildBtns()
        {
            UIStackPanel btns = new UIStackPanel();
            btns.Width.Set(0, 1);
            btns.Height.Set(20, 0);
            btns.Horizontal = true;
            btns.ItemMargin = 2;

            UISwitchButtonImage enable = new UISwitchButtonImage(20, "启用", "Images/Item_4346", "Images/Item_5391");
            enable.GetVal += () => console.EnableGet();
            enable.OnClick += () => console.EnableSet(!console.EnableGet());
            btns.Append(enable);

            UIButtonImage load = new UIButtonImage(20, "加载", "Images/UI/Cursor_8");
            load.OnClick += console.Load;
            btns.Append(load);

            UIButtonImage save = new UIButtonImage(20, "保存", "Images/UI/Cursor_9");
            save.OnClick += console.Save;
            btns.Append(save);

            UIButtonImage add = new UIButtonImage(20, "添加", "Images/UI/Cursor_4");
            add.OnClick += () => console.AddItem(new Terraria.Item());
            btns.Append(add);

            UIButtonImage clear = new UIButtonImage(20, "清空", "Images/UI/Cursor_6");
            clear.OnClick += console.ClearItem;
            btns.Append(clear);

            return btns;
        }
    }
}
