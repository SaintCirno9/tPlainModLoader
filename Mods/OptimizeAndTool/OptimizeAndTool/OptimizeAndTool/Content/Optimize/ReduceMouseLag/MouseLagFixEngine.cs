using CommandHelp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;

namespace OptimizeAndTool.Content.Optimize.ReduceMouseLag
{
    /// <summary>
    /// 减少鼠标输入延迟核心采样引擎
    /// 在渲染阶段直接高频采样硬件光标位置，消除原版 60Hz 逻辑帧绑定的固有输入延迟
    /// 作者: SaintCirno9
    /// </summary>
    public static class MouseLagFixEngine
    {
        public static GetSetReset<bool> Enabled = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> UseWin32Direct = new GetSetReset<bool>(true, true);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        /// <summary>
        /// 即时更新鼠标坐标到当前物理光标位置
        /// </summary>
        public static void UpdateMousePosition()
        {
            if (!Enabled.val || Main.dedServ || (Main.instance != null && !Main.instance.IsActive)) return;

            int rawX = 0;
            int rawY = 0;
            bool gotWin32 = false;

            if (UseWin32Direct.val && Main.instance != null && Main.instance.Window != null)
            {
                IntPtr handle = Main.instance.Window.Handle;
                if (handle != IntPtr.Zero && GetCursorPos(out POINT pt))
                {
                    if (ScreenToClient(handle, ref pt))
                    {
                        rawX = pt.X;
                        rawY = pt.Y;
                        gotWin32 = true;
                    }
                }
            }

            if (!gotWin32)
            {
                MouseState state = Mouse.GetState();
                rawX = state.X;
                rawY = state.Y;
            }

            // 保持按键与滚轮状态不变，仅刷新物理光标采样
            MouseState mouseInfo = PlayerInput.MouseInfo;
            PlayerInput.MouseInfo = new MouseState(
                rawX,
                rawY,
                mouseInfo.ScrollWheelValue,
                mouseInfo.LeftButton,
                mouseInfo.MiddleButton,
                mouseInfo.RightButton,
                mouseInfo.XButton1,
                mouseInfo.XButton2
            );

            PlayerInput.MouseX = (int)((float)rawX * PlayerInput.RawMouseScale.X);
            PlayerInput.MouseY = (int)((float)rawY * PlayerInput.RawMouseScale.Y);
            PlayerInput.UpdateMainMouse();
            PlayerInput.CacheMousePositionForZoom();
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("reduceMouseLag", Enabled),
                CommandBuild.get2("reduceMouseLagWin32", UseWin32Direct)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enabled, "在渲染层高频直读物理光标坐标，彻底消除 60Hz 逻辑帧绑定的输入滞后", "Images/Item_5010", "硬件鼠标 / 消除输入延迟"),
                UIBuild.get2(UseWin32Direct, "优先通过 Windows 原生 API 获取窗口内绝对物理光标位置", "Images/Item_5011", "Win32 窗口直连采样")
            };
        }
    }
}
