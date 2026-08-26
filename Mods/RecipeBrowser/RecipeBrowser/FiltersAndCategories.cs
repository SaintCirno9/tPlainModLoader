using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;

namespace RecipeBrowser
{
    public class Category
    {
        internal string name;
        internal string displayName;
        internal Predicate<Item> belongs;
        internal List<Category> subCategories;
        internal List<Sort> sorts;
        internal List<Filter> filters;
        internal UISilentImageButton button;
        internal Category parent;

        public Category(string internalName, string displayName, Predicate<Item> belongs, Texture2D texture = null)
        {
            if (texture == null)
            {
                texture = RBTextures.GetTexture("Images/sortAmmo");
            }
            name = internalName;
            this.displayName = displayName;
            subCategories = new List<Category>();
            sorts = new List<Sort>();
            filters = new List<Filter>();
            this.belongs = belongs;
            button = new UISilentImageButton(texture);
            button.OnLeftClick += (evt, el) =>
            {
                if (SharedUI.instance != null)
                {
                    SharedUI.instance.SelectedCategory = this;
                }
            };
        }

        public Category(string internalName, string name, Predicate<Item> belongs, string textureFileName)
            : this(internalName, name, belongs, RBTextures.GetTexture(textureFileName))
        {
        }

        internal bool BelongsRecursive(Item item)
        {
            if (belongs(item)) return true;
            return subCategories.Any(x => x.belongs(item));
        }

        internal void ParentAddToSorts(List<Sort> availableSorts)
        {
            if (parent != null) parent.ParentAddToSorts(availableSorts);
            availableSorts.AddRange(sorts);
        }

        internal void ParentAddToFilters(List<Filter> availableFilters)
        {
            if (parent != null) parent.ParentAddToFilters(availableFilters);
            availableFilters.AddRange(filters);
        }
    }

    public class Filter
    {
        internal string name;
        internal Predicate<Item> belongs;
        internal Predicate<Recipe> recipeBelongs;
        internal List<Category> subCategories;
        internal List<Sort> sorts;
        internal UISilentImageButton button;
        internal Texture2D texture;

        public Filter(string name, Predicate<Item> belongs, Texture2D texture)
        {
            this.name = name;
            this.texture = texture;
            subCategories = new List<Category>();
            sorts = new List<Sort>();
            this.belongs = belongs;
            button = new UISilentImageButton(texture);
            button.OnLeftClick += (evt, el) =>
            {
                button.selected = !button.selected;
                if (ItemCatalogueUI.instance != null) ItemCatalogueUI.instance.updateNeeded = true;
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
            };
        }
    }

    public class CycleFilter : Filter
    {
        private int index;
        private List<Filter> filters;
        private List<UISilentImageButton> buttons;

        public CycleFilter(string name, string textureFileName, List<Filter> filters)
            : this(name, RBTextures.GetTexture(textureFileName), filters)
        {
        }

        public CycleFilter(string name, Texture2D texture, List<Filter> filters)
            : base(name, item => false, texture)
        {
            buttons = new List<UISilentImageButton>();
            this.filters = filters;
            belongs = item => index == 0 || filters[index - 1].belongs(item);
            recipeBelongs = recipe => index == 0 || (filters[index - 1].recipeBelongs?.Invoke(recipe) ?? true);

            UISilentImageButton baseBtn = new UISilentImageButton(texture);
            baseBtn.OnLeftClick += (evt, el) => ButtonBehavior(true);
            baseBtn.OnRightClick += (evt, el) => ButtonBehavior(false);
            buttons.Add(baseBtn);

            for (int i = 0; i < filters.Count; i++)
            {
                var fBtn = new UISilentImageButton(filters[i].texture);
                fBtn.OnLeftClick += (evt, el) => ButtonBehavior(true);
                fBtn.OnRightClick += (evt, el) => ButtonBehavior(false);
                fBtn.OnMiddleClick += (evt, el) => ButtonBehavior(false, true);
                fBtn.Color = filters[i].button.Color;
                buttons.Add(fBtn);
            }
            button = buttons[0];

            void ButtonBehavior(bool increment, bool zero = false)
            {
                button.selected = false;
                index = !zero ? (increment ? ((index + 1) % buttons.Count) : ((buttons.Count + index - 1) % buttons.Count)) : 0;
                button = buttons[index];
                if (index != 0) button.selected = true;
                if (ItemCatalogueUI.instance != null) ItemCatalogueUI.instance.updateNeeded = true;
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
            }
        }

        public string FormatText(string modName)
        {
            try { return string.Format(name, modName); } catch { return name; }
        }
    }

    public class DoubleFilter : Filter
    {
        private bool right;
        private string other;

        public DoubleFilter(string name, string other, Texture2D texture, Predicate<Item> belongs)
            : base(name, belongs, texture)
        {
            this.other = other;
            base.belongs = item => belongs(item) ^ right;
            button = new UIBadgedSilentImageButton(texture, "");
            button.OnLeftClick += (evt, el) =>
            {
                button.selected = !button.selected;
                if (ItemCatalogueUI.instance != null) ItemCatalogueUI.instance.updateNeeded = true;
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
            };
            button.OnRightClick += (evt, el) =>
            {
                right = !right;
                (button as UIBadgedSilentImageButton).badgeText = right ? "X" : "";
                if (ItemCatalogueUI.instance != null) ItemCatalogueUI.instance.updateNeeded = true;
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
            };
        }
    }

    public class MutuallyExclusiveFilter : Filter
    {
        private List<Filter> exclusives;

        public MutuallyExclusiveFilter(string name, Predicate<Item> belongs, Texture2D texture)
            : base(name, belongs, texture)
        {
            button.OnLeftClick += (evt, el) =>
            {
                if (button.selected && exclusives != null)
                {
                    foreach (var exc in exclusives)
                    {
                        if (exc != this) exc.button.selected = false;
                    }
                }
            };
        }

        internal void SetExclusions(List<Filter> exclusives)
        {
            this.exclusives = exclusives;
        }
    }

    public class ModCategory
    {
        internal string name;
        internal string parent;
        internal Texture2D icon;
        internal Predicate<Item> belongs;

        public ModCategory(string name, string parent, Texture2D icon, Predicate<Item> belongs)
        {
            this.name = name;
            this.parent = parent;
            this.icon = icon;
            this.belongs = belongs;
        }
    }
}
