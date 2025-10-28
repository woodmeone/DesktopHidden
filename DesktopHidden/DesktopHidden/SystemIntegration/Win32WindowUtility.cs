using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.IO; // Added for File.GetAttributes

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
        internal const uint WS_MINIMIZEBOX = 0x00020000; // 最小化按钮

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

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        // SetLayeredWindowAttributes flags
        internal const uint LWA_ALPHA = 0x00000002; // 使用bAlpha来设置窗口的透明度
        internal const uint LWA_COLORKEY = 0x00000001; // 使用crKey来设置透明色键

        // SetParent parameters
        internal const int GWL_HWNDPARENT = -8; // 设置窗口的父窗口句柄

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetDesktopWindow(); // 获取桌面窗口句柄

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
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_LAYERED; // 移除 WS_EX_TRANSPARENT，只保留 WS_EX_TOOLWINDOW 和 WS_EX_LAYERED
            SetWindowLong(hWnd, GWL_EXSTYLE, (int)exStyle);

            // 设置窗口的整体透明度为 45% (115/255)
            SetLayeredWindowAttributes(hWnd, 0, 115, LWA_ALPHA); // 设置 Alpha 值

            // 将窗口的父窗口设置为桌面窗口，使其始终显示在桌面上，而不是浮动在其他应用程序之上
            IntPtr hWndDesktop = GetDesktopWindow();
            SetParent(hWnd, hWndDesktop);
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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetFileAttributes(string lpFileName, FileAttributes dwFileAttributes);

        [Flags]
        public enum FileAttributes : uint
        {
            FILE_ATTRIBUTE_READONLY = 0x00000001,
            FILE_ATTRIBUTE_HIDDEN = 0x00000002,
            FILE_ATTRIBUTE_SYSTEM = 0x00000004,
            FILE_ATTRIBUTE_DIRECTORY = 0x00000010,
            FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
            FILE_ATTRIBUTE_DEVICE = 0x00000040,
            FILE_ATTRIBUTE_NORMAL = 0x00000080,
            FILE_ATTRIBUTE_TEMPORARY = 0x00000100,
            FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200,
            FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400,
            FILE_ATTRIBUTE_COMPRESSED = 0x00000800,
            FILE_ATTRIBUTE_OFFLINE = 0x00001000,
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
            FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
            FILE_ATTRIBUTE_VIRTUAL = 0x00010000
        }

        public static void HideFile(string filePath)
        {
            SetFileAttributes(filePath, FileAttributes.FILE_ATTRIBUTE_HIDDEN);
        }

        public static void ShowFile(string filePath)
        {
            // 获取当前文件属性
            FileAttributes attributes = (FileAttributes)File.GetAttributes(filePath);
            // 移除隐藏属性
            attributes &= ~FileAttributes.FILE_ATTRIBUTE_HIDDEN;
            SetFileAttributes(filePath, attributes);
        }
    }
}
