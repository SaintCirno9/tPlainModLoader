using System;
using System.Runtime.InteropServices;
using TPML.Core.Logging;

namespace TPML.Utils
{
    /// <summary>
    /// 在 ReLogic.Native 初始化前保证游戏窗口具备可用的 IMM 输入上下文。
    /// </summary>
    public static class ImeContextBootstrap
    {
        private const uint IACE_DEFAULT = 0x0010;

        private static readonly ILogger Logger = LogManager.GetLogger("ImeContextBootstrap");

        /// <summary>
        /// 确保指定窗口句柄具备有效的 IMM 输入上下文。
        /// 在游戏启动初始化前由 Prepatcher 织入调用。
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
                Logger.Info($"[IME-BOOTSTRAP] 游戏窗口已有输入上下文: hwnd={FormatPointer(windowHandle)}, himc={FormatPointer(existingContext)}");
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
}
