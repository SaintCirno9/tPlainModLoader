using Microsoft.Xna.Framework;
using System;
using tContentPatch.ModLoad;
using Terraria;
using TPML.Content.Engine;

namespace tContentPatch.Content
{
    /// <summary>
    /// 标题界面自动加载模组（M2 迁移：Harmony → MonoMod）
    /// </summary>
    internal class AutoLoadMod
    {
        private static bool oneLoadMod = true;

        static AutoLoadMod()
        {
            LoaderControl.OnModLoad_Start += _ => oneLoadMod = false;
        }

        /// <summary>集中注册全部补丁（由 ContentPatch_Initialize 调用）</summary>
        public static void RegisterAll()
        {
            // Main.Update(GameTime)（实例）
            HookRegistry.Add(MethodLookup.Instance(typeof(Main), "Update", typeof(GameTime)),
                (Action<Action<Main, GameTime>, Main, GameTime>)((orig, self, gameTime) =>
                {
                    Prefix(gameTime);
                    orig(self, gameTime);
                }));
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
