using System;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.IO; // Added for File.GetAttributes
using Windows.Storage; // 添加此行，用于获取应用数据文件夹

namespace DesktopHidden.SystemIntegration
{
    public static class Win32WindowUtility
    {
        private static string _hiddenStoragePath = string.Empty; // 隐藏文件存储目录路径

        static Win32WindowUtility()
        {
            // 在静态构造函数中初始化隐藏文件存储目录
            string localAppData = ApplicationData.Current.LocalFolder.Path;
            _hiddenStoragePath = Path.Combine(localAppData, "DesktopHiddenItems");

            if (!Directory.Exists(_hiddenStoragePath))
            {
                Directory.CreateDirectory(_hiddenStoragePath);
            }
            System.Diagnostics.Debug.WriteLine($"隐藏文件存储目录: {_hiddenStoragePath}");
        }

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

        // SHChangeNotify 函数和相关常量
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        // SHChangeNotify 事件 ID
        public const uint SHCNE_ASSOCCHANGED = 0x08000000; // 系统文件关联改变
        public const uint SHCNE_ALLEVENTS = 0x7FFFFFFF; // 所有事件
        public const uint SHCNE_ATTRIBUTES = 0x00000008; // 属性改变 (我们主要用这个)
        public const uint SHCNE_CREATE = 0x00000002; // 创建
        public const uint SHCNE_DELETE = 0x00000004; // 删除
        public const uint SHCNE_UPDATEITEM = 0x00000001; // 更新项目

        // SHChangeNotify 标志
        public const uint SHCNF_IDLIST = 0x0000; // dwItem1 和 dwItem2 是 PIDLs
        public const uint SHCNF_PATHA = 0x0001; // dwItem1 和 dwItem2 是字符串路径 (ANSI)
        public const uint SHCNF_PATHW = 0x0002; // dwItem1 和 dwItem2 是字符串路径 (Unicode)
        public const uint SHCNF_PRINTER = 0x0003; // dwItem1 和 dwItem2 是打印机名称
        public const uint SHCNF_DWORD = 0x0003; // dwItem1 是 DWORD
        public const uint SHCNF_FLUSH = 0x1000; // 同步刷新
        public const uint SHCNF_FLUSHNOWAIT = 0x2000; // 异步刷新


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

        /// <summary>
        /// 将桌面项目移动到隐藏存储目录。
        /// </summary>
        /// <param name="originalDesktopPath">桌面项目的原始路径。</param>
        /// <returns>移动后在隐藏存储中的新路径，如果移动失败则返回空。</returns>
        public static string? MoveDesktopItemToHiddenStorage(string originalDesktopPath)
        {
            if (!File.Exists(originalDesktopPath) && !Directory.Exists(originalDesktopPath))
            {
                System.Diagnostics.Debug.WriteLine($"MoveDesktopItemToHiddenStorage: Original item not found at {originalDesktopPath}");
                return null;
            }

            try
            {
                string fileName = Path.GetFileName(originalDesktopPath);
                string destinationPathInHiddenStorage = Path.Combine(_hiddenStoragePath, fileName);

                // 如果目标文件已存在，则删除它（简化处理，实际应用中可能需要更复杂的冲突解决）
                if (File.Exists(destinationPathInHiddenStorage)) File.Delete(destinationPathInHiddenStorage);
                else if (Directory.Exists(destinationPathInHiddenStorage)) Directory.Delete(destinationPathInHiddenStorage, true);

                File.Move(originalDesktopPath, destinationPathInHiddenStorage);
                System.Diagnostics.Debug.WriteLine($"Moved '{originalDesktopPath}' to hidden storage: '{destinationPathInHiddenStorage}'");

                // 通知 Shell 刷新原始目录（桌面）
                SHChangeNotify(SHCNE_DELETE, SHCNF_PATHW, Marshal.StringToCoTaskMemUni(originalDesktopPath), IntPtr.Zero);
                return destinationPathInHiddenStorage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error moving '{originalDesktopPath}' to hidden storage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将隐藏存储中的项目恢复到其原始桌面位置。
        /// </summary>
        /// <param name="hiddenPathInStorage">隐藏存储中的项目路径。</param>
        /// <param name="originalDesktopPath">项目在桌面上的原始路径。</param>
        public static void RestoreDesktopItemFromHiddenStorage(string hiddenPathInStorage, string originalDesktopPath)
        {
            if (!File.Exists(hiddenPathInStorage) && !Directory.Exists(hiddenPathInStorage))
            {
                System.Diagnostics.Debug.WriteLine($"RestoreDesktopItemFromHiddenStorage: Hidden item not found at {hiddenPathInStorage}");
                return;
            }

            try
            {
                // 如果桌面已存在同名文件，先删除 (简化处理)
                if (File.Exists(originalDesktopPath)) File.Delete(originalDesktopPath);
                else if (Directory.Exists(originalDesktopPath)) Directory.Delete(originalDesktopPath, true);

                File.Move(hiddenPathInStorage, originalDesktopPath);
                System.Diagnostics.Debug.WriteLine($"Restored '{hiddenPathInStorage}' to desktop: '{originalDesktopPath}'");

                // 通知 Shell 刷新原始桌面目录
                SHChangeNotify(SHCNE_CREATE, SHCNF_PATHW, Marshal.StringToCoTaskMemUni(originalDesktopPath), IntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring '{hiddenPathInStorage}' to '{originalDesktopPath}': {ex.Message}");
            }
        }

        /// <summary>
        /// 隐藏桌面上的一个项目，将其移动到隐藏存储。
        /// </summary>
        /// <param name="originalDesktopPath">桌面项目的原始路径。</param>
        /// <returns>移动后在隐藏存储中的新路径，如果隐藏失败则返回空。</returns>
        public static string? HideDesktopItem(string originalDesktopPath)
        {
            return MoveDesktopItemToHiddenStorage(originalDesktopPath);
        }

        /// <summary>
        /// 隐藏文件，将其移动到隐藏存储。
        /// </summary>
        /// <param name="originalFilePath">文件的原始路径。</param>
        /// <returns>移动后在隐藏存储中的新路径，如果隐藏失败则返回空。</returns>
        public static string? HideFile(string originalFilePath)
        {
            return MoveDesktopItemToHiddenStorage(originalFilePath);
        }

        /// <summary>
        /// 显示一个之前隐藏的桌面项目，将其从隐藏存储移回桌面。
        /// </summary>
        /// <param name="hiddenPathInStorage">隐藏存储中的项目路径。</param>
        /// <param name="originalDesktopPath">项目在桌面上的原始路径。</param>
        public static void ShowDesktopItem(string hiddenPathInStorage, string originalDesktopPath)
        {
            RestoreDesktopItemFromHiddenStorage(hiddenPathInStorage, originalDesktopPath);
        }
    }
}
