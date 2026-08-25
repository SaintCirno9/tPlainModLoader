using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Terraria;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// GABP 游戏内全屏与 UI 截图捕获工具（基于 Win32 强力置顶与 GDI 高保真捕获）
    /// 作者: SaintCirno9
    /// </summary>
    public static class ScreenCaptureTools
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;

        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/capture_screenshot",
                    Description = "捕获当前游戏画面的完整全屏截图（包含 UI、菜单、创造模式物品浏览器与输入法面板），并保存为 PNG 图像文件。",
                    Tags = new List<string> { "read-only", "ui", "diagnostic" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            outputPath = new { type = "string", description = "保存截图的绝对路径（可选；默认自动生成至 Screenshots 目录）" }
                        }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/capture_screenshot":
                case "tpml_capture_screenshot":
                case "capture_screenshot":
                    {
                        string outputPath = args?["outputPath"]?.ToString();
                        if (string.IsNullOrWhiteSpace(outputPath))
                        {
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Screenshots");
                            Directory.CreateDirectory(dir);
                            outputPath = Path.Combine(dir, $"screenshot_{timestamp}.png");
                        }
                        else
                        {
                            string dir = Path.GetDirectoryName(outputPath);
                            if (!string.IsNullOrEmpty(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                        }

                        return await MainThreadQueue.EnqueueAsync(() => CaptureScreenshotInternal(outputPath));
                    }

                default:
                    return null;
            }
        }

        private static object CaptureScreenshotInternal(string outputPath)
        {
            try
            {
                IntPtr hWnd = Main.instance.Window.Handle;

                // Win32 强力强制置顶激活
                IntPtr hForeWnd = GetForegroundWindow();
                uint foreThread = GetWindowThreadProcessId(hForeWnd, IntPtr.Zero);
                uint curThread = GetCurrentThreadId();

                if (foreThread != curThread)
                {
                    AttachThreadInput(curThread, foreThread, true);
                    BringWindowToTop(hWnd);
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                    AttachThreadInput(curThread, foreThread, false);
                }
                else
                {
                    BringWindowToTop(hWnd);
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }

                // 瞬时临时置顶确保物理屏幕可见
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                Thread.Sleep(120);
                SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                GetClientRect(hWnd, out RECT rect);
                POINT pt = new POINT { X = 0, Y = 0 };
                ClientToScreen(hWnd, ref pt);

                int width = rect.Width > 0 ? rect.Width : Main.screenWidth;
                int height = rect.Height > 0 ? rect.Height : Main.screenHeight;

                using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(pt.X, pt.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                    }

                    bmp.Save(outputPath, ImageFormat.Png);
                }

                long fileSize = new FileInfo(outputPath).Length;
                return new
                {
                    success = true,
                    outputPath = Path.GetFullPath(outputPath),
                    width,
                    height,
                    fileSize,
                    message = $"游戏截图成功保存至: {outputPath}"
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    error = ex.Message,
                    message = $"截图异常: {ex.Message}"
                };
            }
        }
    }
}
