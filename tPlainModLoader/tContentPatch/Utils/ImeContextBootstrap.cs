using System;
using System.Runtime.InteropServices;
using HarmonyLib;
using ReLogic.Localization.IME;
using Terraria;
using TPML.Core.Logging;

namespace tContentPatch.Utils
{
    /// <summary>
    /// 管理游戏窗口的 Windows IMM 输入上下文 (HIMC)，确保 ReLogic 原生输入法生命周期正确激活。
    /// </summary>
    public static class ImeContextBootstrap
    {
        private const uint IACE_DEFAULT = 0x0010;

        private static readonly ILogger Logger = LogManager.GetLogger("ImeContextBootstrap");

        /// <summary>
        /// 确保指定窗口句柄具备有效的 IMM 输入上下文。
        /// 在游戏启动初始化前（Prepatcher 注入）及每次输入法启用前调用。
        /// </summary>
        public static void EnsureAssociated(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                Logger.Warn("[IME-BOOTSTRAP] 游戏窗口句柄为空，无法恢复输入上下文");
                return;
            }

            IntPtr existingContext = ImmGetContext(windowHandle);
            if (existingContext != IntPtr.Zero)
            {
                ImmReleaseContext(windowHandle, existingContext);
                return;
            }

            bool restoredDefault = ImmAssociateContextEx(windowHandle, IntPtr.Zero, IACE_DEFAULT);
            IntPtr restoredContext = ImmGetContext(windowHandle);
            if (restoredContext != IntPtr.Zero)
            {
                ImmReleaseContext(windowHandle, restoredContext);
                Logger.Info($"[IME-BOOTSTRAP] 已恢复游戏窗口的默认输入上下文: hwnd={FormatPointer(windowHandle)}, himc={FormatPointer(restoredContext)}");
                return;
            }

            IntPtr createdContext = ImmCreateContext();
            if (createdContext == IntPtr.Zero)
            {
                Logger.Error($"[IME-BOOTSTRAP] 默认输入上下文恢复失败，且无法创建新上下文: hwnd={FormatPointer(windowHandle)}");
                return;
            }

            ImmAssociateContext(windowHandle, createdContext);
            IntPtr associatedContext = ImmGetContext(windowHandle);
            if (associatedContext == IntPtr.Zero)
            {
                ImmDestroyContext(createdContext);
                Logger.Error($"[IME-BOOTSTRAP] 新输入上下文关联失败: hwnd={FormatPointer(windowHandle)}, createdHimc={FormatPointer(createdContext)}");
                return;
            }

            ImmReleaseContext(windowHandle, associatedContext);
            Logger.Warn($"[IME-BOOTSTRAP] 默认输入上下文不可用，已创建并关联新上下文: hwnd={FormatPointer(windowHandle)}, himc={FormatPointer(associatedContext)}");
        }

        private static string FormatPointer(IntPtr value)
        {
            return "0x" + unchecked((ulong)value.ToInt64()).ToString("X");
        }

        [DllImport("imm32.dll")]
        private static extern IntPtr ImmGetContext(IntPtr windowHandle);

        [DllImport("imm32.dll")]
        private static extern bool ImmReleaseContext(IntPtr windowHandle, IntPtr inputContext);

        [DllImport("imm32.dll")]
        private static extern bool ImmAssociateContextEx(IntPtr windowHandle, IntPtr inputContext, uint flags);

        [DllImport("imm32.dll")]
        private static extern IntPtr ImmCreateContext();

        [DllImport("imm32.dll")]
        private static extern IntPtr ImmAssociateContext(IntPtr windowHandle, IntPtr inputContext);

        [DllImport("imm32.dll")]
        private static extern bool ImmDestroyContext(IntPtr inputContext);
    }

    /// <summary>
    /// 在 PlatformIme.Enable 执行前确保窗口 HIMC 与焦点状态就绪，保障 ReLogic.Native 能够成功激活输入法。
    /// </summary>
    [HarmonyPatch(typeof(PlatformIme), nameof(PlatformIme.Enable))]
    internal static class Patch_PlatformIme_Enable
    {
        private static readonly ILogger Logger = LogManager.GetLogger("PlatformImePatch");

        [DllImport("ReLogic.Native.dll", EntryPoint = "ImeUi_IsEnabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ImeUi_IsEnabled();

        [DllImport("ReLogic.Native.dll", EntryPoint = "ImeUi_Enable")]
        private static extern void ImeUi_Enable([MarshalAs(UnmanagedType.I1)] bool bEnable);

        [HarmonyPrefix]
        private static void Prefix(PlatformIme __instance)
        {
            try
            {
                if (Main.dedServ) return;

                IntPtr hwnd = IntPtr.Zero;
                if (Main.instance?.Window != null)
                {
                    hwnd = Main.instance.Window.Handle;
                }

                if (hwnd != IntPtr.Zero)
                {
                    ImeContextBootstrap.EnsureAssociated(hwnd);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[IME] PlatformIme.Enable 前置上下文准备异常: {ex.Message}");
            }
        }

        [HarmonyPostfix]
        private static void Postfix(PlatformIme __instance)
        {
            try
            {
                if (Main.dedServ) return;

                if (!ImeUi_IsEnabled())
                {
                    ImeUi_Enable(true);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[IME] PlatformIme.Enable 后置状态保障异常: {ex.Message}");
            }
        }
    }
}
