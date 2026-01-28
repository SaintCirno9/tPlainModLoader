using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using tContentPatch.Utils;
using Terraria;
using Terraria.UI;

namespace tContentPatch.ModPatch
{
    [HarmonyPatch(typeof(Main))]
    internal class Patch_Main : ListCopy<PatchMain>
    {
        private static List<PatchMain> mod = new List<PatchMain>();

        public Patch_Main() : base(mod) { }

        #region
        private static FieldInfo _gameInterfaceLayers_fi = null;
        #endregion

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void UpdatePrefix(GameTime gameTime)
        {
            try
            {
                UpdatePrefix_CanUpdateGameplay();

                mod.ForTry(item => item.UpdatePrefix(gameTime));
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }

        private static bool _UpdatePrefix_CanUpdateGameplay_old = false;

        private static void UpdatePrefix_CanUpdateGameplay()
        {
            if (Main.netMode != 0 && Main.netMode != 1) return;

            if (_UpdatePrefix_CanUpdateGameplay_old == false && Main.CanUpdateGameplay == true)
            {
                mod.ForTry(item => item.OnEnterWorld());
            }
            else if (_UpdatePrefix_CanUpdateGameplay_old && Main.CanUpdateGameplay == false)
            {
                mod.ForTry(item => item.OnEnterWorldPrefix());
            }

            _UpdatePrefix_CanUpdateGameplay_old = Main.CanUpdateGameplay;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.UpdatePostfix(gameTime));
        }

        [HarmonyPatch("SetupDrawInterfaceLayers")]
        [HarmonyPostfix]
        public static void SetupDrawInterfaceLayersPostfix()
        {
            try
            {
                if (_gameInterfaceLayers_fi == null)
                {
                    _gameInterfaceLayers_fi = typeof(Main).GetField("_gameInterfaceLayers", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                List<GameInterfaceLayer> gameInterfaceLayers = (List<GameInterfaceLayer>)_gameInterfaceLayers_fi.GetValue(Main.instance);

                mod.ForTry(item => item.SetupDrawInterfaceLayersPostfix(gameInterfaceLayers));
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }

        [HarmonyPatch("UpdateUIStates")]
        [HarmonyPrefix]
        public static void UpdateUIStatesPrefix(GameTime gameTime)
        {
            mod.ForTry(item => item.UpdateUIStatesPrefix(gameTime));
        }

        [HarmonyPatch("UpdateUIStates")]
        [HarmonyPostfix]
        public static void UpdateUIStatesPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.UpdateUIStatesPostfix(gameTime));
        }

        [HarmonyPatch("DoUpdateInWorld")]
        [HarmonyPrefix]
        public static void DoUpdateInWorldPrefix(Stopwatch sw)
        {
            mod.ForTry(item => item.DoUpdateInWorldPrefix(sw));
        }

        [HarmonyPatch("DoUpdateInWorld")]
        [HarmonyPostfix]
        public static void DoUpdateInWorldPostfix(Stopwatch sw)
        {
            mod.ForTry(item => item.DoUpdateInWorldPostfix(sw));
        }

        [HarmonyPatch("DrawMap")]
        [HarmonyPostfix]
        public static void DrawMapPostfix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMapPostfix(gameTime));
        }

        [HarmonyPatch("DrawMenu")]
        [HarmonyPrefix]
        public static void DrawMenuPrefix(GameTime gameTime)
        {
            mod.ForTry(item => item.DrawMenuPrefix(gameTime));
        }

        [HarmonyPatch("MouseText_DrawItemTooltip_GetLinesInfo")]
        [HarmonyPostfix]
        public static void MouseText_DrawItemTooltip_GetLinesInfoPostfix(Item item, ref int yoyoLogo, ref int researchLine,
            ref float oldKB, ref int numLines, ref string[] toolTipLine, ref Color[] lineColors)
        {
            try
            {
                foreach (PatchMain i in mod) i.MouseText_DrawItemTooltip_GetLinesInfoPostfix(item, ref yoyoLogo, ref researchLine,
                    ref oldKB, ref numLines, ref toolTipLine, ref lineColors);
            }
            catch (Exception ex)
            {
                OutputDebug.OutputException(ex);
            }
        }
    }
}
