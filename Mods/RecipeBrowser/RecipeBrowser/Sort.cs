using System;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;

namespace RecipeBrowser
{
    public class Sort
    {
        internal Func<Item, Item, int> sort;
        internal Func<Recipe, Recipe, int> recipeSort;
        internal Func<bool> sortAvailable;
        internal UISilentImageButton button;

        public Sort(string hoverText, Texture2D texture, Func<Item, Item, int> sort)
        {
            this.sort = sort;
            button = new UISilentImageButton(texture);
            button.OnLeftClick += (evt, el) =>
            {
                if (SharedUI.instance != null)
                {
                    SharedUI.instance.SelectedSort = this;
                }
            };
        }

        public Sort(string hoverText, string textureName, Func<Item, Item, int> sort)
            : this(hoverText, RBTextures.GetTexture(textureName), sort)
        {
        }
    }
}
