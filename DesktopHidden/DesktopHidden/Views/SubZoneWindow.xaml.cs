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
using System.Diagnostics; // 用于Debug.WriteLine

namespace DesktopHidden.Views
{
    public sealed partial class SubZoneWindow : Window
    {
        // Win32 常量
        private const int GWLP_WNDPROC = -4; // 设置新的窗口过程
        private const int WM_SYSCOMMAND = 0x0112; // 系统命令消息
        private const int SC_MINIMIZE = 0xF020; // 最小化命令

        // 用于存储原始窗口过程的委托和句柄
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private static WndProcDelegate newWndProc = null;
        private IntPtr oldWndProc = IntPtr.Zero;

        // DllImport 声明
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndProc, IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public event EventHandler<Guid>? RequestClose; // 添加 RequestClose 事件，声明为可空
        public SubZoneModel? SubZoneModel { get; set; } // 声明为可空
        public new AppWindow AppWindow => _appWindow; // 提供公共访问器
        private AppWindow _appWindow;
        private SubZoneView? SubZoneUserControl; // 声明 SubZoneView 字段，声明为可空

        public SubZoneWindow(SubZoneModel subZoneModel)
        {
            this.InitializeComponent();
            SubZoneModel = subZoneModel;
            this.Title = "子区 " + subZoneModel.Id.ToString().Substring(0, 4); // 简单标题

            // 将SubZoneModel绑定到SubZoneView
            SubZoneUserControl = new SubZoneView(this); // 实例化 SubZoneView 并传递当前 SubZoneWindow 实例
            SubZoneUserControl.DataContext = SubZoneModel;
            this.Content = SubZoneUserControl; // 设置窗口内容为 SubZoneView
            SubZoneUserControl.PointerPressed += SubZoneUserControl_PointerPressed; // 订阅 PointerPressed 事件
            if (SubZoneModel != null)
            {
                SubZoneModel.PropertyChanged += SubZoneModel_PropertyChanged; // 订阅 SubZoneModel 的 PropertyChanged 事件
            }

            // 获取窗口句柄
            IntPtr hWnd = WindowNative.GetWindowHandle(this);

            // 移除最小化框样式
            uint style = (uint)Win32WindowUtility.GetWindowLong(hWnd, Win32WindowUtility.GWL_STYLE);
            style &= ~Win32WindowUtility.WS_MINIMIZEBOX;
            Win32WindowUtility.SetWindowLong(hWnd, Win32WindowUtility.GWL_STYLE, (int)style);

            // 设置窗口的初始位置和大小
            _appWindow = GetAppWindowForCurrentWindow();
            if (_appWindow != null)
            {
                _appWindow.MoveAndResize(new RectInt32((int)SubZoneModel.Position.X, (int)SubZoneModel.Position.Y, (int)SubZoneModel.Size.Width, (int)SubZoneModel.Size.Height));
                if (_appWindow.Presenter is OverlappedPresenter overlappedPresenter)
                {
                    overlappedPresenter.IsResizable = true; // 允许调整大小
                }
                // _appWindow.IsShownInSwitchers = false; // 不在Alt+Tab中显示，如果需要
                _appWindow.TitleBar.BackgroundColor = Colors.Transparent; // 设置标题栏背景为透明
                _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent; // 设置标题栏按钮背景为透明
            }

            // 应用系统集成特性
            Win32WindowUtility.SetWindowTransparent(hWnd); // 设置透明、鼠标穿透、不在任务栏
            // Win32WindowUtility.SetWindowTopmost(hWnd);    // 设置置顶，此行已移除，避免窗口浮动在其他应用之上

            // 为了避免标题栏显示，可以设置ExtendsContentIntoTitleBar = true
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null); // 移除标题栏

            // 设置可拖拽和调整大小的区域
            Win32WindowUtility.SetWindowResizeRegion(hWnd, 8); // 8像素宽的边框用于调整大小

            // 监听AppWindow的Changed事件来更新SubZoneModel的位置和大小
            _appWindow.Changed += AppWindow_Changed;

            // 替换窗口过程以拦截消息
            newWndProc = new WndProcDelegate(NewWindowProc);
            oldWndProc = SetWindowLongPtr(hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(newWndProc));

            // 订阅Closed事件，以便在窗口关闭时恢复原始窗口过程
            this.Closed += SubZoneWindow_Closed;
        }

        private void SubZoneWindow_Closed(object sender, WindowEventArgs args)
        {
            // 恢复原始窗口过程
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            SetWindowLongPtr(hWnd, GWLP_WNDPROC, oldWndProc);
            newWndProc = null; // 释放对委托的引用
        }

        private IntPtr NewWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_SYSCOMMAND)
            {
                // 阻止最小化命令
                if (wParam.ToInt32() == SC_MINIMIZE)
                {
                    Debug.WriteLine("阻止最小化！"); // 调试输出
                    return IntPtr.Zero; // 阻止消息进一步处理
                }
            }
            // 调用原始窗口过程
            return CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
        }

        // 用于触发 RequestClose 事件的方法
        public void OnRequestClose()
        {
            if (SubZoneModel != null)
            {
                RequestClose?.Invoke(this, SubZoneModel.Id);
            }
        }

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
                if (SubZoneModel != null)
                {
                    SubZoneModel.Position = new Windows.Foundation.Point(sender.Position.X, sender.Position.Y);
                    SubZoneModel.Size = new Windows.Foundation.Size(sender.Size.Width, sender.Size.Height); // 更新当前尺寸
                    if (args.DidSizeChange && SubZoneModel.IsContentVisible) // 如果大小改变且内容可见，才更新 OriginalSize
                    {
                        SubZoneModel.OriginalSize = new Windows.Foundation.Size(sender.Size.Width, sender.Size.Height);
                    }
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

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32WindowUtility.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
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

                UIElement? uiElementSender = sender as UIElement; // 声明为可空
                Windows.Foundation.Point mousePosition = e.GetCurrentPoint(uiElementSender).Position; // 鼠标相对于SubZoneView的坐标
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

        public static void SetCursor(InputCursor? cursor) // 声明为可空
        {
            if (cursor != null && cursor.GetType().Name == "CoreCursorInputCursor")
            {
                // Get CoreCursor's underlying HCURSOR value
                // This is a bit hacky, as CoreCursor is internal, but for now this works.
                var coreCursor = (CoreCursor)cursor.GetType().GetProperty("CoreCursor")!.GetValue(cursor)!; // 使用 ! 避免警告
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
