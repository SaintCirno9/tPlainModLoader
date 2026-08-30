using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.Creative
{
    /// <summary>
    /// 
    /// </summary>
    public class UIItemGrid : UIElement
    {
        private Item _item;
        private int _itemSlotContext;


        public UIItemGrid(Item item, int itemSlotContext)
        {
            _item = item;
            _itemSlotContext = itemSlotContext;
            Width = new StyleDimension(48f, 0f);
            Height = new StyleDimension(48f, 0f);
        }


        private void HandleItemSlotLogic()
        {
            if (base.IsMouseHovering)
            {
                Player player = Main.LocalPlayer;
                if (player != null) player.mouseInterface = true;
                Item[] items = new Item[] { _item };
                ItemSlot.OverrideHover(items, _itemSlotContext);
                ItemSlot.LeftClick(items, _itemSlotContext);
                ItemSlot.RightClick(items, _itemSlotContext);
                ItemSlot.MouseHover(items, _itemSlotContext);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //让没在显示区域的控件不要绘制, 防止一次加载太多资源导致卡顿
            if (IsOverflowHidden(this, Parent)) return;

            HandleItemSlotLogic();
            Vector2 position = GetDimensions().Center() + new Vector2(52f, 52f) * -0.5f * Terraria.Main.inventoryScale;
            ItemSlot.Draw(spriteBatch, ref _item, _itemSlotContext, position);
        }

        private bool IsOverflowHidden(Terraria.UI.UIElement uie, Terraria.UI.UIElement parent)
        {
            if (uie == null || parent == null) return false;

            if (parent.OverflowHidden)
            {
                Rectangle parentRect = parent.GetInnerDimensions().ToRectangle();
                Rectangle elementRect = uie.GetDimensions().ToRectangle();

                if (!parentRect.Intersects(elementRect))
                {
                    return true;
                }
            }

            return parent.Parent != null && IsOverflowHidden(uie, parent.Parent);
        }
    }
}
