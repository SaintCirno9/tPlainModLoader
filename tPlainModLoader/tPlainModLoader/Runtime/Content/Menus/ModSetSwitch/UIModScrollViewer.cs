using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using TPML.UI;
using TPML.Content;
using Terraria.GameContent.UI.Elements;

namespace TPML.UI.Menus.ModSetSwitch
{
    /// <summary>
    /// 一个页面, 顶部一个标题, 下面滚动UI
    /// </summary>
    internal class UIModScrollViewer : UIPanel
    {
        private UIScrollViewer2 ui_sv = null;
        private List<UIButton> ui_sv_list = null;

        public UIModScrollViewer(string title, bool scrollbarIsLeft)
        {
            UITextPanel<string> ui_title = new UITextPanel<string>(title, 0.5f, true);
            ui_title.Height.Pixels = 50;
            ui_title.HAlign = 0.5f;
            ui_title.DrawPanel = false;

            ui_sv_list = new List<UIButton>();
            ui_sv = new UIScrollViewer2(scrollbarIsLeft);
            ui_sv.Width.Precent = 1;
            ui_sv.Height.Set(-ui_title.Height.Pixels - 2, 1);
            ui_sv.Top.Pixels = ui_title.Height.Pixels;
            ui_sv.ItemMargin = 4;

            Append(ui_title);
            Append(ui_sv);
        }

        public void InitializeList(List<(string, Action, bool)> btnInfo)
        {
            ui_sv.ClearChild();
            ui_sv_list.Clear();

            if (btnInfo == null) return;

            foreach (var info in btnInfo)
            {
                string text = info.Item1 ?? string.Empty;
                if (text.Length > 15) text = $"{text.Substring(0, 15)}..";

                UIButton btn = new UIButton(text);
                btn.HAlign = 0.5f;
                btn.MaxWidth.Precent = 1;

                if (info.Item2 != null)
                {
                    Action action = () =>
                    {
                        foreach (UIButton i in ui_sv_list) i.EnableColorBack = new Color(63, 82, 151) * 0.8f;
                        btn.EnableColorBack = new Color(43, 60, 120);
                        info.Item2();
                    };
                    btn.OnLeftClick += (e, s) => action();
                    if (info.Item3) action();
                }
                else btn.isEnable = false;

                ui_sv.AddChild(btn);
                ui_sv_list.Add(btn);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            foreach (UIButton i in ui_sv_list)
            {
                if (i.isEnable || i.IsMouseHovering == false) continue;
                DrawTip.SetDraw("该模组没有设置页面");
                return;
            }
        }
    }
}
