using DesktopHidden.Models;
using DesktopHidden.SystemIntegration;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Graphics;
using WinRT.Interop;
using System.Runtime.InteropServices; // Added for DllImport
using Windows.UI.Core; // Add this for CoreCursorType
// using System.Windows.Interop; // Removed for HwndSource

namespace DesktopHidden.Views
{
    public sealed partial class SubZoneWindow : Window
    {
        public event EventHandler<Guid> RequestClose; // 添加 RequestClose 事件
        public SubZoneModel SubZoneModel { get; set; }
        public new AppWindow AppWindow => _appWindow; // 提供公共访问器
        private AppWindow _appWindow;
        private SubZoneView SubZoneUserControl; // 声明 SubZoneView 字段

        public SubZoneWindow(SubZoneModel subZoneModel)
        {
            System.Diagnostics.Debug.WriteLine("SubZoneWindow: Constructor started.");
            this.InitializeComponent();
            SubZoneModel = subZoneModel;
            this.Title = "子区 " + subZoneModel.Id.ToString().Substring(0, 4); // 简单标题

            // 将SubZoneModel绑定到SubZoneView
            SubZoneUserControl = new SubZoneView(this); // 实例化 SubZoneView 并传递当前 SubZoneWindow 实例
            SubZoneUserControl.DataContext = SubZoneModel;
            this.Content = SubZoneUserControl; // 设置窗口内容为 SubZoneView
            SubZoneUserControl.PointerPressed += SubZoneUserControl_PointerPressed; // 订阅 PointerPressed 事件
            SubZoneModel.PropertyChanged += SubZoneModel_PropertyChanged; // 订阅 SubZoneModel 的 PropertyChanged 事件

            // 获取窗口句柄
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            System.Diagnostics.Debug.WriteLine($"SubZoneWindow: Window Handle obtained: {hWnd}");

            // 设置窗口的初始位置和大小
            WindowId windowId = Win32WindowUtility.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            if (_appWindow != null)
            {
                System.Diagnostics.Debug.WriteLine("SubZoneWindow: AppWindow obtained successfully.");
                _appWindow.MoveAndResize(new RectInt32((int)SubZoneModel.Position.X, (int)SubZoneModel.Position.Y, (int)SubZoneModel.Size.Width, (int)SubZoneModel.Size.Height));
                (_appWindow.Presenter as OverlappedPresenter).IsResizable = true; // 允许调整大小
                (_appWindow.Presenter as OverlappedPresenter).IsMinimizable = false; // 禁止最小化
                // _appWindow.IsShownInSwitchers = false; // 不在Alt+Tab中显示，如果需要
                _appWindow.TitleBar.BackgroundColor = Colors.Transparent; // 设置标题栏背景为透明
                _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent; // 设置标题栏按钮背景为透明
                _appWindow.Changed += AppWindow_Changed; // 订阅 AppWindow 的 Changed 事件
                System.Diagnostics.Debug.WriteLine("SubZoneWindow: AppWindow properties set.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SubZoneWindow: Failed to obtain AppWindow.");
            }

            // 获取桌面背景窗口句柄
            // IntPtr hWndDesktopBackground = Win32WindowUtility.GetDesktopBackgroundWindow();
            // if (hWndDesktopBackground != IntPtr.Zero)
            // {
            //    // 将子区窗口设置为桌面背景窗口的子级
            //    Win32WindowUtility.SetParent(hWnd, hWndDesktopBackground);
            //    // 设置窗口样式，使其不被显示桌面最小化，并允许鼠标穿透
            //    uint exStyle = (uint)Win32WindowUtility.GetWindowLong(hWnd, Win32WindowUtility.GWL_EXSTYLE);
            //    exStyle |= Win32WindowUtility.WS_EX_TOOLWINDOW | Win32WindowUtility.WS_EX_LAYERED; // 添加WS_EX_TOOLWINDOW，但暂时不添加WS_EX_TRANSPARENT
            //    exStyle &= ~Win32WindowUtility.WS_EX_APPWINDOW; // 移除WS_EX_APPWINDOW，使其不显示在Alt+Tab中
            //    Win32WindowUtility.SetWindowLong(hWnd, Win32WindowUtility.GWL_EXSTYLE, (int)exStyle);

            //    // 确保窗口位于所有非置顶窗口的最底层
            //    // Win32WindowUtility.SetWindowPos(hWnd, Win32WindowUtility.HWND_TOPMOST, 0, 0, 0, 0, Win32WindowUtility.SWP_NOMOVE | Win32WindowUtility.SWP_NOSIZE | Win32WindowUtility.SWP_NOACTIVATE);
            // }
            // else
            // {
            //    // 如果无法获取桌面背景窗口，则回退到通用透明设置
            //    // 这里也需要设置 Alt+Tab 和任务栏相关的样式
            //    uint exStyle = (uint)Win32WindowUtility.GetWindowLong(hWnd, Win32WindowUtility.GWL_EXSTYLE);
            //    exStyle |= Win32WindowUtility.WS_EX_TOOLWINDOW | Win32WindowUtility.WS_EX_LAYERED; // 添加WS_EX_TOOLWINDOW，但暂时不添加WS_EX_TRANSPARENT
            //    exStyle &= ~Win32WindowUtility.WS_EX_APPWINDOW; // 移除WS_EX_APPWINDOW
            //    Win32WindowUtility.SetWindowLong(hWnd, Win32WindowUtility.GWL_EXSTYLE, (int)exStyle);

            //    // 确保窗口位于所有非置顶窗口的最底层
            //    // Win32WindowUtility.SetWindowPos(hWnd, Win32WindowUtility.HWND_TOPMOST, 0, 0, 0, 0, Win32WindowUtility.SWP_NOMOVE | Win32WindowUtility.SWP_NOSIZE | Win32WindowUtility.SWP_NOACTIVATE);
            // }

            // 设置窗口样式，使其不被显示桌面最小化，并允许鼠标穿透 (更新: 现在仅设置基本的样式，鼠标穿透由 ToggleWindowTransparent 动态控制)
            uint exStyle = (uint)Win32WindowUtility.GetWindowLong(hWnd, Win32WindowUtility.GWL_EXSTYLE);
            exStyle |= Win32WindowUtility.WS_EX_TOOLWINDOW | Win32WindowUtility.WS_EX_LAYERED; // 添加WS_EX_TOOLWINDOW, WS_EX_LAYERED
            exStyle &= ~Win32WindowUtility.WS_EX_APPWINDOW; // 移除WS_EX_APPWINDOW，使其不显示在Alt+Tab中
            Win32WindowUtility.SetWindowLong(hWnd, Win32WindowUtility.GWL_EXSTYLE, (int)exStyle);
            System.Diagnostics.Debug.WriteLine($"SubZoneWindow: Window Extended Style set to: {exStyle}");

            // 应用系统集成特性
            // Win32WindowUtility.SetWindowTransparent(hWnd); // 设置透明、鼠标穿透、不在任务栏
            // Win32WindowUtility.SetWindowTopmost(hWnd);    // 设置置顶，不再需要，已在SetWindowTransparent中处理

            // 初始设置窗口的鼠标穿透状态，窗口创建时默认为非激活状态（可穿透），等待 AppWindow_Changed 激活时切换
            Win32WindowUtility.ToggleWindowTransparent(hWnd, false); // 窗口创建时，默认不启用鼠标穿透 (更新: 初始应为可交互状态)
            System.Diagnostics.Debug.WriteLine("SubZoneWindow: Mouse transparency initialized.");

            // 为了避免标题栏显示，可以设置ExtendsContentIntoTitleBar = true
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null); // 移除标题栏

