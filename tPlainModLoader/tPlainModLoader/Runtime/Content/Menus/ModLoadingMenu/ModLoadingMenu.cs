using System;
using TPML.ModLoad;
using Terraria;

namespace TPML.UI.Menus.ModLoadingMenu
{
    internal class ModLoadingMenu
    {
        private static UILoadProgressBar uistate = null;

        internal static void OpenLoadMenu(IModLoaderState modLoaderState, Action cancelLoad)
        {
            if (Main.dedServ) return;
            if (Main.showSplash) return;

            if (uistate == null)
            {
                uistate = new UILoadProgressBar();
            }

            if (modLoaderState != null)
            {
                uistate.InitializeLoader(modLoaderState, cancelLoad);
                MenuUI.Menu.OpenMenu(uistate);
            }
        }
    }
}
