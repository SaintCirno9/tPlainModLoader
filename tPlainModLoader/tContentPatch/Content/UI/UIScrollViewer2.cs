using Microsoft.Xna.Framework;
using Terraria.UI;

namespace tContentPatch.Content.UI
{
    /// <summary>
    /// <see cref="UIScrollViewer"/>和<see cref="UIStackPanel"/>的结合
    /// </summary>
    public class UIScrollViewer2 : UIElement
    {
        /// <summary/>
        public int ItemMargin = 0;
        private UIStackPanel ui_sp = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scrollbarIsLeft">滚动条是否在左边</param>
        public UIScrollViewer2(bool scrollbarIsLeft = false)
        {
            ui_sp = new UIStackPanel();
            ui_sp.Width.Precent = 1;
            ui_sp.IsAutoUpdateSize = true;

            UIScrollViewer ui_sv = new UIScrollViewer(scrollbarIsLeft);
            ui_sv.Width.Precent = 1;
            ui_sv.Height.Precent = 1;
            ui_sv.SetChild(ui_sp);

            Append(ui_sv);
        }

        /// <summary/>
        public void AddChild(UIElement uie)
        {
            if (uie == null) return;

            ui_sp.Append(uie);
        }

        /// <summary/>
        public void ClearChild()
        {
            ui_sp.RemoveAllChildren();
        }

        /// <summary/>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            ui_sp.ItemMargin = ItemMargin;
        }
    }
}
