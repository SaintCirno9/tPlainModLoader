using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace AccessoryBox.Common.UI
{
    internal class UISwitchButtonImage : UIButtonImage
    {
        public Func<bool> GetVal = null;
        private bool oldV = false;
        protected Asset<Texture2D> img1 = null;
        protected Asset<Texture2D> img2 = null;

        public UISwitchButtonImage(float size, string mouseText, string image, string image2) : base(size, mouseText, image)
        {
            img1 = Texture;
            img2 = Main.Assets.Request<Texture2D>(image, AssetRequestMode.ImmediateLoad);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (GetVal == null) return;

            bool val = GetVal();
            if (val == oldV) return;
            oldV = val;

            SetImage(oldV ? img2 : img1);
        }
    }
}