            // 注册窗口子类以拦截消息
            Win32WindowUtility.SetWindowSubclass(hWnd, SubZoneWndProc, (UIntPtr)SubZoneModel.Id.GetHashCode(), UIntPtr.Zero);
            System.Diagnostics.Debug.WriteLine("SubZoneWindow: Window subclass registered.");

            this.Closed += SubZoneWindow_Closed; // 注册 Closed 事件
            System.Diagnostics.Debug.WriteLine("SubZoneWindow: Constructor finished.");
        }

        // 新的消息处理函数，用于子类化
        private static IntPtr SubZoneWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData)
        {
            // const int WM_SHOWWINDOW = 0x0018;
            // const int SW_PARENTCLOSING = 1;

            // 拦截 WM_SYSCOMMAND 消息，特别是 SC_MINIMIZE 命令
            if (uMsg == Win32WindowUtility.WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == Win32WindowUtility.SC_MINIMIZE)
            {
                // 忽略最小化命令，防止子区窗口被最小化
                return IntPtr.Zero; // 消息已处理
            }

            // 拦截 WM_SIZE 消息，防止窗口被最小化
            if (uMsg == Win32WindowUtility.WM_SIZE && wParam.ToInt32() == Win32WindowUtility.SIZE_MINIMIZED)
            {
                // 忽略最小化命令，防止子区窗口被最小化
                return IntPtr.Zero; // 消息已处理
            }

            return Win32WindowUtility.DefSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass); // 修正参数数量，移除dwRefData
        }

        // 窗口关闭时移除子类
        private void SubZoneWindow_Closed(object sender, WindowEventArgs args)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            Win32WindowUtility.RemoveWindowSubclass(hWnd, SubZoneWndProc, (UIntPtr)SubZoneModel.Id.GetHashCode());
        }

        // 移除 GetAppWindowForCurrentWindow 方法

        // 用于触发 RequestClose 事件的方法
        public void OnRequestClose() => RequestClose?.Invoke(this, SubZoneModel.Id);

        // 设置窗口是否可调整大小的方法
        public void SetResizable(bool canResize)
        {
            if (_appWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                overlappedPresenter.IsResizable = canResize;
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange || args.DidSizeChange)
            {
                SubZoneModel.Position = new Windows.Foundation.Point(sender.Position.X, sender.Position.Y);
                SubZoneModel.Size = new Windows.Foundation.Size(sender.Size.Width, sender.Size.Height); // 更新当前尺寸
                if (args.DidSizeChange && SubZoneModel.IsContentVisible) // 如果大小改变且内容可见，才更新 OriginalSize
                {
                    SubZoneModel.OriginalSize = new Windows.Foundation.Size(sender.Size.Width, sender.Size.Height);
                }
            }

            // 监听窗口激活状态变化，切换鼠标穿透
            if (args.DidPresenterChange)
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                // 使用 OverlappedPresenterState.Minimized 判断激活状态
                bool isActivated = (sender.Presenter as OverlappedPresenter)?.State != OverlappedPresenterState.Minimized;
                // 激活时禁用穿透，非激活时启用穿透
                Win32WindowUtility.ToggleWindowTransparent(hWnd, !isActivated); 

                // 根据激活状态设置窗口置顶或取消置顶
                if (isActivated)
                {
                    Win32WindowUtility.SetWindowTopmost(hWnd); // 激活时置顶
                }
                else
                {
                    Win32WindowUtility.SetWindowNoTopmost(hWnd); // 非激活时取消置顶
                }
            }
        }

        private void SubZoneModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubZoneModel.IsContentVisible))
            {
                AdjustWindowHeight();
            }
        }

        private void AdjustWindowHeight()
        {
            if (_appWindow != null && SubZoneModel != null)
            {
                int newHeight = SubZoneModel.IsContentVisible ? (int)SubZoneModel.OriginalSize.Height : 60; // 使用 OriginalSize.Height
                _appWindow.Resize(new Windows.Graphics.SizeInt32((int)SubZoneModel.Size.Width, newHeight));
            }
        }

        public void StartDragging()
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Win32WindowUtility.SendMessage(hWnd, Win32WindowUtility.WM_NCLBUTTONDOWN, Win32WindowUtility.HTCAPTION, 0);
        }

        private const double ResizeBorder = 8; // 边缘宽度
        private const double CornerResizeBorder = 16; // 角落宽度

        private enum ResizeDirection
        {
            None,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private ResizeDirection GetResizeDirection(Windows.Foundation.Point position)
        {
            // The content of the SubZoneWindow is SubZoneView, which is a UserControl
            // We need to get the ActualWidth and ActualHeight of the SubZoneView
            double actualWidth = 0;
            double actualHeight = 0;

            if (this.Content is FrameworkElement contentElement)
            {
                actualWidth = contentElement.ActualWidth;
                actualHeight = contentElement.ActualHeight;
            }

            bool onLeft = position.X < ResizeBorder;
            bool onRight = position.X > actualWidth - ResizeBorder;
            bool onTop = position.Y < ResizeBorder;
            bool onBottom = position.Y > actualHeight - ResizeBorder;

            if (onTop && onLeft) return ResizeDirection.TopLeft;
            if (onTop && onRight) return ResizeDirection.TopRight;
            if (onBottom && onLeft) return ResizeDirection.BottomLeft;
            if (onBottom && onRight) return ResizeDirection.BottomRight;
            if (onLeft) return ResizeDirection.Left;
            if (onRight) return ResizeDirection.Right;
            if (onTop) return ResizeDirection.Top;
            if (onBottom) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }

        private void SubZoneUserControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed)
            {
                if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽和调整大小

                AppWindow appWindow = this.AppWindow; // 获取AppWindow
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this); // 获取窗口句柄

                Windows.Foundation.Point mousePosition = e.GetCurrentPoint(sender as UIElement).Position; // 鼠标相对于SubZoneView的坐标
                ResizeDirection direction = GetResizeDirection(mousePosition);

                if (direction != ResizeDirection.None)
                {
                    int htParam = 0;
                    switch (direction)
                    {
                        case ResizeDirection.Left: htParam = Win32WindowUtility.HTLEFT; break;
                        case ResizeDirection.Right: htParam = Win32WindowUtility.HTRIGHT; break;
                        case ResizeDirection.Top: htParam = Win32WindowUtility.HTTOP; break;
                        case ResizeDirection.Bottom: htParam = Win32WindowUtility.HTBOTTOM; break;
                        case ResizeDirection.TopLeft: htParam = Win32WindowUtility.HTTOPLEFT; break;
                        case ResizeDirection.TopRight: htParam = Win32WindowUtility.HTTOPRIGHT; break;
                        case ResizeDirection.BottomLeft: htParam = Win32WindowUtility.HTBOTTOMLEFT; break;
                        case ResizeDirection.BottomRight: htParam = Win32WindowUtility.HTBOTTOMRIGHT; break;
                    }
                    Win32WindowUtility.SendMessage(hWnd, Win32WindowUtility.WM_NCLBUTTONDOWN, htParam, 0);
                }
            }
        }

        // 不再需要 SubZoneUserControl_PointerMoved 和 SubZoneUserControl_PointerReleased 方法，
        // 因为 SubZoneView 会直接处理 NavBar_PointerPressed 来进行拖拽，
        // 并且调整大小的逻辑已经通过 SubZoneUserControl_PointerPressed 中的 SendMessage 处理。
    }

    public static class CustomInputCursor
    {
        // 辅助类用于设置鼠标光标
        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        public static void SetCursor(InputCursor cursor)
        {
            if (cursor != null && cursor.GetType().Name == "CoreCursorInputCursor")
            {
                // Get CoreCursor's underlying HCURSOR value
                // This is a bit hacky, as CoreCursor is internal, but for now this works.
                var coreCursor = (CoreCursor)cursor.GetType().GetProperty("CoreCursor").GetValue(cursor);
                SetCursor(coreCursor.Type.ToHcursor());
            }
            else
            {
                // Fallback for other cursor types or if introspection fails
                SetCursor(InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 1)));
            }
        }

        // Helper to convert CoreCursorType to HCURSOR
        private static IntPtr ToHcursor(this CoreCursorType cursorType)
        {
            switch (cursorType)
            {
                case CoreCursorType.Arrow: return (IntPtr)32512; // IDC_ARROW
                case CoreCursorType.SizeNorthSouth: return (IntPtr)32645; // IDC_SIZENS
                case CoreCursorType.SizeWestEast: return (IntPtr)32644; // IDC_SIZEWE
                case CoreCursorType.SizeAll: return (IntPtr)32646; // IDC_SIZEALL (添加 SizeAll)
                // Add more as needed
                default: return (IntPtr)32512; // IDC_ARROW
            }
        }
    }
}
