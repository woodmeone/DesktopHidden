using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace DesktopHidden.SystemIntegration
{
    public static class Win32WindowUtility
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        // SetWindowPos flags
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_SHOWWINDOW = 0x0040;

        // Get/SetWindowLongPtr parameters
        internal const int GWL_EXSTYLE = -20;
        internal const int GWL_STYLE = -16;

        // Window Styles
        internal const uint WS_CAPTION = 0x00C00000; // 标题栏和边框
        internal const uint WS_THICKFRAME = 0x00040000; // 可调整大小的边框

        // Extended Window Styles
        internal const uint WS_EX_TOOLWINDOW = 0x00000080; // 不在任务栏显示
        internal const uint WS_EX_TRANSPARENT = 0x00000020; // 鼠标穿透
        internal const uint WS_EX_LAYERED = 0x00080000; // 允许透明和鼠标穿透

        // Special window handles for SetWindowPos
        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // 置顶
        internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        internal static readonly IntPtr HWND_TOP = new IntPtr(0);
        internal static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        // Win32 Messages
        internal const int WM_NCLBUTTONDOWN = 0xA1;
        internal const int HTCAPTION = 0x2;
        internal const int HTLEFT = 0xA;
        internal const int HTRIGHT = 0xB;
        internal const int HTTOP = 0xC;
        internal const int HTTOPLEFT = 0xD;
        internal const int HTTOPRIGHT = 0xE;
        internal const int HTBOTTOM = 0xF;
        internal const int HTBOTTOMLEFT = 0x10;
        internal const int HTBOTTOMRIGHT = 0x11;

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // Dwm API
        [DllImport("dwmapi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MARGINS
        {
            public int cxLeftWidth;
            public int cyTopHeight;
            public int cxRightWidth;
            public int cyBottomHeight;
        }

        public static WindowId GetWindowIdFromWindow(IntPtr hWnd)
        {
            return Win32Interop.GetWindowIdFromWindow(hWnd);
        }

        public static void SetWindowTransparent(IntPtr hWnd)
        {
            // 设置窗口为无边框、无任务栏图标、透明、鼠标穿透和置顶
            uint exStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT; // Add WS_EX_TOOLWINDOW to remove from taskbar, WS_EX_TRANSPARENT for mouse pass-through
            SetWindowLong(hWnd, GWL_EXSTYLE, (int)exStyle);

            // Make the window topmost
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public static void SetWindowTopmost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public static void SetWindowNoTopmost(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }

        public static void SetWindowResizeRegion(IntPtr hWnd, int resizeBorderThickness)
        {
            // 移除系统标题栏和边框，但保留可调整大小的边框
            uint style = (uint)GetWindowLong(hWnd, GWL_STYLE);
            style &= ~WS_CAPTION; // 移除标题栏
            style |= WS_THICKFRAME; // 保留可调整大小的边框
            SetWindowLong(hWnd, GWL_STYLE, (int)style);

            // 扩展客户区到窗口边框，以允许调整大小
            MARGINS margins = new MARGINS
            {
                cxLeftWidth = resizeBorderThickness,
                cyTopHeight = resizeBorderThickness,
                cxRightWidth = resizeBorderThickness,
                cyBottomHeight = resizeBorderThickness
            };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);
        }
    }
}
