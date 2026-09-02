using Microsoft.Xna.Framework;
using TPML.ModLoad;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// 标题界面自动加载模组（由 Patch_Main.Hook_Update 单点调度）
    /// </summary>
    internal static class AutoLoadMod
    {
        private static bool oneLoadMod = true;

        static AutoLoadMod()
        {
            LoaderControl.OnModLoad_Start += _ => oneLoadMod = false;
        }

        public static void RegisterAll()
        {
            // 已收敛由 Patch_Main 单点调度，无需单独 Detour
        }

        internal static void Prefix(GameTime gameTime)
        {
            if (oneLoadMod == false) return;
            if (Main.dedServ) return;
            if (Main.showSplash) return;

            if (Main.gameMenu == false) return;
            if (Main.menuMode != Terraria.ID.MenuID.Title) return;
            if (LoaderControl.CanLoad == false) return;

            oneLoadMod = false;
            LoaderControl.Load();
        }
    }
}
