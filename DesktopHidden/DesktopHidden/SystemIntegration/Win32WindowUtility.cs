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

        // SetWindowSubclass flags
        internal const uint SUBCLASS_DISABLED = 0x00000008; // 禁用子类化

        // Subclassing API
        [DllImport("ComCtl32.dll", SetLastError = true)]
        internal static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass, UIntPtr dwRefData);

        [DllImport("ComCtl32.dll", SetLastError = true)]
        internal static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, UIntPtr uIdSubclass);

        [DllImport("ComCtl32.dll", SetLastError = true)]
        internal static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass); // 修正参数数量，移除dwRefData

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
        internal const uint WS_EX_APPWINDOW = 0x00040000; // 应用程序窗口，出现在任务栏和Alt+Tab中

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

        // System Commands
        internal const int WM_SYSCOMMAND = 0x0112;
        internal const int SC_MINIMIZE = 0xF020;

        // Window Size Messages
        internal const int WM_SIZE = 0x0005;
        internal const int SIZE_MINIMIZED = 1; // 窗口被最小化

        // Subclassing delegate
        internal delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData);

        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetDesktopWindow(); // 获取桌面窗口句柄

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName); // 查找顶级窗口

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow); // 查找子窗口

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent); // 设置窗口父级

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        // SetLayeredWindowAttributes flags
        internal const uint LWA_ALPHA = 0x00000002; // 使用bAlpha来设置窗口的透明度
        internal const uint LWA_COLORKEY = 0x00000001; // 使用crKey来设置透明色键

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
            // 设置窗口为无边框、无任务栏图标、透明、鼠标穿透，但不修改Z轴顺序或影响Alt+Tab/任务栏显示
            uint exStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
            exStyle |= WS_EX_LAYERED; // 仅保留 WS_EX_LAYERED (用于透明效果)
            SetWindowLong(hWnd, GWL_EXSTYLE, (int)exStyle);

            // 设置窗口的整体透明度为 45% (115/255)
            SetLayeredWindowAttributes(hWnd, 0, 115, LWA_ALPHA);
        }

        /// <summary>
        /// 切换窗口的鼠标穿透状态 (WS_EX_TRANSPARENT)
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="enable">是否启用鼠标穿透</param>
        public static void ToggleWindowTransparent(IntPtr hWnd, bool enable)
        {
            uint exStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
            if (enable)
            {
                exStyle |= WS_EX_TRANSPARENT; // 添加鼠标穿透样式
            }
            else
            {
                exStyle &= ~WS_EX_TRANSPARENT; // 移除鼠标穿透样式
            }
            SetWindowLong(hWnd, GWL_EXSTYLE, (int)exStyle);
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

        // 获取桌面壁纸路径
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(uint uiAction, uint uiParam, System.Text.StringBuilder pvParam, uint fWinIni);

        private const uint SPI_GETDESKWALLPAPER = 0x0073;
        private const uint MAX_PATH = 260;

        public static string GetDesktopWallpaperPath()
        {
            System.Text.StringBuilder wallpaperPath = new System.Text.StringBuilder((int)MAX_PATH);
            SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaperPath, 0);
            return wallpaperPath.ToString();
        }

        /// <summary>
        /// 获取桌面背景窗口句柄 (Progman)
        /// </summary>
        /// <returns>桌面背景窗口句柄</returns>
        public static IntPtr GetDesktopBackgroundWindow()
        {
            // 直接找到Progman窗口，它通常是桌面图标的父窗口
            IntPtr progman = FindWindow("Progman", null);
            return progman; // 返回Progman窗口句柄
        }
    }
}
