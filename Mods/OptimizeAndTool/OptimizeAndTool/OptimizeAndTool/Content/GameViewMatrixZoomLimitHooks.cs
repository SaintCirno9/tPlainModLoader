using CommandHelp;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace OptimizeAndTool.Content
{
    /// <summary>
    /// 视口缩放限制突破与透视照明 Hook 门控（基于 HookGen 强类型 On_ / IL_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    internal static class GameViewMatrixZoomLimitHooks
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(false, false);
        public static GetSetReset<float> Scale = new GetSetReset<float>(1f, 1f, ScaleClamp);
        public static GetSetReset<bool> Light = new GetSetReset<bool>(false, false);
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            IL_Main.DoDraw += Patch_DoDraw;
            On_Lighting.GetColor_int_int += Hook_GetColor;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            IL_Main.DoDraw -= Patch_DoDraw;
            On_Lighting.GetColor_int_int -= Hook_GetColor;
            _registered = false;
        }

        private static void Patch_DoDraw(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                    i => i.MatchLdsfld(typeof(Main), nameof(Main.ForcedMinimumZoom)),
                    i => i.MatchLdsfld(typeof(Main), nameof(Main.GameZoomTarget)),
                    i => i.MatchLdcR4(1f),
                    i => i.MatchLdcR4(2f),
                    i => i.MatchCall(typeof(MathHelper), nameof(MathHelper.Clamp))))
                {
                    c.Index--;
                    c.Remove();
                    c.Emit(OpCodes.Call, typeof(GameViewMatrixZoomLimitHooks).GetMethod(nameof(PatchClamp)));
                }
            }
            catch { }
        }

        private static Color Hook_GetColor(On_Lighting.orig_GetColor_int_int orig, int x, int y)
        {
            if (Light.val) return Color.White;
            return orig(x, y);
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get3("zoom", Enable,
                new CommandHRA<float>("scale", Scale, new CommandFloat()),
                CommandBuild.get3("light", Light)),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get1(Enable, Scale, float.Parse, mouseText: "缩放值<float>", text: "取消限制"),
                UIBuild.get2(Light, mouseText: "照明设置为复古或迷幻可看到范围外图格", text: "透视照明"),
            };

            return uis;
        }

        public class GameViewMatrixZoomLimitLoop : TPML.Content.ModSystem
        {
            public override void DoUpdateInWorldPrefix()
            {
                if (Enable.val == false) return;

                if (PlayerInput.Triggers.Current.ViewZoomIn)
                {
                    Scale.val += Scale.val * 0.01f;
                }
                else if (PlayerInput.Triggers.Current.ViewZoomOut)
                {
                    Scale.val -= Scale.val * 0.01f;
                }

                Main.GameZoomTarget = Scale.val;
            }
        }

        public static float ScaleClamp(float value)
        {
            return MathHelper.Clamp(value, 0.1f, 100f);
        }

        public static float PatchClamp(float value, float min, float max)
        {
            if (Enable.val == false) return MathHelper.Clamp(value, min, max);

            return ScaleClamp(value);
        }
    }
}
