using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace TPML.UI
{
    /// <summary>
    /// 窗口, 可以打开关闭拖动位置和大小
    /// </summary>
    public class UIWindow : UIPanel
    {
        /// <summary/>
        public Action OnOpen = null;
        /// <summary/>
        public Action OnClose = null;
        /// <summary/>
        public UIElement Child { get; protected set; } = null;
        /// <summary/>
        public bool IsOpen { get; protected set; } = false;
        /// <summary/>
        public UIText ui_title { get; protected set; } = null;
        private UIElement WindowParent = null;
        private UIElement uie = null;
        private bool dragPos = false;
        private Vector2 dragPosOff = Vector2.Zero;
        private UIImage ui_dragSize_img = null;
        private bool dragSize = false;
        private Vector2 dragSizeOff = Vector2.Zero;

        /// <summary/>
        public UIWindow(string title, int width, int height)
        {
            Width.Pixels = width;
            Height.Pixels = height;
            MinWidth.Pixels = 100;
            MinHeight.Pixels = 100;
            Left.Set(Main.screenWidth / 2 - Width.Pixels / 2, 0);
            Top.Set(Main.screenHeight / 2 - Height.Pixels / 2, 0);
            SetPadding(10);

            Child = new UIElement();
            Child.Width.Precent = 1;

            uie = new UIElement();
            uie.Width.Precent = 1;
            uie.Height.Pixels = 40;
            uie.OnLeftMouseDown += (e, s) =>
            {
                dragPosOff = new Vector2(Left.Pixels - Main.mouseX, Top.Pixels - Main.mouseY);
                dragPos = true;
            };

            ui_title = new UIText(title ?? string.Empty);

            UIText ui_close = new UIText("X");
            ui_close.HAlign = 1;
            ui_close.OnUpdate += _ => ui_close.TextColor = ui_close.IsMouseHovering ? Color.Red : Color.White;
            ui_close.OnLeftClick += (e, s) => Close();

            ui_dragSize_img = new UIImage(Main.Assets.Request<Texture2D>("Images/UI/Wires_11", ReLogic.Content.AssetRequestMode.ImmediateLoad));
            ui_dragSize_img.Width.Pixels = ui_dragSize_img.Height.Pixels = 10;
            ui_dragSize_img.HAlign = 1;
            ui_dragSize_img.VAlign = 1;
            ui_dragSize_img.ScaleToFit = true;
            ui_dragSize_img.OnLeftMouseDown += (e, s) =>
            {
                dragSizeOff = new Vector2(Left.Pixels + Width.Pixels - Main.mouseX, Top.Pixels + Height.Pixels - Main.mouseY);
                dragSize = true;
            };

            uie.Append(ui_title);
            uie.Append(ui_close);
            Append(uie);
            Append(Child);
            Append(ui_dragSize_img);
        }

        /// <inheritdoc/>
        public override void Update(GameTime gameTime)
        {
            //if (IsOpen == false) return;

            base.Update(gameTime);

            #region 拖动位置
            if (dragPos)
            {
                Left.Pixels = Main.mouseX + dragPosOff.X;
                Top.Pixels = Main.mouseY + dragPosOff.Y;
                if (Main.mouseLeft == false) dragPos = false;
            }
            #endregion

            #region 拖动大小
            if (dragSize)
            {
                Width.Pixels = Main.mouseX + dragSizeOff.X - Left.Pixels;
                Height.Pixels = Main.mouseY + dragSizeOff.Y - Top.Pixels;
                if (Main.mouseLeft == false) dragSize = false;
            }
            #endregion

            int margin = 10;

            //限制大小
            if (Width.Pixels < MinWidth.Pixels) Width.Pixels = MinWidth.Pixels;
            else
            {
                if (Width.Pixels > MaxWidth.Pixels && MaxWidth.Pixels > MinWidth.Pixels) Width.Pixels = MaxWidth.Pixels;
                if (Width.Pixels > Main.screenWidth - margin * 2) Width.Pixels = Main.screenWidth - margin * 2;
            }
            if (Height.Pixels < MinHeight.Pixels) Height.Pixels = MinHeight.Pixels;
            else
            {
                if (Height.Pixels > MaxHeight.Pixels && MaxHeight.Pixels > MinHeight.Pixels) Height.Pixels = MaxHeight.Pixels;
                if (Height.Pixels > Main.screenHeight - margin * 2) Height.Pixels = Main.screenHeight - margin * 2;
            }

            //限制位置
            if (Left.Pixels + Width.Pixels > Main.screenWidth - margin) Left.Pixels = Main.screenWidth - margin - Width.Pixels;
            else if (Left.Pixels < margin) Left.Pixels = margin;

            if (Top.Pixels + Height.Pixels > Main.screenHeight - margin) Top.Pixels = Main.screenHeight - margin - Height.Pixels;
            else if (Top.Pixels < margin) Top.Pixels = margin;

            //
            uie.UpdateContainer_Height();
            Child.Height.Set(-(uie.Height.Pixels + ui_dragSize_img.Height.Pixels), 1);
            Child.Top.Pixels = uie.Height.Pixels;

            if (IsMouseHovering) Main.LocalPlayer.mouseInterface = true;
        }

        ///// <inheritdoc/>
        //public override void Draw(SpriteBatch spriteBatch)
        //{
        //    if (IsOpen == false) return;

        //    base.Draw(spriteBatch);
        //}

        /// <summary/>
        public virtual void Open(UIElement windowParent)
        {
            WindowParent?.RemoveChild(this);
            WindowParent = windowParent;
            if (WindowParent == null) return;

            WindowParent.Append(this);

            IsOpen = true;
            OnOpen?.Invoke();
        }

        /// <summary/>
        public virtual void Close()
        {
            WindowParent?.RemoveChild(this);
            WindowParent = null;

            IsOpen = false;
            dragPos = false;
            dragSize = false;
            OnClose?.Invoke();
        }
    }
}
