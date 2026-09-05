using Microsoft.Xna.Framework;
using System;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 通用容器顶部工具栏按钮数据定义
    /// 作者: SaintCirno9
    /// </summary>
    public class BagToolbarButton
    {
        public Func<string> TooltipFunc { get; set; }
        public Func<string> IconPathFunc { get; set; }
        public Action OnClick { get; set; }
        public Action OnRightClick { get; set; }
        public Action OnMiddleClick { get; set; }
        public Func<Color> ColorFunc { get; set; }
        public Func<bool> IsActiveFunc { get; set; }
        public Func<bool> IsVisibleFunc { get; set; }

        public BagToolbarButton(Func<string> tooltipFunc, string iconPath, Action onClick, Func<Color> colorFunc = null, Action onRightClick = null, Action onMiddleClick = null, Func<bool> isActiveFunc = null)
            : this(tooltipFunc, () => iconPath, onClick, colorFunc, onRightClick, onMiddleClick, isActiveFunc)
        {
        }

        public BagToolbarButton(Func<string> tooltipFunc, Func<string> iconPathFunc, Action onClick, Func<Color> colorFunc = null, Action onRightClick = null, Action onMiddleClick = null, Func<bool> isActiveFunc = null)
        {
            TooltipFunc = tooltipFunc;
            IconPathFunc = iconPathFunc;
            OnClick = onClick;
            ColorFunc = colorFunc;
            OnRightClick = onRightClick;
            OnMiddleClick = onMiddleClick;
            IsActiveFunc = isActiveFunc;
        }
    }
}
