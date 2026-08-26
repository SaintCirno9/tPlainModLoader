using System.Text;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace RecipeBrowser
{
    public enum ShowOtherPlayersFavoritedRecipesOption
    {
        ShowAll,
        ShowTeamOnly,
        Hide
    }

    public class HelpUIPanel : UIPanel
    {
    }

    public class HelpUI
    {
        internal static HelpUI instance;
        internal static Color color = Color.LightPink * 0.5f;

        internal UIPanel mainPanel;
        internal UIMessageBox message;

        public HelpUI()
        {
            instance = this;
        }

        internal UIElement CreateHelpPanel()
        {
            mainPanel = new HelpUIPanel();
            mainPanel.SetPadding(6f);
            mainPanel.BackgroundColor = color;
            mainPanel.Top.Set(20f, 0f);
            mainPanel.Height.Set(-20f, 1f);
            mainPanel.Width.Set(0f, 1f);

            StringBuilder sb = new StringBuilder();
            string blue = Utils.Hex3(Color.CornflowerBlue);
            string gold = Utils.Hex3(Color.Goldenrod);
            string green = Utils.Hex3(Utilities.yesColor);
            string yellow = Utils.Hex3(Utilities.maybeColor);
            string red = Utils.Hex3(Utilities.noColor);

            sb.AppendLine(RBLanguage.GetText("HelpUI", "Recipes").Replace("{0}", blue).Replace("{1}", gold));
            sb.AppendLine(RBLanguage.GetText("HelpUI", "Craft").Replace("{0}", blue).Replace("{1}", green).Replace("{2}", yellow).Replace("{3}", red));
            sb.AppendLine(RBLanguage.GetText("HelpUI", "Items").Replace("{0}", blue).Replace("{1}", gold));
            sb.AppendLine(RBLanguage.GetText("HelpUI", "Bestiary").Replace("{0}", blue).Replace("{1}", gold));
            sb.AppendLine(RBLanguage.GetText("HelpUI", "Borders").Replace("{0}", blue).Replace("{1}", gold));
            sb.AppendLine(RBLanguage.GetText("HelpUI", "Favorite").Replace("{0}", blue).Replace("{1}", gold));
            sb.AppendLine(RBLanguage.GetText("HelpUI", "QueryHoveredItem").Replace("{0}", blue));
            sb.AppendLine(RBLanguage.GetText("HelpUI", "CategoriesSubCategoriesSortsAndFilters").Replace("{0}", blue));

            message = new UIMessageBox(sb.ToString());
            message.Width.Set(-25f, 1f);
            message.Height.Set(0f, 1f);
            mainPanel.Append(message);

            FixedUIScrollbar scrollbar = new FixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            scrollbar.SetView(100f, 1000f);
            scrollbar.Top.Set(5f, 0f);
            scrollbar.Height.Set(-30f, 1f);
            scrollbar.HAlign = 1f;
            mainPanel.Append(scrollbar);
            message.SetScrollbar(scrollbar);

            return mainPanel;
        }
    }
}
