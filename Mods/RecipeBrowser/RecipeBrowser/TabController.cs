using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RecipeBrowser.UIElements;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser
{
    public class LootCache
    {
        public static LootCache instance;
        public Dictionary<int, List<int>> lootInfos;

        public LootCache()
        {
            instance = this;
            lootInfos = new Dictionary<int, List<int>>();
        }
    }

    public class UITabButton : UIBottomlessPanel
    {
        public Color BaseColor { get; set; }
        public UIText LabelText { get; set; }
        public int PanelIndex { get; set; }
        public bool IsSelected { get; set; }

        public UITabButton(Color baseColor, string text, int panelIndex)
        {
            BaseColor = baseColor;
            PanelIndex = panelIndex;
            SetPadding(0f);
            Width.Set(80f, 0f);
            Height.Set(22f, 0f);

            LabelText = new UIText(text, 0.85f, false);
            LabelText.HAlign = 0.5f;
            LabelText.VAlign = 0.5f;
            Append(LabelText);

            UpdateVisuals(false);
        }

        public void UpdateVisuals(bool isSelected)
        {
            IsSelected = isSelected;
            if (isSelected)
            {
                BackgroundColor = BaseColor;
                BorderColor = Color.Black;
                Height.Set(24f, 0f);
                LabelText.TextColor = Color.Goldenrod;
            }
            else
            {
                BackgroundColor = BaseColor * 0.42f;
                BorderColor = Color.Black * 0.7f;
                Height.Set(22f, 0f);
                LabelText.TextColor = Color.Silver * 0.9f;
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            if (!IsSelected)
            {
                BackgroundColor = BaseColor * 0.75f;
                LabelText.TextColor = Color.White;
            }
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            if (!IsSelected)
            {
                BackgroundColor = BaseColor * 0.42f;
                LabelText.TextColor = Color.Silver * 0.9f;
            }
        }
    }

    public class TabController
    {
        private UIElement parent;
        private List<UIElement> panels;
        private List<UITabButton> buttons;
        internal int currentPanel;

        public TabController(UIElement parent)
        {
            this.parent = parent;
            panels = new List<UIElement>();
            buttons = new List<UITabButton>();
        }

        public void AddPanel(UIElement element)
        {
            panels.Add(element);
        }

        public void AddButton(UITabButton element)
        {
            buttons.Add(element);
        }

        public void SetPanel(int panelIndex)
        {
            if (panelIndex < 0 || panelIndex >= panels.Count) return;
            currentPanel = panelIndex;

            if (SharedUI.instance?.sortsAndFiltersPanel != null && SharedUI.instance.sortsAndFiltersPanel.Parent != null)
            {
                SharedUI.instance.sortsAndFiltersPanel.Parent.RemoveChild(SharedUI.instance.sortsAndFiltersPanel);
            }

            foreach (var panel in panels)
            {
                if (parent.Elements.Contains(panel))
                {
                    parent.RemoveChild(panel);
                }
            }

            for (int i = buttons.Count - 1; i >= 0; i--)
            {
                var btn = buttons[i];
                parent.RemoveChild(btn);
                parent.Append(btn);
            }

            if (panelIndex < buttons.Count)
            {
                parent.RemoveChild(buttons[panelIndex]);
                parent.Append(panels[panelIndex]);
                parent.Append(buttons[panelIndex]);
            }
            else
            {
                parent.Append(panels[panelIndex]);
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                buttons[i].UpdateVisuals(i == panelIndex);
            }

            var dropdown = RecipeBrowserUI.instance?.ModFilterDropdown;
            if (dropdown != null && dropdown.Parent == parent)
            {
                parent.RemoveChild(dropdown);
                parent.Append(dropdown);
            }

            switch (panelIndex)
            {
                case 2: // Item Catalogue
                    if (SharedUI.instance != null && ItemCatalogueUI.instance?.mainPanel != null)
                    {
                        SharedUI.instance.sortsAndFiltersPanel.Top.Set(0f, 0f);
                        SharedUI.instance.sortsAndFiltersPanel.Width.Set(-272f, 1f);
                        SharedUI.instance.sortsAndFiltersPanel.Height.Set(60f, 0f);
                        SharedUI.instance.updateNeeded = true;
                        ItemCatalogueUI.instance.mainPanel.Append(SharedUI.instance.sortsAndFiltersPanel);
                    }
                    break;
                case 0: // Recipe Catalogue
                    if (SharedUI.instance != null && RecipeCatalogueUI.instance?.mainPanel != null)
                    {
                        SharedUI.instance.sortsAndFiltersPanel.Top.Set(60f, 0f);
                        SharedUI.instance.sortsAndFiltersPanel.Width.Set(-52f, 1f);
                        SharedUI.instance.sortsAndFiltersPanel.Height.Set(60f, 0f);
                        RecipeCatalogueUI.instance.mainPanel.Append(SharedUI.instance.sortsAndFiltersPanel);
                        SharedUI.instance.updateNeeded = true;
                        if (SharedUI.instance.SelectedCategory?.name == ArmorSetFeatureHelper.ArmorSetsInternalName)
                        {
                            SharedUI.instance.SelectedCategory = SharedUI.instance.categories[0];
                        }
                    }
                    break;
            }

            parent.Recalculate();
        }
    }
}
