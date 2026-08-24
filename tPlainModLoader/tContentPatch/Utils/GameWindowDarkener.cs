using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

        [DllImport("user32.dll", EntryPoint = "SetClassLong", CharSet = CharSet.Auto)]
        private static extern int SetClassLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetClassLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetClassLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetClassLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int fnObject);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private static volatile bool _applied = false;
        private static IntPtr _lastHwnd = IntPtr.Zero;
        private static FormSubclassWindow _subclass = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// 提供给 Prepatcher Cecil 预织入调用的早期窗口黑化直连入口（在 Main 构造函数结束前在 UI 主线程直接执行）
        /// </summary>
        public static void ApplyFromGame(object game)
        {
            try
            {
                if (game != null)
                {
                    PropertyInfo windowProp = game.GetType().GetProperty("Window", BindingFlags.Public | BindingFlags.Instance);
                    object windowObj = windowProp?.GetValue(game, null);
                    if (windowObj != null)
                    {
                        PropertyInfo handleProp = windowObj.GetType().GetProperty("Handle", BindingFlags.Public | BindingFlags.Instance);
                        object handleVal = handleProp?.GetValue(windowObj, null);
                        if (handleVal is IntPtr hWnd && hWnd != IntPtr.Zero)
                        {
                            Apply(hWnd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Add($"[GameWindowDarkener] ApplyFromGame 异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 对指定的游戏窗口句柄执行全方位黑化与防闪烁处理
        /// </summary>
        public static void Apply(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            if (_applied && _lastHwnd == hWnd) return;

            lock (_lock)
            {
                if (_applied && _lastHwnd == hWnd) return;

                try
                {
                    // 1. 替换 Win32 窗口类背景画刷为系统纯黑画刷 (BLACK_BRUSH = 4)
                    IntPtr blackBrush = GetStockObject(BLACK_BRUSH);
                    SetClassLongPtr(hWnd, GCLP_HBRBACKGROUND, blackBrush);

                    // 2. 启用 Windows 10 / 11 现代深色模式沉浸式标题栏
                    int darkMode = 1;
                    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref darkMode, sizeof(int));

                    // 强制刷新 DWM 窗口框架属性
                    SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                    // 3. 将底层 WinForms Form 的背景色直接置为黑色并拦截擦除
                    try
                    {
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

                            if (_subclass == null || _subclass.Handle != hWnd)
                            {
                                if (_subclass != null)
                                {
                                    try { _subclass.ReleaseHandle(); } catch { }
                                }
                                _subclass = new FormSubclassWindow();
                                _subclass.AssignHandle(hWnd);
                            }
                        }
                    }
                    catch
                    {
                    }

                    _applied = true;
                    _lastHwnd = hWnd;
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
}
