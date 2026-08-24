using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using HarmonyLib;
using Terraria;

namespace tContentPatch.Utils
{
    /// <summary>
    /// 游戏窗口黑化与防白屏闪烁引擎：<br/>
    /// 消除 XNA / WinForms 启动与重绘时的瞬间白屏闪烁，并将窗口类与 Form 背景替换为纯黑色，开启 Windows 沉浸式暗黑标题栏。
    /// </summary>
    public static class GameWindowDarkener
    {
        private const int GCLP_HBRBACKGROUND = -10;
        private const int BLACK_BRUSH = 4;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int WM_ERASEBKGND = 0x0014;

        [DllImport("user32.dll", EntryPoint = "SetClassLong")]
        private static extern IntPtr SetClassLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int fnObject);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private static volatile bool _applied = false;
        private static FormSubclassWindow _subclass = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// 启动毫秒级窗口看门狗线程：在窗口被创建与显示的第一时间注入黑化，彻底杜绝哪怕 1 帧的白屏！
        /// </summary>
        public static void StartWatchdog()
        {
            Thread thread = new Thread(() =>
            {
                for (int i = 0; i < 400 && !_applied; i++) // 轮询最多 4 秒
                {
                    try
                    {
                        if (Main.instance != null && Main.instance.Window != null && Main.instance.Window.Handle != IntPtr.Zero)
                        {
                            Apply(Main.instance.Window.Handle);
                            break;
                        }

                        Process currentProcess = Process.GetCurrentProcess();
                        if (currentProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            Apply(currentProcess.MainWindowHandle);
                            break;
                        }
                    }
                    catch
                    {
                    }

                    Thread.Sleep(10);
                }
            })
            {
                IsBackground = true,
                Name = "GameWindowDarkenerWatchdog"
            };
            thread.Start();
        }

        /// <summary>
        /// 对指定的游戏窗口句柄执行全方位黑化与防闪烁处理
        /// </summary>
        public static void Apply(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;

            lock (_lock)
            {
                try
                {
                    // 1. 替换 Win32 窗口类背景画刷为系统纯黑画刷 (BLACK_BRUSH = 4)
                    IntPtr blackBrush = GetStockObject(BLACK_BRUSH);
                    SetClassLongPtr32(hWnd, GCLP_HBRBACKGROUND, blackBrush);

                    // 2. 启用 Windows 10 / 11 现代深色模式沉浸式标题栏
                    int darkMode = 1;
                    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref darkMode, sizeof(int));

                    // 3. 将底层 WinForms Form 的背景色直接置为黑色并拦截擦除
                    if (Control.FromHandle(hWnd) is Form form)
                    {
                        form.BackColor = Color.Black;
                        form.ForeColor = Color.White;

                        // 开启双缓冲与用户绘制，防止重绘时闪白
                        MethodInfo setStyleMethod = typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (setStyleMethod != null)
                        {
                            setStyleMethod.Invoke(form, new object[] { ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.Opaque, true });
                        }

                        if (_subclass == null)
                        {
                            _subclass = new FormSubclassWindow();
                            _subclass.AssignHandle(hWnd);
                        }
                    }

                    _applied = true;
                    Log.Add("[GameWindowDarkener] 已成功对主窗口应用纯黑背景与防白屏闪烁配置");
                }
                catch (Exception ex)
                {
                    Log.Add($"[GameWindowDarkener] 黑化窗口异常: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 拦截窗口擦除背景消息 (WM_ERASEBKGND)，防止 WinForms / GDI 刷白客户区
        /// </summary>
        private class FormSubclassWindow : NativeWindow
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_ERASEBKGND)
                {
                    // 返回 1 (非零) 告诉 Windows 背景已被应用程序自行擦除，禁止 GDI 填充默认白底
                    m.Result = (IntPtr)1;
                    return;
                }

                base.WndProc(ref m);
            }
        }
    }

    /// <summary>
    /// 在游戏关键生命周期中自动加固窗口黑化（构造、客户端初始化与分辨率变更）
    /// </summary>
    [HarmonyPatch]
    internal static class Patch_GameWindowDarkener
    {
        [HarmonyPatch(typeof(Main), MethodType.Constructor)]
        [HarmonyPostfix]
        private static void MainConstructorPostfix()
        {
            try
            {
                if (Main.instance?.Window?.Handle != null && Main.instance.Window.Handle != IntPtr.Zero)
                {
                    GameWindowDarkener.Apply(Main.instance.Window.Handle);
                }
            }
            catch
            {
            }
        }

        [HarmonyPatch(typeof(Main), "ClientInitialize")]
        [HarmonyPostfix]
        private static void ClientInitializePostfix()
        {
            try
            {
                if (Main.instance?.Window?.Handle != null && Main.instance.Window.Handle != IntPtr.Zero)
                {
                    GameWindowDarkener.Apply(Main.instance.Window.Handle);
                }
            }
            catch
            {
            }
        }

        [HarmonyPatch(typeof(Main), "UpdateDisplaySettings")]
        [HarmonyPostfix]
        private static void UpdateDisplaySettingsPostfix()
        {
            try
            {
                if (Main.instance?.Window?.Handle != null && Main.instance.Window.Handle != IntPtr.Zero)
                {
                    GameWindowDarkener.Apply(Main.instance.Window.Handle);
                }
            }
            catch
            {
            }
        }
    }
}
