using Microsoft.Xna.Framework.Graphics;
using MapAtlasTool.Content.UI;
using System;
using System.Collections.Generic;
using TPML.UI;
using Terraria;
using Terraria.UI;

namespace MapAtlasTool.Utils.quickBuild
{
    /// <summary>
    /// 创建一些预设的ui
    /// </summary>
    internal static class UIBuild
    {
        /// <summary>
        /// 开关, 绑定值
        /// </summary>
        /// <param name="gsr"></param>
        /// <param name="mouseText"></param>
        /// <param name="ico"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static UIElement get2(GetSetReset<bool> gsr, string mouseText = null, string ico = null, string text = null)
        {
            Texture2D texture = ico == null ? null : Main.Assets.Request<Texture2D>(ico, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            UIItemSwitchBind ui = new UIItemSwitchBind(gsr, texture, text);
            ui.MouseText = mouseText;

            return ui;
        }

        public static UIElement get3(List<UIElement> uis)
        {
            UIScrollViewer2 sv = new UIScrollViewer2();
            sv.Width.Precent = 1;
            sv.Height.Precent = 1;

            foreach (UIElement ui in uis)
            {
                sv.AddChild(ui);
            }

            // 底部安全垫高，防止滑到最底端时被底框遮挡
            UIElement bottomSpacer = new UIElement();
            bottomSpacer.Width.Precent = 1f;
            bottomSpacer.Height.Pixels = 28f;
            sv.AddChild(bottomSpacer);

            return sv;
        }

        public static UIElement get4(string btnTxt, Action click, string mouseText = null, string ico = null, string text = null)
        {
            Texture2D texture = ico == null ? null : Main.Assets.Request<Texture2D>(ico, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            UIItemButton ui = new UIItemButton(btnTxt, texture, text);
            ui.MouseText = mouseText;
            ui.OnClick = click;

            return ui;
        }
    }
}
