using AccessoryBox.Common.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.UI;

namespace AccessoryBox.Common
{
    internal class BoxWindow : UIWindow
    {
        protected IBoxConsole console;
        protected UIWrapPanel wp = null;

        public BoxWindow(IBoxConsole console, string title, int width, int height) : base(title, width, height)
        {
            this.console = console;

            UIElement btns = BuildBtns();
            Child.Append(btns);

            UIScrollViewer sv = new UIScrollViewer();
            sv.Width.Set(0, 1);
            sv.Height.Set(-btns.Height.Pixels - 2, 1);
            Child.Append(sv);

            wp = new UIWrapPanel();
            wp.Width.Set(0, 1);
            wp.ItemMargin = 2;
            sv.Append(wp);

            console.OnLoaded += UpdateData;
            console.OnClearItemed += UpdateData;
            console.OnAdded += AddItem;
            console.OnDeled += DelItem;
            console.OnSetItemed += SetItem;
        }

        private UIElement BuildBtns()
        {
            UIStackPanel btns = new UIStackPanel();
            btns.Width.Set(0, 1);
            btns.Height.Set(20, 0);
            btns.Horizontal = true;
            btns.ItemMargin = 2;

            UISwitchButtonImage enable = new UISwitchButtonImage(20, "启用", "Images/Item_4346", "Images/Item_5391");
            enable.GetVal += () => console.GetEnable();
            enable.OnClick += () => console.SetEnable(!console.GetEnable());
            btns.Append(enable);

            UIButtonImage load = new UIButtonImage(20, "加载", "Images/UI/Cursor_8");
            load.OnClick += console.Load;
            btns.Append(load);

            UIButtonImage save = new UIButtonImage(20, "保存", "Images/UI/Cursor_9");
            save.OnClick += console.Save;
            btns.Append(save);

            UIButtonImage add = new UIButtonImage(20, "添加", "Images/UI/Cursor_4");
            add.OnClick += () => console.AddItem(new Item());
            btns.Append(add);

            UIButtonImage clear = new UIButtonImage(20, "清空", "Images/UI/Cursor_6");
            clear.OnClick += console.ClearItem;
            btns.Append(clear);

            return btns;
        }

        protected void UpdateData()
        {
            List<Item> items = console.GetItems();

            wp.RemoveAllChildren();

            items.ForEach(AddItem);
        }

        protected BoxItem ItemToUI(Item item)
        {
            BoxItem ui = new BoxItem(item);
            ui.OnSetItem += console.SetItem;
            ui.OnDel += console.DelItem;

            return ui;
        }

        protected void AddItem(Item item)
        {
            BoxItem ui = ItemToUI(item);

            wp.Append(ui);
        }

        protected void ActionItem(Item item, Action<BoxItem> action)
        {
            BoxItem ui = (BoxItem)wp.Children.FirstOrDefault(i =>
            {
                BoxItem bi = i as BoxItem;
                if (bi == null) return false;

                return bi.item == item;
            });

            action(ui);
        }

        protected void DelItem(Item item)
        {
            ActionItem(item, wp.RemoveChild);
        }

        protected void SetItem(Item item, Item val)
        {
            ActionItem(item, i => i.SetItem(val));
        }
    }
}
