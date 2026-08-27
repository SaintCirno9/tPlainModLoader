using System;
using Terraria.UI;

namespace tContentPatch.Content.UI
{
    /// <summary>
    /// 向右堆叠, 超出宽度换行，并自动计算更新自身总高度
    /// </summary>
    public class UIWrapPanel : UIElement
    {
        /// <summary/>
        public float ItemMargin = 0f;

        /// <inheritdoc/>
        public override void Recalculate()
        {
            float availableWidth = GetInnerDimensions().Width;
            if (availableWidth <= 0 && Parent != null)
            {
                availableWidth = Parent.GetInnerDimensions().Width;
                if (availableWidth <= 0) availableWidth = Parent.GetDimensions().Width;
            }
            if (availableWidth <= 0) availableWidth = 400f;

            float x = 0;
            float y = 0;
            float i_HeightMax = 0;

            foreach (UIElement i in Children)
            {
                float iw = i.GetOuterDimensions().Width;
                if (iw <= 0) iw = i.Width.Pixels > 0 ? i.Width.Pixels : 38f;

                float ih = i.GetOuterDimensions().Height;
                if (ih <= 0) ih = i.Height.Pixels > 0 ? i.Height.Pixels : 38f;

                if (x > 0 && (x + iw > availableWidth))
                {
                    x = 0;
                    y += i_HeightMax + ItemMargin;
                    i_HeightMax = 0;
                }

                i.Left.Set(x, 0);
                i.Top.Set(y, 0);

                i_HeightMax = Math.Max(i_HeightMax, ih);
                x += ItemMargin + iw;
            }

            float totalHeight = i_HeightMax > 0 ? (y + i_HeightMax) : 0f;
            Height.Set(totalHeight, 0);

            base.Recalculate();
        }
    }
}

