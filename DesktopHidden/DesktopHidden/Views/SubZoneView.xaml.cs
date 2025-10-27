using DesktopHidden.Models;
using DesktopHidden.SystemIntegration;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices; // Explicitly add this
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core; // Add this for CoreCursorType
using WinRT.Interop;
using Microsoft.UI.Xaml.Media; // Add this for VisualTreeHelper
using DesktopHidden.Converters; // Add this using statement

namespace DesktopHidden.Views
{
    public sealed partial class SubZoneView : UserControl
    {
        public SubZoneWindow ParentWindow { get; set; } // 添加 SubZoneWindow 引用

        public SubZoneView(SubZoneWindow parentWindow) // 修改构造函数
        {
            this.InitializeComponent();
            ParentWindow = parentWindow;
        }

        public SubZoneModel SubZoneModel => DataContext as SubZoneModel;

        /*
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

        private ResizeDirection GetResizeDirection(Point position)
        {
            bool onLeft = position.X < ResizeBorder;
            bool onRight = position.X > ActualWidth - ResizeBorder;
            bool onTop = position.Y < ResizeBorder;
            bool onBottom = position.Y > ActualHeight - ResizeBorder;

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
        */

        private void NavBar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (SubZoneModel != null && SubZoneModel.IsLocked) return; // 锁定状态下不允许拖拽
            // 调用 SubZoneWindow 的拖拽方法
            ParentWindow?.StartDragging(); // 使用 ParentWindow 属性
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubZoneModel != null)
            {
                SubZoneModel.IsLocked = !SubZoneModel.IsLocked; // 切换锁定状态
                ParentWindow?.SetResizable(!SubZoneModel.IsLocked); // 根据锁定状态设置窗口是否可调整大小
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 调用父窗口的 RequestClose 事件来请求关闭子区窗口。
            ParentWindow?.OnRequestClose();
        }

        private void ToggleContentButton_Click(object sender, RoutedEventArgs e)
        {
            if (SubZoneModel != null)
            {
                SubZoneModel.IsContentVisible = !SubZoneModel.IsContentVisible; // 切换内容区域显示状态
                ContentArea.Visibility = SubZoneModel.IsContentVisible ? Visibility.Visible : Visibility.Collapsed; // 更新UI
            }
        }

        private void DisguiseButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建并显示抠图界面
            var disguiseWindow = new Window();
            disguiseWindow.Title = "伪装抠图";
            disguiseWindow.Content = new DisguiseEditorView();
            disguiseWindow.Activate();
        }
    }
}
