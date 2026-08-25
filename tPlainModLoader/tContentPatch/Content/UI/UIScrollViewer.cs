using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace tContentPatch.Content.UI
{
    /// <summary>
    /// <see cref="UIList"/>和<see cref="UIScrollbar"/>的结合
    /// </summary>
    public class UIScrollViewer : UIElement
    {
        private UIList ui_list = null;
        private UIScrollbar ui_sb = null;

        public UIScrollbar Scrollbar => ui_sb;
        public UIList List => ui_list;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="scrollbarIsLeft">滚动条是否在左边</param>
        public UIScrollViewer(bool scrollbarIsLeft = false)
        {
            ui_sb = new UIScrollbar();
            ui_sb.Height.Set(-12, 1);
            ui_sb.HAlign = scrollbarIsLeft ? 0 : 1;
            ui_sb.VAlign = 0.5f;

            ui_list = new UIList();
            ui_list.Width.Set(-25, 1);
            ui_list.Height.Precent = 1;
            ui_list.HAlign = scrollbarIsLeft ? 1 : 0;
            ui_list.SetScrollbar(ui_sb);

            Append(ui_list);
            Append(ui_sb);
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            if (ui_sb != null && evt.ScrollWheelValue != 0)
            {
                ui_sb.ViewPosition -= evt.ScrollWheelValue;
            }
        }

        /// <summary/>
        public void SetChild(UIElement uie)
        {
            ui_list.Clear();
            if (uie == null) return;
            ui_list.Add(uie);
        }
    }
}
