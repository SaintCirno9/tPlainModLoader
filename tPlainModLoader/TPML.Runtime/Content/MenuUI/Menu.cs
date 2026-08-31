using tContentPatch.Threading;
using Terraria;
using Terraria.UI;

namespace tContentPatch.Content.MenuUI
{
    /// <summary/>
    public class Menu
    {
        /// <summary/>
        public static bool OpenMenu(UIState uistate, bool inGame = false)
        {
            if (Main.dedServ) return false;
            if (Main.showSplash) return false;

            if (MainThreadDispatcher.IsMainThread)
            {
                OpenMenuCore(uistate, inGame);
            }
            else
            {
                MainThreadDispatcher.Enqueue(() => OpenMenuCore(uistate, inGame));
            }

            return true;
        }

        private static void OpenMenuCore(UIState uistate, bool inGame)
        {
            if (inGame)
            {
                Main.InGameUI.SetState(uistate);
            }
            else
            {
                Main.menuMode = 888;
                Main.MenuUI.SetState(uistate);
            }
        }

        /// <summary/>
        public static bool OpenInGameMenu(UIState uistate)
        {
            return OpenMenu(uistate, true);
        }
    }
}
