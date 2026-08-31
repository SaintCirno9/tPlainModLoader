using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using TPML.Core.Logging;

namespace tContentPatch.Content.Menus.Patch_MainMenu
{
    /// <summary>
    /// 主菜单一级列表增强：注入“模组 / Mods”独立入口，并适配按钮排版与点击交互
    /// </summary>
    internal static class Patch_MainMenu
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Patch_MainMenu");

        /// <summary>集中注册主菜单补丁</summary>
        public static void RegisterAll()
        {
            IL_Main.DrawMenu += Hook_DrawMenu;
            Logger.Info("★ 主菜单一级“模组”入口补丁已注册");
        }

        private static void Hook_DrawMenu(ILContext il)
        {
            var c = new ILCursor(il);

            // 1. 定位到 menuMode == 0 分支内的 ClearVisualPostProcessEffects 调用
            if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(typeof(Main), "ClearVisualPostProcessEffects")))
            {
                Logger.Warn("未能定位 ClearVisualPostProcessEffects，跳过主菜单排版注入");
                return;
            }

            // 2. 调整主菜单排版参数：
            // 起始 Y 坐标: 220 -> 205 (留出 8 个按钮的纵向空间)
            // 按钮总数: 7 -> 8
            // 按钮行间距: 52 -> 48
            if (c.TryGotoNext(i => i.MatchLdcI4(220)))
            {
                c.Next.Operand = 205;
            }

            if (c.TryGotoNext(i => i.MatchLdcI4(7)))
            {
                c.Next.OpCode = OpCodes.Ldc_I4_8;
                c.Next.Operand = null;
            }

            if (c.TryGotoNext(i => i.MatchLdcI4(52)))
            {
                c.Next.OpCode = OpCodes.Ldc_I4_S;
                c.Next.Operand = (sbyte)48;
            }

            // 3. 定位到“创意工坊 / 材质包”之后、“设置 (Lang.menu[14])”之前的按钮加载位置
            if (!c.TryGotoNext(
                i => i.MatchLdsfld(typeof(Lang), nameof(Lang.menu)),
                i => i.MatchLdcI4(14)))
            {
                Logger.Warn("未能定位 Lang.menu[14]，跳过主菜单“模组”按钮插入");
                return;
            }

            // 回退到加载 array9 与 num12 的位置 (即 ldloc.s array9, ldloc.s num12)
            c.Index -= 2;

            var array9Var = c.Next.Operand as VariableDefinition;
            var num12Var = c.Next.Next.Operand as VariableDefinition;

            if (array9Var == null || num12Var == null)
            {
                Logger.Warn("未能解析 array9 或 num12 局部变量，跳过主菜单“模组”按钮插入");
                return;
            }

            // 插入我们的委托调用：InsertModsButton(Main self, string[] array9, ref int num12)
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, array9Var);
            c.Emit(OpCodes.Ldloca, num12Var);
            c.Emit(OpCodes.Call, typeof(Patch_MainMenu).GetMethod(nameof(InsertModsButton), BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static void InsertModsButton(Main main, string[] array9, ref int num12)
        {
            string modsText = Language.ActiveCulture?.LegacyId == (int)GameCulture.CultureName.Chinese ? "模组" : "Mods";
            array9[num12] = modsText;

            if (main.selectedMenu == num12)
            {
                SoundEngine.PlaySound(10);
                ModManager.ModManager.OpenModManagerMenu(null);
            }

            num12++;
        }
    }
}
