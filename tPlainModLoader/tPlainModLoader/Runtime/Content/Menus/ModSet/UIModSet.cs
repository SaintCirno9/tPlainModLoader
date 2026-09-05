using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TPML.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;

namespace TPML.UI.Menus.ModSet
{
    internal class UIModSet : UIState
    {
        private UIState backUI = null;
        private List<ModSetting> mss = null;
        private ModSetting ms_this = null;
        private int mssIndex = -1;

        private UIElement ui_container = null;
        private UITextPanel<string> ui_title = null;
        private UIPanel ui_panel = null;
        private UIElement ui_set = null;
        private UIElement ui_btns1 = null;
        private UIElement ui_btns2 = null;
        private UIButton btn_save = null;
        private UIButton btn_prev = null;
        private UIButton btn_next = null;

        public UIModSet()
        {
            ui_container = new UIElement();
            ui_container.Width.Pixels = 640;
            ui_container.Height.Set(-30, 1f); // 上下各留 15px 安全边距，最大化展示区域
            ui_container.HAlign = 0.5f;
            ui_container.VAlign = 0.5f;

            // 1. 顶部标题
            ui_title = new UITextPanel<string>(string.Empty, 0.85f, true);
            ui_title.HAlign = 0.5f;
            ui_title.Top.Pixels = 0;
            ui_title.Height.Pixels = 40;
            ui_title.BackgroundColor = new Color(73, 94, 171);

            // 2. 中间内容面板
            ui_panel = new UIPanel();
            ui_panel.Width.Precent = 1f;
            ui_panel.HAlign = 0.5f;
            ui_panel.Top.Pixels = 46;
            ui_panel.Height.Set(-(46 + 36 + 10), 1f); // 默认单页高度填满
            ui_panel.BackgroundColor = Color.MidnightBlue * 0.85f;
            ui_panel.PaddingTop = 8;
            ui_panel.PaddingBottom = 8;
            ui_panel.PaddingLeft = 10;
            ui_panel.PaddingRight = 10;

            ui_set = new UIElement();
            ui_set.Width.Precent = 1f;
            ui_set.Height.Precent = 1f;
            ui_panel.Append(ui_set);

            // 3. 翻页栏 (多页时展示在底部操作栏上方)
            ui_btns1 = new UIElement();
            ui_btns1.Width.Precent = 1f;
            ui_btns1.Height.Pixels = 34;
            ui_btns1.VAlign = 1f;
            ui_btns1.Top.Pixels = -42;

            btn_prev = new UIButton("<", 0.9f);
            btn_prev.Width.Set(-4, 0.5f);
            btn_prev.Height.Precent = 1f;
            btn_prev.OnLeftClick += (e, s) => SetItem(mssIndex - 1);

            btn_next = new UIButton(">", 0.9f);
            btn_next.Width.Set(-4, 0.5f);
            btn_next.Height.Precent = 1f;
            btn_next.HAlign = 1f;
            btn_next.OnLeftClick += (e, s) => SetItem(mssIndex + 1);

            ui_btns1.Append(btn_prev);
            ui_btns1.Append(btn_next);

            // 4. 底部操作栏 (返回 / 保存 / 恢复默认)
            ui_btns2 = new UIElement();
            ui_btns2.Width.Precent = 1f;
            ui_btns2.Height.Pixels = 36;
            ui_btns2.VAlign = 1f;
            ui_btns2.Top.Pixels = 0; // 紧贴容器最底边

            UIButton btn_back = new UIButton("返回", 0.9f);
            btn_back.Width.Set(-4, 1 / 3f);
            btn_back.Height.Precent = 1f;
            btn_back.OnLeftClick += (e, s) => Back(backUI);

            btn_save = new UIButton("保存", 0.9f);
            btn_save.Width.Set(-4, 1 / 3f);
            btn_save.Height.Precent = 1f;
            btn_save.HAlign = 0.5f;
            btn_save.OnLeftClick += (e, s) => ModSet.SaveData(mss);

            UIButton btn2_setDeft = new UIButton("恢复默认", 0.9f);
            btn2_setDeft.Width.Set(-4, 1 / 3f);
            btn2_setDeft.Height.Precent = 1f;
            btn2_setDeft.HAlign = 1f;
            btn2_setDeft.OnLeftClick += (e, s) => ms_this?.SetDefault();

            ui_btns2.Append(btn_back);
            ui_btns2.Append(btn_save);
            ui_btns2.Append(btn2_setDeft);

            Append(ui_container);
            ui_container.Append(ui_title);
            ui_container.Append(ui_panel);
            ui_container.Append(ui_btns1);
            ui_container.Append(ui_btns2);
        }

        public void InitializeSetList(UIState backUI, List<ModSetting> mss, ModSetting open = null)
        {
            this.backUI = backUI;
            this.mss = mss;

            if (mss == null)
            {
                ui_title.SetText(string.Empty);
                ui_set.RemoveAllChildren();
                return;
            }

            int openIndex = -1;
            if (mss.Count > 0)
            {
                if (open == null) open = mss.First();
                openIndex = mss.IndexOf(open);
            }

            SetItem(openIndex);
        }

        public void SetItem(int index)
        {
            if (mss?.Count > 0)
            {
                if (index < 0) index = 0;
                else if (index >= mss.Count) index = mss.Count - 1;
            }
            else index = -1;

            mssIndex = index;
            ms_this = mssIndex == -1 ? null : mss[mssIndex];

            ui_title.SetText(ms_this?.Title ?? string.Empty);
            ui_set.RemoveAllChildren();

            UIElement uie = ms_this?.GetUI();
            if (uie != null) ui_set.Append(uie);
        }

        public void Back(UIState backUI)
        {
            if (backUI == null)
            {
                if (Main.gameMenu) Main.menuMode = 0;
                else IngameFancyUI.Close();
            }
            else
            {
                if (Main.gameMenu)
                {
                    Main.menuMode = 888;
                    Main.MenuUI.SetState(backUI);
                }
                else
                {
                    Main.InGameUI.SetState(backUI);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // 有需要保存的就显示保存按钮
            bool needSave = mss?.FirstOrDefault(i => i.NeedSave) != null;
            btn_save.isDraw = btn_save.isEnable = needSave;

            // 动态控制多页翻页栏
            bool hasMultiPages = mss != null && mss.Count > 1;
            btn_prev.isDraw = btn_prev.isEnable = hasMultiPages && mssIndex > 0;
            btn_next.isDraw = btn_next.isEnable = hasMultiPages && mssIndex < mss.Count - 1;

            if (hasMultiPages)
            {
                ui_btns1.Height.Pixels = 34;
                ui_panel.Height.Set(-(46 + 36 + 34 + 16), 1f);
            }
            else
            {
                ui_btns1.Height.Pixels = 0;
                ui_panel.Height.Set(-(46 + 36 + 10), 1f);
            }

            if (PlayerInput.Triggers.JustPressed.Inventory) Back(backUI);
        }
    }
}
